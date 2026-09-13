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

namespace ProgressionHarness;

public sealed class ProgressionHarness : Mod { }

public sealed class CiCommand : ModCommand
{
    public override string Command => "tpci";
    public override CommandType Type => CommandType.Console;
    public override void Action(CommandCaller caller, string input, string[] args) => ModContent.GetInstance<RuntimeChecks>().RunFromConsole();
}

public sealed class RuntimeChecks : ModSystem
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
            Main.player[i].dead = false;
            Main.player[i].GetModPlayer<ProgressionPlayer>().Initialize();
            Main.player[i].GetModPlayer<ProgressionPlayer>().SessionReady = true;
        }
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
    }
    private void RunTalents()
    {
        Reset(); A.Award(10000000 * Experience.Scale);
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
        Check(B.State.Talents.Count==21 && !B.State.Talents["ManaRegen"].Enabled,"real SaveData / LoadData preserve all talents and toggle state");
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
            writer.Write((byte)operation); writer.Write(id); writer.Write((byte)TalentCategory.BaseStats); writer.Write((byte)count);
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
