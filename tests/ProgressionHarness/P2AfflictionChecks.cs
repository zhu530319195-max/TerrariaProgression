using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariaProgression.Core;
using TerrariaProgression.Diagnostics;
using TerrariaProgression.NPCs;
using TerrariaProgression.Players;
using TerrariaProgression.Networking;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace ProgressionHarness;

public sealed partial class RuntimeChecks
{
    private void RunP2Afflictions()
    {
        var oldText = Main.combatText;
        Main.combatText = Enumerable.Range(0,100).Select(_=>new CombatText{active=true}).ToArray();
        var flags = typeof(NPC).GetMethod("UpdateNPC_BuffFlagsReset",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var dot = typeof(NPC).GetMethod("UpdateNPC_BuffApplyDOTs",BindingFlags.Instance|BindingFlags.NonPublic)!;
        void SetTalent(ProgressionPlayer p,string id,int level) {
            p.State.RefundAll(id); if(level>0&&!p.State.Invest(id,1,level))throw new Exception("Insufficient fixture points for "+id);
        }
        NPC Target() {
            var n=Spawn(100000);foreach(var e in AfflictionNpc.Effects)n.buffImmune[e.Buff]=false;return n;
        }
        void ItemHit(NPC n) {
            var hit=Hit(10);int dealt=n.StrikeNPC(hit);
            // Match native melee dispatch: generic ApplyDamageToNPC deliberately
            // omits item-specific hooks, including the existing XP attribution hook.
            CombinedHooks.OnPlayerHitNPCWithItem(A.Player,new Item(ItemID.WoodenSword),n,in hit,dealt);
        }
        void Flags(NPC n) {flags.Invoke(n,null);n.UpdateNPC_BuffSetFlags(false);}
        int Bonus(NPC n,int ticks=5) {
            int sum=0;for(int i=0;i<ticks;i++){Flags(n);n.lifeRegen=0;int d=0;NPCLoader.UpdateLifeRegen(n,ref d);sum-=n.lifeRegen;}return sum;
        }
        int DamageTicks(NPC n,int ticks=120) {
            int before=n.life;
            for(int i=0;i<ticks && n.active;i++){Flags(n);n.lifeRegen=0;dot.Invoke(n,null);}
            return before-n.life;
        }
        try {
            foreach(var e in AfflictionNpc.Effects) {
                Reset();A.Award(100000000*Experience.Scale);
                SetTalent(A,e.Talent,10);SetTalent(A,"AfflictionDamage",10);
                var native=Target();native.AddBuff(e.Buff,1200);int normal=DamageTicks(native,600);
                Check(Bonus(native)==0,"native-only status never acquires a talent damage source: "+e.Talent);
                var enhanced=Target();AfflictionNpc.ApplyHit(enhanced,A.Player,10);
                Check(enhanced.HasBuff(e.Buff)&&enhanced.buffTime[enhanced.FindBuffIndex(e.Buff)]==1200,"native level-ten status has twenty-second duration: "+e.Talent);
                int increased=DamageTicks(enhanced,600);
                Console.WriteLine($"AFFLICTION_EVIDENCE {e.Talent}: native/600ticks={normal}, boost10={increased}");
                Check(normal==e.Regen*5 && increased==normal*3,"actual native DoT health loss triples at level ten (Ichor stays zero): "+e.Talent);
                if(e.Regen>0) {
                    SetTalent(A,"AfflictionDamage",1);enhanced.lifeRegenCount=0;
                    int fractionalLoss=DamageTicks(enhanced,600);
                    Check(fractionalLoss*120-enhanced.lifeRegenCount==e.Regen*6*120,"fractional level-one bonus preserves twenty percent including native popup carry: "+e.Talent);
                } else {
                    var before=enhanced.GetIncomingStrikeModifiers(DamageClass.Generic,1).Defense.Flat;
                    SetTalent(A,"AfflictionDamage",100);Flags(enhanced);
                    Check(enhanced.GetIncomingStrikeModifiers(DamageClass.Generic,1).Defense.Flat==before,"Ichor defense effect does not scale with damage talent");
                }
                var blocked=Target();blocked.buffImmune[e.Buff]=true;AfflictionNpc.ApplyHit(blocked,A.Player,10);
                Check(!blocked.HasBuff(e.Buff)&&Bonus(blocked)==0,"native immunity also prevents a source lease: "+e.Talent);
                var one=Target();SetTalent(A,e.Talent,1);AfflictionNpc.ApplyHit(one,A.Player,10);
                Check(one.buffTime[one.FindBuffIndex(e.Buff)]==120,"native level-one status lasts two seconds: "+e.Talent);
                AfflictionNpc.ApplyHit(one,A.Player,10);Check(one.buffTime[one.FindBuffIndex(e.Buff)]==120,"refresh does not add duration: "+e.Talent);
            }
            Reset();A.Award(100000000*Experience.Scale);B.Award(100000000*Experience.Scale);
            SetTalent(A,"AttackBurn",10);SetTalent(B,"AttackBurn",10);SetTalent(A,"AfflictionDamage",10);SetTalent(B,"AfflictionDamage",5);
            var target=Target();ServerStrike(target,0,10);ServerStrike(target,1,10);
            Check(target.HasBuff(BuffID.OnFire)&&Bonus(target)==80,"confirmed server strikes select strongest source without stacking");
            byte[] Snapshot(NPC n) {
                using var data=new MemoryStream();using var dataWriter=new BinaryWriter(data);
                var bits=new BitWriter();n.GetGlobalNPC<AfflictionNpc>().SendExtraAI(n,bits,dataWriter);
                using var wire=new MemoryStream();using var wireWriter=new BinaryWriter(wire);bits.Flush(wireWriter);wireWriter.Write(data.ToArray());return wire.ToArray();
            }
            void ReceiveSnapshot(NPC n,byte[] bytes) {
                using var stream=new MemoryStream(bytes);using var reader=new BinaryReader(stream);
                var bits=new BitReader(reader);n.GetGlobalNPC<AfflictionNpc>().ReceiveExtraAI(n,bits,reader);
            }
            byte[] serverRates=Snapshot(target);var replica=Target();replica.AddBuff(BuffID.OnFire,1200);
            Main.netMode=NetmodeID.MultiplayerClient;ReceiveSnapshot(replica,serverRates);
            Check(Bonus(replica)==80,"authoritative NPC extra-AI snapshot reproduces client damage rate");
            Main.netMode=NetmodeID.Server;var spoofed=Target();spoofed.AddBuff(BuffID.OnFire,1200);ReceiveSnapshot(spoofed,serverRates);
            Check(Bonus(spoofed)==0,"server ignores received client-shaped damage-rate snapshots");
            A.State.Talents["AfflictionDamage"].CurrentIntensity=1;
            Check(Bonus(target)==40,"server source follows current intensity and falls back to stronger remaining player");
            A.State.Talents["AfflictionDamage"].CurrentIntensity=null;B.State.Talents["AfflictionDamage"].Enabled=false;
            Check(Bonus(target)==80,"weaker source disable preserves stronger source");
            A.State.Talents["AfflictionDamage"].Enabled=false;
            Check(Bonus(target)==0&&target.HasBuff(BuffID.OnFire),"disabling all source boosts leaves native status intact");
            byte[] clearedRates=Snapshot(target);Main.netMode=NetmodeID.MultiplayerClient;ReceiveSnapshot(replica,clearedRates);
            Check(Bonus(replica)==0&&replica.HasBuff(BuffID.OnFire),"zero-rate snapshot clears client boost without clearing native buff");
            Main.netMode=NetmodeID.Server;
            A.State.Talents["AfflictionDamage"].Enabled=true;
            Check(Bonus(target)==80,"reenable within original lease restores current bonus");
            A.State.RefundAll("AfflictionDamage");Check(Bonus(target)==0&&target.HasBuff(BuffID.OnFire),"refund stops bonus without removing native burn");
            SetTalent(A,"AfflictionDamage",10);A.PlayerDisconnect();
            Check(Bonus(target)==0,"disconnected session is removed from ongoing damage");
            A.SessionReady=true;Check(Bonus(target)==0,"new session cannot inherit previous player-slot source");
            AfflictionNpc.ApplyHit(target,A.Player,10);Check(Bonus(target)==80,"new verified hit creates a new session lease");
            target.DelBuff(target.FindBuffIndex(BuffID.OnFire));Check(Bonus(target)==0,"native removal clears source");
            target.AddBuff(BuffID.OnFire,3600);Check(Bonus(target)==0,"later weapon refresh cannot resurrect removed source");
            AfflictionNpc.ApplyHit(target,A.Player,10);
            target.SetDefaults(NPCID.BlueSlime);target.active=true;target.life=10000;target.buffImmune[BuffID.OnFire]=false;target.AddBuff(BuffID.OnFire,1200);
            Check(Bonus(target)==0,"NPC SetDefaults slot reuse discards old sources");
            AfflictionNpc.ApplyHit(target,A.Player,10);ModContent.GetInstance<EncounterSystem>().ClearWorld();
            Check(Bonus(target)==0,"world clear invalidates source state on surviving references");

            Main.netMode=NetmodeID.SinglePlayer;target=Target();
            foreach(var e in AfflictionNpc.Effects)SetTalent(A,e.Talent,10);
            ItemHit(target);
            Check(AfflictionNpc.Effects.All(e=>target.HasBuff(e.Buff)),"real player hit applies all six native statuses with native coexistence");
            foreach(var q in Main.projectile)q.active=false;foreach(var n in Main.npc)n.active=false;var minionTarget=Target();
            int shot=Projectile.NewProjectile(A.Player.GetSource_Misc("affliction minion test"),minionTarget.Center,Vector2.Zero,ProjectileID.BabySlime,10,0,0);
            // Native slime AI enables contact damage only after choosing a target.
            // Start this collision fixture in that attacking state.
            Main.projectile[shot].friendly=true;Main.projectile[shot].Damage();
            Check(Main.projectile[shot].minion && AfflictionNpc.Effects.All(e=>minionTarget.HasBuff(e.Buff)),"real owned summon collision applies all attack afflictions");
            var clientOnly=Target();Main.netMode=NetmodeID.MultiplayerClient;
            A.Player.GetModPlayer<CombatUtilityPlayer>().OnHitNPC(clientOnly,Hit(10),10);AfflictionNpc.ApplyHit(clientOnly,A.Player,10);
            Check(AfflictionNpc.Effects.All(e=>!clientOnly.HasBuff(e.Buff)),"client callback cannot mint authoritative damage sources");
            Main.netMode=NetmodeID.SinglePlayer;
            var rejected=Target();rejected.friendly=true;AfflictionNpc.ApplyHit(rejected,A.Player,10);rejected.friendly=false;
            AfflictionNpc.ApplyHit(rejected,A.Player,0);Check(Bonus(rejected)==0&&!rejected.HasBuff(BuffID.OnFire),"friendly and zero-damage hits do not apply statuses");
            var mixed=Target();AfflictionNpc.ApplyHit(mixed,A.Player,10);mixed.AddBuff(BuffID.Oiled,1200);
            int withoutOil=AfflictionNpc.Effects.Sum(e=>e.Regen)*3;
            int mixedLoss=DamageTicks(mixed);
            Check(mixedLoss*120-mixed.lifeRegenCount==(withoutOil+50)*120,"oiled native synergy is not multiplied by talent damage");
            var huge=Target();AfflictionNpc.ApplyHit(huge,A.Player,10);
            A.State.Talents["AfflictionDamage"].AddCost(1,BigInteger.Pow(10,80));Flags(huge);huge.lifeRegen=0;int popup=0;
            NPCLoader.UpdateLifeRegen(huge,ref popup);
            Check(huge.lifeRegen==-120000000&&popup==1000000,"unlimited levels saturate engine output and bound native damage loops");

            Reset();A.Award(100000000*Experience.Scale);SetTalent(A,"AttackVenom",10);SetTalent(A,"AfflictionDamage",10);
            var doomed=Target();doomed.life=doomed.lifeMax=60;EncounterSystem.Spawn(doomed,null);
            var xpBefore=A.State.TotalExperienceEarned;
            ItemHit(doomed);DamageTicks(doomed);
            Check(!doomed.active,"native talent-enhanced DoT can finish an enemy");
            Settle(doomed);var xpAfter=A.State.TotalExperienceEarned;Settle(doomed);
            Check(xpAfter-xpBefore==60*Experience.Scale&&A.State.TotalExperienceEarned==xpAfter,"DoT kill follows existing maximum-life XP settlement exactly once");

            Reset();A.Award(100000000*Experience.Scale);var p=A.Player;
            p.inventory[0]=new Item(ItemID.WoodFishingPole);p.inventory[1]=new Item(ItemID.MasterBait){stack=999};
            int Power(int level,int equipment=0) {
                SetTalent(A,"FishingPower",level);p.ResetEffects();p.fishingSkill=equipment;PlayerLoader.UpdateEquips(p);
                return p.GetFishingConditions().FinalFishingLevel;
            }
            int basePower=Power(0),power1=Power(1),power10=Power(10),gear=Power(0,10),combined=Power(10,10);
            float factor=p.GetFishingConditions().LevelMultipliers;
            Check(power1==(int)((55+5)*factor)&&power10==(int)((55+50)*factor),"native fishing conditions include plus-five and plus-fifty base power");
            Check(gear==(int)(65*factor)&&combined==(int)(115*factor),"native equipment and talent power add before weather multiplier");
            A.State.Talents["FishingPower"].Enabled=false;p.ResetEffects();PlayerLoader.UpdateEquips(p);
            Check(p.GetFishingConditions().FinalFishingLevel==basePower,"fishing disable removes only talent contribution");
            A.State.Talents["FishingPower"].Enabled=true;A.State.Talents["FishingPower"].CurrentIntensity=0;p.ResetEffects();PlayerLoader.UpdateEquips(p);
            Check(p.GetFishingConditions().FinalFishingLevel==basePower,"fishing zero intensity preserves native power");
            string folder=Path.Combine(Path.GetTempPath(),"tp-affliction-scanner-"+Guid.NewGuid());
            try {
                string report=AccessoryTalentScanner.WriteReport(folder);using var json=JsonDocument.Parse(File.ReadAllText(report+".json"));
                Check(json.RootElement.GetProperty("ModVersion").GetString()==ModContent.GetInstance<TerrariaProgression.TerrariaProgression>().Version.ToString(),"scanner reports actual loaded mod version");
                var rows=AccessoryTalentScanner.Scan();
                Check(rows.Single(r=>r.ItemId==ItemID.AnglerEarring).TalentIds.Contains("FishingPower"),"reviewed fishing accessory maps to new power talent");
                Check(rows.Single(r=>r.ItemId==ItemID.BrickLayer).TalentIds.Contains("PlacementSpeed")&&rows.Single(r=>r.ItemId==ItemID.PortableCementMixer).TalentIds.Contains("WallPlacementSpeed"),"block and wall accessory mappings remain distinct");
                Check(rows.Single(r=>r.ItemId==ItemID.Radar).MappingStatus=="Partial"&&rows.Single(r=>r.ItemId==ItemID.AnkhShield).MappingStatus=="Partial","reviewed children do not falsely claim composite equivalence");
            } finally {Directory.Delete(folder,true);}
            Check(ProgressionNetwork.ProtocolVersion==11,"catalog and NPC sync use protocol eleven boundary");
        } finally {Main.combatText=oldText;Reset();}
    }
}
