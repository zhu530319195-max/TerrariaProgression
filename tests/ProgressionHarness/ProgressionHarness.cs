using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.NPCs;
using TerrariaProgression.Players;
using TerrariaProgression.Networking;
using TerrariaProgression.Talents;
using Terraria.GameContent.ItemDropRules;

namespace ProgressionHarness;

public sealed class ProgressionHarness : Mod { }

public sealed class CiCommand : ModCommand
{
    public override string Command => "tpci";
    public override CommandType Type => CommandType.Console;
    public override void Action(CommandCaller caller, string input, string[] args) => ModContent.GetInstance<RuntimeChecks>().RunFromConsole();
}

public sealed partial class RuntimeChecks : ModSystem
{
    private bool ran;
    private int count;
    private ProgressionConfig Config => ModContent.GetInstance<ProgressionConfig>();
    private ProgressionPlayer A => Main.player[0].GetModPlayer<ProgressionPlayer>();
    private ProgressionPlayer B => Main.player[1].GetModPlayer<ProgressionPlayer>();
    // The world and mod registries are ready, but the networking thread has not yet
    // started. Synthetic clients cannot race real connection/disconnection handling.
    public override void PostWorldLoad() => RunFromConsole();
    private void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        count++;
        Console.WriteLine("RUNTIME PASS: " + name);
    }
    private void Reset()
    {
        foreach (var n in Main.npc) n.active = false;
        ModContent.GetInstance<EncounterSystem>().ClearWorld();
        Main.netMode = NetmodeID.SinglePlayer;
        Main.myPlayer = 0;
        for (int i = 0; i < 2; i++) {
            Main.player[i] = new Player { whoAmI = i };
            Main.player[i].active = true;
            Netplay.Clients[i].Socket = new RecordingSocket();
            NetMessage.buffer[i].broadcast = true;
            Main.player[i].dead = false;
            Main.player[i].GetModPlayer<ProgressionPlayer>().Initialize();
            Main.player[i].GetModPlayer<ProgressionPlayer>().SessionReady = true;
        }
        Config.TalentPointsPerLevel = "1";
        Config.ShareExperienceServerWide = false;
        Config.HpToXpMultiplier = 1;
        Config.StatueExperienceMultiplier = 0;
        Config.ExperienceRequirementCap = 50000;
    }
    private NPC Spawn(int hp, IEntitySource? source = null)
    {
        int index = NPC.NewNPC(source ?? new EntitySource_Misc("CI"), Main.spawnTileX * 16, Main.spawnTileY * 16, NPCID.BlueSlime);
        var npc = Main.npc[index];
        npc.life = npc.lifeMax = hp;
        npc.aiStyle = -1;
        EncounterSystem.Observe(npc);
        return npc;
    }
    private void Settle(params NPC[] members)
    {
        EncounterSystem.UpdateEncounters(Main.GameUpdateCount);
        EncounterSystem.UpdateEncounters(Main.GameUpdateCount + 3);
    }
    private static NPC.HitInfo Hit(int damage) => new() { Damage = damage, SourceDamage = damage, DamageType = DamageClass.Generic, HideCombatText = true };
    private void Kill(NPC npc, int player = 0)
    {
        var hit = Hit(npc.life);
        int dealt = npc.StrikeNPC(hit);
        // The game's item/projectile caller invokes the on-hit hook after StrikeNPC.
        NPCLoader.OnHitByItem(npc, Main.player[player], new Item(), hit, dealt);
    }
    internal void RunFromConsole()
    {
        if (ran) return;
        ran = true;
        if (!Main.dedServ || Environment.GetEnvironmentVariable("TP_CI_HARNESS") != "1") {
            Console.WriteLine("CI HARNESS REFUSED: explicit environment flag required.");
            return;
        }
        try {
            Run();
            Console.WriteLine($"TP_RUNTIME_PASS {count}");
            Environment.Exit(0);
        }
        catch (Exception error) {
            Console.WriteLine("TP_RUNTIME_FAIL " + error);
            Environment.Exit(1);
        }
    }
    private void Run()
    {
        Reset();
        A.Award(1000 * Experience.Scale);
        var tag = new TagCompound(); A.SaveData(tag);
        using var stream = new MemoryStream();
        TagIO.ToStream(tag, stream); stream.Position = 0;
        var decoded = TagIO.FromStream(stream);
        B.LoadData(decoded);
        Check(B.State.Level == 4 && B.State.CurrentExperience == 50 * Experience.Scale, "real TagIO / SaveData / LoadData");
        A.Player.dead = true; A.UpdateDead(); A.OnRespawn();
        Check(A.State.Level == 4, "death hooks preserve progression");
        ModContent.GetInstance<EncounterSystem>().ClearWorld(); A.OnEnterWorld();
        Check(A.State.TotalExperienceEarned == 1000 * Experience.Scale, "world clear preserves character");

        Reset(); var npc = Spawn(1000);
        Kill(npc);
        Check(A.State.TotalExperienceEarned == 0, "fatal strike waits for contribution callback");
        Settle(npc);
        Check(A.State.TotalExperienceEarned == 1000 * Experience.Scale && A.State.Level == 4, "native kill awards lifeMax and levels");
        NPCLoader.OnKill(npc); Settle(npc);
        Check(A.State.TotalExperienceEarned == 1000 * Experience.Scale, "duplicate death callback pays once");

        Reset(); npc = Spawn(1000);
        EncounterSystem.ReportAttributedDamage(npc, Main.player[0], 100);
        npc.life = 0; npc.active = false; Settle(npc);
        Check(A.State.TotalExperienceEarned == 0, "unwitnessed despawn is not a death payout");

        Reset(); npc = Spawn(5); // Use actual rabbit so initial slime defaults cannot affect budget.
        npc.active = false;
        int rabbitSlot = NPC.NewNPC(new EntitySource_Misc("CI"), Main.spawnTileX * 16, Main.spawnTileY * 16, NPCID.Bunny);
        npc = Main.npc[rabbitSlot];
        int rabbitHp = npc.lifeMax;
        Kill(npc); Settle(npc);
        Check(A.State.TotalExperienceEarned == rabbitHp * Experience.Scale, "critter XP uses actual lifeMax");

        foreach (float multiplier in new[] { 0f, .5f, 1f, 2f }) {
            Reset(); Config.StatueExperienceMultiplier = multiplier;
            npc = Spawn(100); npc.SpawnedFromStatue = true;
            Kill(npc); Settle(npc);
            Check(A.State.TotalExperienceEarned == Experience.FromDecimal(100m * (decimal)multiplier), "statue multiplier " + multiplier);
        }
        Reset(); npc = Spawn(100); npc.townNPC = true;
        Kill(npc); Settle(npc);
        Check(A.State.TotalExperienceEarned == 0, "town NPC excluded");

        Reset(); var root = Spawn(1000); var child = Spawn(1000, new EntitySource_Parent(root));
        Kill(child); Kill(root); Settle(root);
        Check(A.State.TotalExperienceEarned == 1000 * Experience.Scale, "parent/child budget is max once");
        Reset(); root = Spawn(1000); child = Spawn(1000);
        child.realLife = root.whoAmI; EncounterSystem.Observe(child);
        Kill(root); child.active = false; Settle(root);
        Check(A.State.TotalExperienceEarned == 1000 * Experience.Scale, "shared root kill with auto-removed body");
        Reset(); root = Spawn(1000); child = Spawn(1000); child.realLife = root.whoAmI;
        EncounterSystem.Observe(child); Kill(root);
        var unrelated = Spawn(100);
        EncounterSystem.Observe(child);
        Check(!ReferenceEquals(EncounterSystem.Get(child).Group.Root, EncounterSystem.Get(unrelated).Group.Root), "recycled root slot does not absorb unrelated NPC");
        Reset(); root = Spawn(1000); child = Spawn(1000);
        root.aiStyle = child.aiStyle = NPCAIStyleID.Worm;
        root.ai[0] = child.whoAmI; child.ai[1] = root.whoAmI;
        EncounterSystem.Observe(root); EncounterSystem.Observe(child);
        Kill(child); root.active = false; Settle(root);
        Check(A.State.TotalExperienceEarned == 0, "partial worm despawn has no completed-boss payout");

        Reset(); npc = Spawn(1000);
        ServerStrike(npc, 0, 700); ServerStrike(npc, 1, 999999); // Overkill must count only remaining 300.
        Main.netMode = NetmodeID.SinglePlayer; Settle(npc);
        Check(A.State.TotalExperienceEarned == 700 * Experience.Scale && B.State.TotalExperienceEarned == 300 * Experience.Scale,
            "official server strike reader + HitEffect: 700/300, overkill clipped");

        Reset(); npc = Spawn(1000);
        Kill(npc); A.PlayerDisconnect(); Settle(npc);
        Check(B.State.TotalExperienceEarned == 0, "disconnected contribution never moves to another slot");

        Reset(); Config.HpToXpMultiplier = float.NaN; Config.StatueExperienceMultiplier = -5; Config.ExperienceRequirementCap = -1; Config.OnChanged();
        Check(Config.HpToXpMultiplier == 1 && Config.StatueExperienceMultiplier == 0 && Config.ExperienceRequirementCap == 0, "config boundary validation");
        Main.netMode = NetmodeID.Server;
        var message = Terraria.Localization.NetworkText.Empty;
        Check(!Config.AcceptClientChanges(Config, 0, ref message), "server rejects client rule edits");

        Reset(); A.Award(1000 * Experience.Scale);
        var imported = new ProgressionState(); imported.Award(10000 * Experience.Scale, 50000);
        Main.netMode = NetmodeID.Server;
        Receive(2, imported); // Client must never upload an authoritative snapshot.
        Check(A.State.Level == 4, "server rejects client-authored snapshot");
        Receive(1, imported);
        Check(A.State.Level == 4, "duplicate join cannot overwrite session progress");
        Main.netMode = NetmodeID.MultiplayerClient;
        Receive(2, imported, padding: true);
        Check(A.State.Level == imported.Level && A.SessionReady, "client accepts server snapshot from shared buffer");
        Main.netMode = NetmodeID.SinglePlayer;
        Check(A.State.TotalExperienceEarned == 10000 * Experience.Scale, "received MP progression survives SP transition");
        Main.netMode = NetmodeID.Server; A.SessionReady = false;
        using var invalid = new MemoryStream(new byte[] { ProgressionNetwork.ProtocolVersion, 1, 4, 0, 99, 0, 0, 0 });
        ModContent.GetInstance<TerrariaProgression.TerrariaProgression>().HandlePacket(new BinaryReader(invalid), 0);
        Check(!A.SessionReady && A.State.TotalExperienceEarned == 10000 * Experience.Scale, "malformed import rejected without erasing existing state");
        RunTalents();
        RunCompletion();
    }
    private void RunCompletion()
    {
        Reset(); A.Award(100000000 * Experience.Scale);
        foreach (var def in NumericTalents.All) A.ApplyTalent(TalentOperation.Upgrade, def.Id, def.Category, 10);
        var p = A.Player; var extended = p.GetModPlayer<ExtendedTalentPlayer>();
        p.statLife = 50; p.statMana = 5; p.breath = 100;
        p.ResetEffects(); PlayerLoader.PostUpdateEquips(p);
        Check(p.breathMax==400 && p.breath==100,"breath capacity doubles without refill");
        var sword = new Item(ItemID.CopperBroadsword);
        Check(p.GetWeaponCrit(sword)==104,"native weapon crit includes +100 percentage points");
        float itemScale = p.GetAdjustedItemScale(sword);
        Check(Math.Abs(itemScale-2*sword.scale)<.001,"native swing item size doubles at level 10");
        A.ApplyTalent(TalentOperation.DecreaseIntensity,"MeleeRange",TalentCategory.Combat,5);
        Check(Math.Abs(p.GetAdjustedItemScale(sword)-1.5*sword.scale)<.001 && A.State.Talents["MeleeRange"].TalentLevel==10,"active melee level 5 without refund");
        var tag = new TagCompound(); A.SaveData(tag); B.LoadData(tag);
        Check(B.State.Talents["MeleeRange"].CurrentIntensity==5 && TalentCatalog.ValidateImported(B.State),"active intensity survives real player save");
        var npc=Spawn(10000); npc.defense=0;
        foreach(var sample in new[]{ (100d,.9,200), (125d,.1,300), (125d,.5,200), (200d,.5,300), (300d,.5,400) }) {
            var m=npc.GetIncomingStrikeModifiers(DamageClass.Melee,1);
            ExtendedTalentPlayer.ApplyCritTiers(sample.Item1,sample.Item2,ref m);
            Check(m.ToHitInfo(100,false,0,false).Damage==sample.Item3,"native tiered damage at chance "+sample.Item1+" roll "+sample.Item2);
        }
        var blocked=npc.GetIncomingStrikeModifiers(DamageClass.Melee,1);
        ExtendedTalentPlayer.ApplyCritTiers(300,.5,ref blocked); blocked.DisableCrit();
        Check(blocked.ToHitInfo(100,true,0,false).Damage==100,"later DisableCrit overrides guaranteed tiers");
        var summon=npc.GetIncomingStrikeModifiers(DamageClass.Summon,1);
        ExtendedTalentPlayer.ApplyCritTiers(300,.5,ref summon);
        Check(!summon.ToHitInfo(100,false,0,false).Crit,"nonstandard summon classes not forced to crit");
        var hurt=new Player.HurtModifiers(); extended.ModifyHurt(ref hurt);
        Check(Math.Abs(hurt.GetKnockback(10,false)-10*Math.Pow(.95,10))<.001,"native incoming knockback reduction");
        p.immuneTime=40; extended.PostHurt(new Player.HurtInfo{CooldownCounter=-1});
        Check(p.immuneTime==50,"general immunity receives 10 frames");
        p.hurtCooldowns[0]=20; extended.PostHurt(new Player.HurtInfo{CooldownCounter=0});
        Check(p.hurtCooldowns[0]==30 && p.immuneTime==50,"slot immunity does not also extend general counter");
        Player.jumpSpeed=5; Player.jumpHeight=15; p.UpdateJumpHeight();
        Check(Math.Abs(Player.jumpSpeed-6.5)<.001,"jump speed hook multiplies final native speed");
        var v=new Microsoft.Xna.Framework.Vector2(8,-6); var pos=p.Center; int type=ProjectileID.WoodenArrowFriendly, damage=10; float kb=1;
        extended.ModifyShootStats(sword,ref pos,ref v,ref type,ref damage,ref kb);
        Check(v.X==12 && v.Y==-9 && damage==10,"projectile launch speed preserves direction and damage");
        p.inventory[0]=new Item(ItemID.CopperPickaxe); p.selectedItem=0;
        Player.tileRangeX=5; Player.tileRangeY=4; p.blockRange=0; p.breathMax=200; extended.PostUpdateEquips();
        Check(Player.tileRangeX==15 && Player.tileRangeY==14 && p.blockRange==0,"tool reach increases by 10 tiles");
        p.inventory[0]=new Item(ItemID.DirtBlock); Player.tileRangeX=5; Player.tileRangeY=4; p.blockRange=0; p.breathMax=200; extended.PostUpdateEquips();
        Check(Player.tileRangeX==5 && Player.tileRangeY==4 && p.blockRange==10,"placement reach separate from tool reach");
        int mode=Main.GameMode; Main.GameMode=0;
        p.AddBuff(BuffID.Poisoned,600); int buff=p.FindBuffIndex(BuffID.Poisoned);
        Check(buff>=0 && p.buffTime[buff]==(int)(600*Math.Pow(.95,10)),"native AddBuff shortens new debuff once");
        p.AddBuff(BuffID.PotionSickness,3600); buff=p.FindBuffIndex(BuffID.PotionSickness);
        Check(buff>=0 && p.buffTime[buff]==(int)(3600*Math.Pow(.95,10)*Math.Pow(.93,10)),"potion and general duration reductions multiply once");
        p.UpdateBuffs(0);
        Check(p.potionDelay<=1044 && p.potionDelay>0,"actual potion lockout follows shortened buff timer");
        Main.GameMode=mode;
        var sale=new Item(ItemID.CopperBroadsword); sale.value=10000;
        p.GetItemExpectedPrice(sale,out long sell,out long buy);
        Check(sell==15000 && buy==5987,"native coin prices include sell and purchase talents");
        sale.shopSpecialCurrency=1; long specialSell=100, specialBuy=200; NativeTalentHooks.AdjustPrice(p,sale,ref specialSell,ref specialBuy);
        Check(specialSell==100 && specialBuy==200,"special currency untouched");
        int reforge=10000; bool discount=true; new NumericTalentItem().ReforgePrice(sale,ref reforge,ref discount);
        Check(reforge==5987 && discount,"reforge retains vanilla final calculation");
        var fish=new Item(ItemID.Bass); extended.ModifyCaughtFish(fish);
        Check(fish.stack==2,"native caught fish doubles quantity");
        foreach(var it in Main.item) it.active=false;
        npc.lastInteraction=0;
        int normal=Item.NewItem(new EntitySource_Misc("CI discarded"),p.Hitbox,ItemID.IronOre,3);
        Check(Main.item[normal].stack==3,"unattributed ordinary item spawn is not multiplied");
        int loot=Item.NewItem(npc.GetSource_Loot(),p.Hitbox,ItemID.IronOre,3);
        Check(Main.item[loot].stack==6,"NPC loot quantity doubles exactly once");
        int coin=Item.NewItem(npc.GetSource_Loot(),p.Hitbox,ItemID.CopperCoin,10);
        Check(Main.item[coin].stack==20,"NPC coin quantity uses its separate talent");
        Config.BoostMaterials=false;
        int excluded=Item.NewItem(npc.GetSource_Loot(),p.Hitbox,ItemID.IronOre,3);
        Check(Main.item[excluded].stack==3,"server material category switch prevents multiplier");
        Config.BoostMaterials=true;
        EconomySystem.Actor=p; EconomySystem.BreakingTile=TileID.Iron;
        int ore=Item.NewItem(new EntitySource_TileBreak(Main.spawnTileX,Main.spawnTileY),p.Hitbox,ItemID.IronOre,3);
        EconomySystem.Actor=null; EconomySystem.BreakingTile=-1;
        Check(Main.item[ore].stack==6,"attributed ore source doubles without NPC multiplier");
        Check(EconomySystem.Resource(TileID.Trees,new Item(ItemID.Wood))=="WoodYield" && EconomySystem.Resource(TileID.Trees,new Item(ItemID.Acorn))==null,"tree wood classifier excludes acorns");
        Check(EconomySystem.Resource(TileID.BloomingHerbs,new Item(ItemID.Daybloom))=="HerbYield" && EconomySystem.Resource(TileID.ExposedGems,new Item(ItemID.Ruby))=="GemYield","herb and gem families classified");
        int before=Main.item.Where(i=>i.active&&i.type==ItemID.Wood).Sum(i=>i.stack);
        var rule=new CommonDrop(ItemID.Wood,1,3,3);
        var info=new DropAttemptInfo{npc=npc,player=p,rng=new Terraria.Utilities.UnifiedRandom(123)};
        var dropResult=rule.TryDroppingItem(info);
        int after=Main.item.Where(i=>i.active&&i.type==ItemID.Wood).Sum(i=>i.stack);
        Check(dropResult.State==ItemDropAttemptResultState.Success && after-before==6,"actual CommonDrop path preserves successful quantity boost");
        // Real mining call proves scope attribution and restoration, not just classification.
        int x=Main.spawnTileX+12, y=Main.spawnTileY-5;
        // Isolate the fixture from randomly generated trees/chests supported above it.
        for(int dx=-2;dx<=2;dx++) for(int dy=-3;dy<=2;dy++) Main.tile[x+dx,y+dy].ClearEverything();
        Main.tile[x,y].ResetToType(TileID.Iron);
        int priorOre=Main.item.Where(i=>i.active&&i.type==ItemID.IronOre).Sum(i=>i.stack);
        p.PickTile(x,y,10000);
        int finalOre=Main.item.Where(i=>i.active&&i.type==ItemID.IronOre).Sum(i=>i.stack);
        Check(!Main.tile[x,y].HasTile && finalOre-priorOre==2 && EconomySystem.Actor==null && EconomySystem.BreakingTile==-1,$"real mining doubles ore and restores scopes: tile={Main.tile[x,y].HasTile}, ore={finalOre-priorOre}");
        Main.netMode=NetmodeID.Server;
        int privateItem=Item.NewItem(npc.GetSource_Loot(),p.Hitbox,ItemID.IronOre,3,noBroadcast:true);
        Check(Main.item[privateItem].stack==3,"private server spawns are not expanded into public bonuses");
        npc.playerInteraction[0]=true;
        CommonCode.DropItemLocalPerClientAndSetNPCMoneyTo0(npc,ItemID.KingSlimeBossBag,1);
        Check(!EconomySystem.SpawningBonus,"instanced boss bag path restores suppression scope");
        Main.netMode=NetmodeID.MultiplayerClient;
        int clientItem=Item.NewItem(npc.GetSource_Loot(),p.Hitbox,ItemID.IronOre,3);
        Check(Main.item[clientItem].stack==3,"client NPC spawn cannot apply server loot multiplier");
        Main.netMode=NetmodeID.SinglePlayer;
        for(int i=0;i<Main.maxItems;i++) { Main.item[i].active=false; Main.timeItemSlotCannotBeReusedFor[i]=90; }
        var overflow=new Item(ItemID.CopperBroadsword);
        EconomySystem.BoostWorld(overflow,new EntitySource_Misc("CI capacity"),10);
        Check(overflow.stack==1 && !Main.item.Take(Main.maxItems).Any(i=>i.active) && Main.timeItemSlotCannotBeReusedFor.Take(Main.maxItems).All(t=>t==90),"bonus overflow preserves inactive but reserved private item slots");
        for(int i=0;i<Main.maxItems;i++) Main.timeItemSlotCannotBeReusedFor[i]=0;
        RunCopperMining();
        RunP1Refinement();
        RunP2();
        RunP2B();
        RunP2C();
        RunP2Completion();
        RunP2MenuFishing();
        RunMenuLifecycle();
        RunP2Afflictions();
        var p3Errors = new System.Collections.Generic.List<Exception>();
        try { RunP3Gathering(); } catch (Exception e) { p3Errors.Add(e); }
        try { RunP3Agriculture(); } catch (Exception e) { p3Errors.Add(e); }
        try { RunBulkUpgrades(); } catch (Exception e) { p3Errors.Add(e); }
        try { RunFlexibleRange(); } catch (Exception e) { p3Errors.Add(e); }
        try { RunToolPowerBlast(); } catch (Exception e) { p3Errors.Add(e); }
        try { RunResourcesLimits(); } catch (Exception e) { p3Errors.Add(e); }
        try { RunSharedExperience(); } catch (Exception e) { p3Errors.Add(e); }
        if (p3Errors.Count > 0) throw new AggregateException("P3 verification failed", p3Errors);
    }
    private void RunCopperMining()
    {
        Reset(); A.Award(1000000 * Experience.Scale);
        var p = A.Player;
        p.inventory[0] = new Item(ItemID.CopperPickaxe); p.selectedItem = 0;
        Check(A.ApplyTalent(TalentOperation.Upgrade, "MiningYield", TalentCategory.Economy, 10) == TalentResult.Success,
            "purchase only mining yield, without other quantity talents");
        int x = Main.spawnTileX + 16, y = Main.spawnTileY - 5;
        for(int dx=-2;dx<=2;dx++) for(int dy=-3;dy<=2;dy++) Main.tile[x+dx,y+dy].ClearEverything();
        foreach (var sample in new[] { (TileID.Copper, ItemID.CopperOre), (TileID.Tin, ItemID.TinOre),
            (TileID.Iron, ItemID.IronOre), (TileID.Lead, ItemID.LeadOre) }) {
            foreach (bool enabled in new[] { true, false, true }) {
                A.ApplyTalent(enabled ? TalentOperation.Enable : TalentOperation.Disable, "MiningYield", TalentCategory.Economy, 1);
                foreach (var item in Main.item) item.active = false;
                Main.tile[x,y].ResetToType(sample.Item1);
                int hits = 0;
                while (Main.tile[x,y].HasTile && hits++ < 20) p.PickTile(x,y,p.HeldItem.pick);
                int amount = Main.item.Where(i => i.active && i.type == sample.Item2).Sum(i => i.stack);
                Check(!Main.tile[x,y].HasTile && hits > 1 && amount == (enabled ? 2 : 1),
                    $"ordinary copper pick repeated hits: tile={sample.Item1}, enabled={enabled}, amount={amount}, hits={hits}");
                Check(EconomySystem.Actor == null && EconomySystem.BreakingTile == -1,
                    "resource scopes restored after partial and final mining hits");
            }
        }
    }
    private void RunTalents()
    {
        Reset(); A.Award(100000000 * Experience.Scale);
        foreach (var def in NumericTalents.All) Check(A.ApplyTalent(TalentOperation.Upgrade, def.Id, def.Category, 10) == TalentResult.Success, "purchase 10 levels: " + def.Id);
        var p = A.Player;
        var effects = p.GetModPlayer<NumericTalentPlayer>();
        p.statLife = 60; p.statMana = 10;
        p.ResetEffects();
        Check(p.statLifeMax2 == 350 && p.statManaMax2 == 220, "native ResetEffects / ModifyMaxStats gives +250 HP +200 MP");
        Check(p.statLife == 60 && p.statMana == 10, "max stat upgrades do not restore resources");
        PlayerLoader.PostUpdateEquips(p);
        Check((int)p.statDefense == 40 && Math.Abs(p.GetDamage(DamageClass.Generic).ApplyTo(100)-150) < .01, "native defense and global damage are applied");
        Check(Math.Abs(p.GetAttackSpeed(DamageClass.Generic)-1.3f)<.001 && p.GetArmorPenetration(DamageClass.Generic)==30, "native generic attack speed and armor penetration");
        Check(p.maxMinions == 11 && p.maxTurrets == 11, "minion / sentry capacity additive");
        Check(Math.Abs(p.manaCost - Math.Pow(.95,10)) < .001, "native mana cost multiplication");
        p.maxRunSpeed = 3; p.accRunSpeed = 6; p.runAcceleration = .08f;
        effects.PostUpdateRunSpeeds();
        Check(Math.Abs(p.maxRunSpeed-4.5f)<.001 && Math.Abs(p.accRunSpeed-9f)<.001 && Math.Abs(p.runAcceleration-.12f)<.001, "run speed and acceleration keep independent multipliers");
        p.lifeRegen = 0; effects.UpdateLifeRegen(); float regen = 4; effects.NaturalLifeRegen(ref regen);
        Check(p.lifeRegen == 20 && regen == 8, "10 HP/sec fixed and +100% natural life regen");
        int healing = 100, manaHealing = 100;
        effects.GetHealLife(new Item(), false, ref healing); effects.GetHealMana(new Item(),false,ref manaHealing);
        Check(healing==150 && manaHealing==150,"healing item modifiers +50%");
        var npc = Spawn(1000); npc.defense = 0; var modifiers = npc.GetIncomingStrikeModifiers(DamageClass.Generic,0);
        effects.ModifyHitNPC(npc,ref modifiers);
        var hit = modifiers.ToHitInfo(100,true,0,false);
        Check(hit.Damage==250,"native critical damage grows x2 to x2.5 at level 10");
        p.statMana=0; p.manaRegen=0;
        for(int i=0;i<60;i++) effects.PostUpdate();
        Check(p.statMana==20,"fixed mana recovers 20 per second at level 10 during regen delay");
        A.ApplyTalent(TalentOperation.Disable,"ManaRegen",TalentCategory.Recovery,1);
        p.statMana=0; p.manaRegen=24;
        for(int i=0;i<60;i++) effects.PostUpdate();
        Check(p.statMana==12,"natural mana adds 100% of vanilla 12 MP/sec at level 10");
        var tag = new TagCompound(); A.SaveData(tag); B.LoadData(tag);
        Check(B.State.Talents.Count==NumericTalents.All.Count && !B.State.Talents["ManaRegen"].Enabled,"real SaveData / LoadData preserve all talents and toggle state");
        p.dead=true; PlayerLoader.UpdateDead(p); p.dead=false;
        Check(A.State.Talents.Count==NumericTalents.All.Count && A.State.TotalSpentTalentPoints==10*NumericTalents.All.Count,"death retains purchased talent levels and invested points");
        A.ApplyTalent(TalentOperation.DisableEverything,"",TalentCategory.BaseStats,1);
        p.statLife=300; p.statMana=200; p.ResetEffects(); effects.ClampResources();
        Check(p.statLife==100 && p.statMana==20,"disabling maximum stats clips resources without damage or healing");
        A.ApplyTalent(TalentOperation.EnableEverything,"",TalentCategory.BaseStats,1);
        p.ResetEffects(); effects.ClampResources();
        Check(p.statLife==100 && p.statMana==20 && p.statLifeMax2==350,"reenabling maximum stats does not refill resources");
        A.ApplyTalent(TalentOperation.RefundEverything,"",TalentCategory.BaseStats,1);
        p.ResetEffects(); PlayerLoader.PostUpdateEquips(p);
        Check(A.State.TotalSpentTalentPoints==0 && p.statLifeMax2==100 && p.maxMinions==1,"refund all removes effects after native reset");

        Reset(); A.Award(10000*Experience.Scale);
        Main.netMode=NetmodeID.Server;
        ulong revision = A.TalentRevision;
        SendTalentAction(A.SessionId,revision,TalentOperation.Upgrade,"MaxLife",1);
        Check(A.State.Talents["MaxLife"].TalentLevel==1 && A.TalentRevision==revision+1,"server applies bounded talent intent");
        A.HasActionTick=false;
        SendTalentAction(A.SessionId,revision,TalentOperation.Upgrade,"MaxLife",1);
        Check(A.State.Talents["MaxLife"].TalentLevel==1,"duplicate revision cannot spend twice");
        Check(((RecordingSocket)Netplay.Clients[0].Socket).Sent > 0, "rejected request sends a real ModPacket response");
        A.HasActionTick=false;
        SendTalentAction(Guid.NewGuid(),A.TalentRevision,TalentOperation.RefundTalent,"MaxLife",1);
        Check(A.State.Talents.ContainsKey("MaxLife"),"old session token cannot refund current character");
        A.HasActionTick=false;
        SendTalentAction(A.SessionId,A.TalentRevision,TalentOperation.Upgrade,"Unknown",1);
        Check(A.State.Talents.Count==1,"server rejects unregistered talent request");
        Main.netMode=NetmodeID.MultiplayerClient;
        Check(A.ApplyTalent(TalentOperation.Upgrade,"MaxMana",TalentCategory.BaseStats,1)==TalentResult.NotReady,"client cannot apply a local authoritative mutation");
        var retained=A.State;
        Main.netMode=NetmodeID.Server; A.SessionReady=false;
        Receive(1,retained);
        Check(A.SessionReady && A.State.Talents["MaxLife"].TalentLevel==1,"registered talents import once on multiplayer join");
    }
    private void SendTalentAction(Guid token, ulong revision, TalentOperation operation, string id, int count)
    {
        using var stream=new MemoryStream();
        using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true)) {
            writer.Write(ProgressionNetwork.ProtocolVersion); writer.Write((byte)ProgressionMessage.TalentAction);
            writer.Write(token.ToByteArray()); writer.Write(revision); writer.Write((uint)1);
            writer.Write((byte)operation); writer.Write(id); writer.Write((byte)TalentCategory.BaseStats); writer.Write((ushort)count);
        }
        stream.Position=0; ProgressionNetwork.Receive(new BinaryReader(stream),0);
    }
    private void Receive(byte kind, ProgressionState state, bool padding = false)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)) {
            byte[] bytes = StateCodec.Encode(state);
            writer.Write(ProgressionNetwork.ProtocolVersion); writer.Write(kind);
            if (kind == 2) {
                writer.Write((byte)0); writer.Write(A.SessionId.ToByteArray()); writer.Write(A.TalentRevision);
                writer.Write((uint)0); writer.Write((byte)TalentResult.Success);
            }
            writer.Write((ushort)bytes.Length); writer.Write(bytes);
            if (padding) writer.Write(new byte[64]); // tML's underlying reader has bytes beyond the packet.
        }
        stream.Position = 0;
        ProgressionNetwork.Receive(new BinaryReader(stream), 0);
    }
    private void ServerStrike(NPC npc, int sender, int damage)
    {
        Main.netMode = NetmodeID.Server;
        Netplay.Clients[sender].State = 10;
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)) {
            writer.Write((short)npc.whoAmI);
            writer.Write7BitEncodedInt(damage); writer.Write7BitEncodedInt(damage);
            writer.Write7BitEncodedInt(DamageClass.Generic.Type);
            writer.Write((sbyte)0); writer.Write(0f); writer.Write((byte)4);
        }
        stream.Position = 0;
        var reader = new BinaryReader(stream);
        byte messageType = 28;
        bool hijacked = ModContent.GetInstance<StrikeObserver>().HijackGetData(ref messageType, ref reader, sender);
        Check(!hijacked && stream.Position == 0, "strike observer preserves vanilla packet");
        npc.StrikeNPC(Hit(damage), fromNet: true);
        Netplay.Clients[sender].State = 0;
    }
}

// Implements tML's public transport interface for the disposable simulated clients.
// This receives actual ModPacket bytes; no production packet path is bypassed.
internal sealed class RecordingSocket : Terraria.Net.Sockets.ISocket
{
    internal byte[] LastPacket = Array.Empty<byte>();
    internal int Sent;
    public void AsyncSend(byte[] data, int offset, int size, Terraria.Net.Sockets.SocketSendCallback callback, object state = null!)
    {
        LastPacket = data.AsSpan(offset,size).ToArray(); Sent++; callback(state);
    }
    public void AsyncReceive(byte[] data, int offset, int size, Terraria.Net.Sockets.SocketReceiveCallback callback, object state = null!) { }
    public void Close() { }
    public void Connect(Terraria.Net.RemoteAddress address) { }
    public bool IsConnected() => true;
    public bool IsDataAvailable() => false;
    public void SendQueuedPackets() { }
    public bool StartListening(Terraria.Net.Sockets.SocketConnectionAccepted callback) => false;
    public void StopListening() { }
    public Terraria.Net.RemoteAddress GetRemoteAddress() => new Terraria.Net.TcpAddress(System.Net.IPAddress.Loopback, 7789);
}
