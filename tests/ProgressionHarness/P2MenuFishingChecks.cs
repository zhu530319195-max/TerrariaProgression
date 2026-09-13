using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using TerrariaProgression.Core;
using TerrariaProgression.Diagnostics;
using TerrariaProgression.Players;
using TerrariaProgression.Networking;

namespace ProgressionHarness;

// A loaded third-party-shaped candidate proves the scanner never runs behavior.
public sealed class UnmappedScannerAccessory : ModItem
{
    public static int EffectsCalled;
    public override string Texture => "Terraria/Images/Item_1";
    public override void SetDefaults() {Item.accessory=true;Item.width=16;Item.height=16;}
    public override void UpdateAccessory(Player player,bool hideVisual) => EffectsCalled++;
}

public sealed partial class RuntimeChecks
{
    private void RunP2MenuFishing()
    {
        Reset();A.Award(100000000*Experience.Scale);
        var p=A.Player;var fishing=p.GetModPlayer<FishingTalentPlayer>();
        var oldRandom=Main.rand;
        void Toggle(string id,bool on)=>A.ApplyTalent(on?TalentOperation.Enable:TalentOperation.Disable,id,TalentCategory.Economy,1);
        foreach(var id in new[]{"BaitSaving","CrateChance"})
            Check(A.ApplyTalent(TalentOperation.Upgrade,id,TalentCategory.Economy,10)==TalentResult.Success,"native new fishing purchase: "+id);
        var bobber=new Projectile{owner=0};bobber.localAI[1]=ItemID.Bass;
        var consume=typeof(Player).GetMethod("ItemCheck_CheckFishingBobber_PickAndConsumeBait",BindingFlags.Instance|BindingFlags.NonPublic)!;
        int Used(bool enabled,bool tackle) {
            Toggle("BaitSaving",enabled);p.accTackleBox=tackle;
            p.inventory[1]=new Item(ItemID.MasterBait){stack=10000};Main.rand=new UnifiedRandom(28081);
            for(int i=0;i<8000;i++)consume.Invoke(p,new object[]{bobber,false,0});
            return 10000-p.inventory[1].stack;
        }
        int baseline=Used(false,false),saved=Used(true,false),tackleBase=Used(false,true),tackleSaved=Used(true,true);
        Console.WriteLine($"FISHING_EVIDENCE bait baseline={baseline}, saving10={saved}, tackle={tackleBase}, tackle+saving10={tackleSaved}");
        Check(baseline>500 && saved>baseline*.38 && saved<baseline*.59,"actual native bait consumption falls by expected level-ten probability");
        Check(tackleBase<baseline && tackleSaved>tackleBase*.38 && tackleSaved<tackleBase*.59,"bait conservation multiplies native tackle-box conservation");
        Check(Used(false,false)==baseline,"disabling bait talent restores identical native seeded result");
        Toggle("BaitSaving",true);
        A.ApplyTalent(TalentOperation.DecreaseIntensity,"BaitSaving",TalentCategory.Economy,10);
        Check(Used(true,false)==baseline,"zero active bait strength preserves native result and RNG");
        A.ApplyTalent(TalentOperation.MaximumIntensity,"BaitSaving",TalentCategory.Economy,1);
        p.inventory[1]=new Item();var noBait=new object[]{bobber,false,0};consume.Invoke(p,noBait);
        Check(!(bool)noBait[1] && (int)noBait[2]==0,"conservation never supplies missing bait");
        Main.netMode=NetmodeID.Server;
        Check(fishing.CanConsumeBait(new Item(ItemID.MasterBait))==null,"server copy does not repeat owner bait roll");
        Main.netMode=NetmodeID.SinglePlayer;Main.myPlayer=1;
        Check(fishing.CanConsumeBait(new Item(ItemID.MasterBait))==null,"spectator copy does not repeat bait roll");Main.myPlayer=0;
        A.SessionReady=false;Check(fishing.CanConsumeBait(new Item(ItemID.MasterBait))==null,"unconfirmed character cannot apply bait talent");A.SessionReady=true;

        var roll=typeof(Projectile).GetMethod("FishingCheck_RollDropLevels",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var drop=typeof(Projectile).GetMethod("FishingCheck_RollItemDrop",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Main.bloodMoon=false;Main.hardMode=false;p.cratePotion=false;
        int Crates(bool enabled,bool potion) {
            Toggle("CrateChance",enabled);p.cratePotion=potion;Main.rand=new UnifiedRandom(81008);int crates=0;
            for(int i=0;i<6000;i++) {
                object[] args={100,false,false,false,false,false,false};roll.Invoke(bobber,args);
                var attempt=new FishingAttempt{fishingLevel=100,waterTilesCount=1000,waterNeededToFish=300,heightLevel=1,questFish=-1,
                    common=(bool)args[1],uncommon=(bool)args[2],rare=(bool)args[3],veryrare=(bool)args[4],legendary=(bool)args[5],crate=(bool)args[6]};
                PlayerLoader.ModifyFishingAttempt(p,ref attempt);
                object[] result={attempt};drop.Invoke(bobber,result);attempt=(FishingAttempt)result[0];
                if(attempt.rolledItemDrop>0 && ItemID.Sets.IsFishingCrate[attempt.rolledItemDrop])crates++;
            }
            return crates;
        }
        int baseCrates=Crates(false,false),boostCrates=Crates(true,false),potionBase=Crates(false,true),potionBoost=Crates(true,true);
        Console.WriteLine($"FISHING_EVIDENCE crates/6000 baseline={baseCrates}, talent10={boostCrates}, potion={potionBase}, potion+talent10={potionBoost}");
        Check(baseCrates>480&&baseCrates<720&&boostCrates>2550&&boostCrates<3000,"native item selection yields approximately 10 to 46 percent ordinary-water crates");
        Check(potionBase>1350&&potionBase<1650&&potionBoost>3100&&potionBoost<3500,"crate talent preserves native potion and multiplies remaining failure");
        Check(Crates(false,false)==baseCrates,"disabled crate talent restores native seeded item result");
        Toggle("CrateChance",true);
        A.State.RefundAll("CrateChance");A.State.Invest("CrateChance",1,1000);
        var gate=new FishingAttempt{crate=false,rare=true,rolledEnemySpawn=NPCID.Zombie,rolledItemDrop=ItemID.Bass,fishingLevel=75};
        fishing.ModifyFishingAttempt(ref gate);
        Check(gate.crate&&gate.rare&&gate.rolledEnemySpawn==NPCID.Zombie&&gate.rolledItemDrop==ItemID.Bass&&gate.fishingLevel==75,"crate enhancement changes only crate eligibility");
        object[] enemyArgs={gate};drop.Invoke(bobber,enemyArgs);
        Check(((FishingAttempt)enemyArgs[0]).rolledEnemySpawn==NPCID.Zombie && ((FishingAttempt)enemyArgs[0]).rolledItemDrop==ItemID.Bass,"existing enemy catch remains native and is not replaced");
        var honey=new FishingAttempt{inHoney=true};fishing.ModifyFishingAttempt(ref honey);Check(!honey.crate,"honey does not gain unsupported crates");
        var lava=new FishingAttempt{inLava=true,CanFishInLava=false};fishing.ModifyFishingAttempt(ref lava);
        object[] lavaArgs={lava};drop.Invoke(bobber,lavaArgs);Check(((FishingAttempt)lavaArgs[0]).rolledItemDrop==0,"crate talent cannot grant lava-fishing eligibility");
        var remote=new FishingAttempt();Main.netMode=NetmodeID.Server;fishing.ModifyFishingAttempt(ref remote);Check(!remote.crate,"server copy does not reroll fishing eligibility");Main.netMode=NetmodeID.SinglePlayer;
        var tag=new TagCompound();A.SaveData(tag);B.LoadData(tag);
        Check(B.State.Talents["CrateChance"].TalentLevel==1000&&B.State.Talents["BaitSaving"].TalentLevel==10,"new fishing entries persist through actual save hooks");

        var before=StateCodec.Encode(A.State);int effects=UnmappedScannerAccessory.EffectsCalled;
        var rows=AccessoryTalentScanner.Scan();
        Check(rows.Count==ContentSamples.ItemsByType.Count(pair=>pair.Key>0&&pair.Value.accessory),"scanner enumerates every loaded accessory sample");
        Check(rows.Single(r=>r.ItemId==ItemID.LuckyHorseshoe).MappingStatus=="Mapped","known horseshoe maps to registered fall immunity");
        Check(rows.Single(r=>r.ItemId==ItemID.AnkhShield).MappingStatus=="Partial","composite with unreviewed effects is explicitly partial");
        Check(rows.Single(r=>r.ItemId==ModContent.ItemType<UnmappedScannerAccessory>()).MappingStatus=="Unmapped","third-party candidate stays unmapped");
        Check(rows.All(r=>r.TalentIds.All(id=>TalentCatalog.TryGet(id,out _))),"scanner has no dangling talent mappings");
        Check(before.SequenceEqual(StateCodec.Encode(A.State))&&effects==UnmappedScannerAccessory.EffectsCalled,"passive scan neither changes progression nor executes accessory logic");
        string temp=Path.Combine(Path.GetTempPath(),"tp-scanner-"+Guid.NewGuid());
        try {
            string report=AccessoryTalentScanner.WriteReport(temp);
            using var json=JsonDocument.Parse(File.ReadAllText(report+".json"));
            Check(json.RootElement.GetProperty("Items").GetArrayLength()==rows.Count && File.ReadAllText(report+".csv").Contains("UnmappedScannerAccessory"),"JSON and UTF8 CSV contain full candidate report");
            Check(json.RootElement.GetProperty("Registry").GetArrayLength()==81,"compatibility report includes all 81 registered talents");
        } finally {Directory.Delete(temp,true);}

        // New refund intents pass through the real server decoder; bad scope and
        // stale request must not widen a refund beyond the selected group.
        Main.netMode=NetmodeID.SinglePlayer;A.State.Invest("MaxLife",7);A.State.Invest("FlightTime",5);
        var invested=A.State.TotalSpentTalentPoints;var amount=TalentNavigation.RefundAmount(A.State,"Fishing",true);
        Main.netMode=NetmodeID.Server;A.HasActionTick=false;
        SendTalentAction(A.SessionId,A.TalentRevision,TalentOperation.RefundMenuGroup,"Fishing",1);
        Check(!A.State.Talents.ContainsKey("BaitSaving")&&!A.State.Talents.ContainsKey("CrateChance")&&A.State.Talents.ContainsKey("MaxLife")&&A.State.TotalSpentTalentPoints==invested-amount,"server refunds exactly new Fishing subgroup at paid cost");
        before=StateCodec.Encode(A.State);A.HasActionTick=false;
        SendTalentAction(A.SessionId,A.TalentRevision,TalentOperation.RefundMenuGroup,"Survival",2);
        Check(before.SequenceEqual(StateCodec.Encode(A.State)),"server rejects category disguised as subgroup");
        A.HasActionTick=false;SendTalentAction(A.SessionId,A.TalentRevision,TalentOperation.RefundMenuCategory,"Survival",3);
        Check(!A.State.Talents.ContainsKey("MaxLife")&&A.State.Talents.ContainsKey("FlightTime"),"server category refund preserves other menu categories");
        Main.rand=oldRandom;Reset();
    }
}
