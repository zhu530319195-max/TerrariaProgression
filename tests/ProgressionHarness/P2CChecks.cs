using System;
using System.Linq;
using System.Numerics;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace ProgressionHarness;

public sealed class HurtImmunityProbe : ModPlayer
{
    internal static bool Enabled;
    public override bool ImmuneTo(PlayerDeathReason source, int cooldownCounter, bool dodgeable) => Enabled;
}

public sealed partial class RuntimeChecks
{
    private void RunP2C()
    {
        Reset(); A.Award(1000000*Experience.Scale);
        var p=A.Player;
        var combat=p.GetModPlayer<CombatUtilityPlayer>();
        void Toggle(string id,bool enabled) => A.ApplyTalent(enabled ? TalentOperation.Enable : TalentOperation.Disable,id,TalentCategory.Combat,1);
        // Player.Update clears buffImmune separately before UpdateBuffs/UpdateEquips.
        void Equip() { p.ResetEffects(); Array.Clear(p.buffImmune); PlayerLoader.UpdateEquips(p); }
        A.ApplyTalent(TalentOperation.Upgrade,"StatusImmunity",TalentCategory.Utility,1);
        Equip();
        Check(CombatUtilityPlayer.Immunities.Select(i=>i.Child).SequenceEqual(FunctionalTalentRegistry.ImmunityChildren),"immunity runtime mapping matches child UI order");
        foreach(var (child,buff) in CombatUtilityPlayer.Immunities) {
            p.AddBuff(buff,600);
            Check(p.buffImmune[buff] && !p.HasBuff(buff),"native immunity rejects "+child);
        }
        Check(!p.buffImmune[BuffID.PotionSickness] && !p.buffImmune[BuffID.ChaosState] && !p.buffImmune[BuffID.MoonLeech] && !p.buffImmune[BuffID.Venom],"immunity excludes cooldowns and unregistered statuses");
        A.ApplyTalent(TalentOperation.ToggleChild,"StatusImmunity",TalentCategory.Utility,1); Equip(); p.AddBuff(BuffID.Poisoned,600);
        Check(p.HasBuff(BuffID.Poisoned) && p.buffImmune[BuffID.OnFire],"poison child off does not disable other immunities");
        A.ApplyTalent(TalentOperation.ToggleChild,"StatusImmunity",TalentCategory.Utility,1); Equip();
        Check(!p.HasBuff(BuffID.Poisoned),"enabling immunity removes already active native status");
        A.ApplyTalent(TalentOperation.ToggleChild,"StatusImmunity",TalentCategory.Utility,1); p.ResetEffects(); p.buffImmune[BuffID.Poisoned]=true; PlayerLoader.UpdateEquips(p);
        Check(p.buffImmune[BuffID.Poisoned],"disabled immunity child preserves real equipment immunity");
        var saved=new TagCompound(); A.SaveData(saved); B.LoadData(saved);
        Check(!FunctionalTalentRegistry.ChildEnabled(B.State,"StatusImmunity","Poisoned"),"native character save retains immunity child selection");

        foreach(string id in new[]{"StarRetaliation","BeeRetaliation","AttackBurn","AttackPoison"})
            A.ApplyTalent(TalentOperation.Upgrade,id,TalentCategory.Combat,1);
        A.ApplyTalent(TalentOperation.Upgrade,"PanicSpeed",TalentCategory.Combat,1);
        Equip();
        p.position=new Vector2(Main.spawnTileX*16,Main.spawnTileY*16-100);
        void ClearProjectiles() { foreach(var projectile in Main.projectile) projectile.active=false; }
        Projectile[] Stars() => Main.projectile.Where(q=>q.active && q.owner==p.whoAmI && q.type is ProjectileID.StarCloakStar or ProjectileID.StarVeilStar or ProjectileID.BeeCloakStar or ProjectileID.ManaCloakStar).ToArray();
        Projectile[] Bees() => Main.projectile.Where(q=>q.active && q.owner==p.whoAmI && q.type is ProjectileID.Bee or ProjectileID.GiantBee).ToArray();
        void Hurt(int channel=-1) {
            p.statLife=p.statLifeMax2=500; p.dead=false; p.immune=false;
            p.Hurt(new Player.HurtInfo { Damage=10, DamageSource=PlayerDeathReason.ByOther(0), CooldownCounter=channel, SoundDisabled=true, DustDisabled=true },quiet:true);
        }
        Check(!p.HasBuff(BuffID.Panic) && Stars().Length==0 && Bees().Length==0,"purchases do not trigger retaliation or panic");
        ClearProjectiles(); Main.rand=new UnifiedRandom(123); Hurt();
        var nativeStarDamage=75*(Main.masterMode ? 3 : Main.expertMode ? 2 : 1);
        int nativeBeeDamage=Bees().First().damage; int nativeBeeCount=Bees().Length;
        Check(p.statLife==490 && Stars().Length==3 && Stars().All(q=>q.damage==nativeStarDamage),"native hurt consumes life and spawns three level-one retaliation stars");
        Check(nativeBeeCount>=1 && nativeBeeCount<=3 && nativeBeeDamage>0 && !p.HasBuff(BuffID.Honey),"bee talent uses bounded native count without unpurchased Honey healing");
        Check(p.HasBuff(BuffID.Panic) && p.buffTime[p.FindBuffIndex(BuffID.Panic)]==480,"native hurt grants eight-second panic");
        A.ApplyTalent(TalentOperation.Upgrade,"StarRetaliation",TalentCategory.Combat,9);
        A.ApplyTalent(TalentOperation.Upgrade,"BeeRetaliation",TalentCategory.Combat,9);
        ClearProjectiles(); Main.rand=new UnifiedRandom(123); Hurt();
        Check(Stars().Length==3 && Stars().All(q=>q.damage==nativeStarDamage*10),"level ten stars scale damage tenfold without growing count");
        Check(Bees().Length==nativeBeeCount && Bees().All(q=>q.damage==nativeBeeDamage*10),"level ten bees scale damage tenfold without growing count");
        A.ApplyTalent(TalentOperation.DecreaseIntensity,"StarRetaliation",TalentCategory.Combat,7);
        A.ApplyTalent(TalentOperation.DecreaseIntensity,"BeeRetaliation",TalentCategory.Combat,7);
        ClearProjectiles(); Main.rand=new UnifiedRandom(123); Hurt();
        Check(Stars().All(q=>q.damage==nativeStarDamage*3) && Bees().All(q=>q.damage==nativeBeeDamage*3),"adjustable active strength affects next retaliation");

        p.starCloakItem=new Item(ItemID.StarCloak); p.honeyCombItem=new Item(ItemID.HoneyComb);
        ClearProjectiles(); Main.rand=new UnifiedRandom(123); Hurt();
        Check(Stars().Length==3 && Stars().All(q=>q.damage==nativeStarDamage*3),"real star cloak is scaled once with no second retaliation");
        Check(Bees().Length==nativeBeeCount && Bees().All(q=>q.damage==nativeBeeDamage*3) && p.HasBuff(BuffID.Honey),"real honey comb retains native healing and only one scaled bee batch");
        p.ClearBuff(BuffID.Honey); p.ClearBuff(BuffID.Panic);
        p.starCloakItem=null; p.honeyCombItem=null;
        ClearProjectiles(); Hurt(0);
        Check(Stars().Length==0 && Bees().Length>0,"star retaliation honors native damage-channel restriction");
        Toggle("StarRetaliation",false); Toggle("BeeRetaliation",false); Toggle("PanicSpeed",false); Equip(); p.ClearBuff(BuffID.Panic);
        ClearProjectiles(); Hurt();
        Check(Stars().Length==0 && Bees().Length==0 && !p.HasBuff(BuffID.Panic),"disabled talents cannot trigger on next hurt");
        p.panic=true; p.starCloakItem=new Item(ItemID.StarCloak); ClearProjectiles(); Hurt();
        Check(p.HasBuff(BuffID.Panic) && Stars().Length==3 && Stars().All(q=>q.damage==nativeStarDamage),"disabled talents preserve real panic and star equipment");
        p.starCloakItem=null; p.panic=false;
        Toggle("StarRetaliation",true); Toggle("BeeRetaliation",true);
        ClearProjectiles(); HurtImmunityProbe.Enabled=true; p.immune=false;
        int life=p.statLife; double dealt=p.Hurt(PlayerDeathReason.ByOther(0),10,0,quiet:true);
        HurtImmunityProbe.Enabled=false;
        Check(dealt==0 && p.statLife==life && Stars().Length==0 && Bees().Length==0,"native blocked hurt cannot trigger retaliation");
        var hurtInfo=new Player.HurtInfo { Damage=10, CooldownCounter=-1 };
        Main.netMode=NetmodeID.Server; combat.PostHurt(hurtInfo);
        Check(Stars().Length==0 && Bees().Length==0,"server replay cannot spawn duplicate owner retaliation");
        Main.netMode=NetmodeID.MultiplayerClient; Main.myPlayer=1; combat.PostHurt(hurtInfo);
        Check(Stars().Length==0 && Bees().Length==0,"spectator cannot spawn another player's retaliation");
        Main.netMode=NetmodeID.SinglePlayer; Main.myPlayer=0; p.dead=true; combat.PostHurt(hurtInfo); p.dead=false;
        Check(Stars().Length==0 && Bees().Length==0,"dead player cannot spawn new talent retaliation");

        // A native player hit and a real friendly projectile collision exercise both routes.
        foreach(var npc in Main.npc) npc.active=false;
        int targetIndex=NPC.NewNPC(new EntitySource_Misc("P2C checks"),(int)p.position.X+64,(int)p.position.Y,NPCID.Zombie);
        var target=Main.npc[targetIndex]; target.life=target.lifeMax=5000; target.aiStyle=-1;
        void ClearStatuses() { Array.Clear(target.buffType); Array.Clear(target.buffTime); Array.Clear(target.immune); }
        p.ApplyDamageToNPC(target,10,0,1,false,DamageClass.Generic);
        Check(target.HasBuff(BuffID.OnFire) && target.HasBuff(BuffID.Poisoned) && target.buffTime[target.FindBuffIndex(BuffID.OnFire)]==120,"native player damage applies level-one attack debuffs");
        A.ApplyTalent(TalentOperation.Upgrade,"AttackBurn",TalentCategory.Combat,9);
        A.ApplyTalent(TalentOperation.Upgrade,"AttackPoison",TalentCategory.Combat,9);
        ClearStatuses(); ClearProjectiles();
        int shot=Projectile.NewProjectile(p.GetSource_Misc("P2C arrow test"),target.Center,Vector2.Zero,ProjectileID.WoodenArrowFriendly,10,0,0);
        Main.projectile[shot].Damage();
        Check(target.HasBuff(BuffID.OnFire) && target.HasBuff(BuffID.Poisoned) && target.buffTime[target.FindBuffIndex(BuffID.OnFire)]==1200,"native projectile collision applies twenty-second level-ten debuffs");
        p.ApplyDamageToNPC(target,10,0,1,false,DamageClass.Generic);
        Check(target.buffTime[target.FindBuffIndex(BuffID.OnFire)]==1200,"repeated attack refreshes duration without accumulating");
        target.AddBuff(BuffID.OnFire,1800); combat.OnHitNPC(target,Hit(10),10);
        Check(target.buffTime[target.FindBuffIndex(BuffID.OnFire)]==1800,"talent cannot shorten existing longer native debuff");
        ClearStatuses(); target.buffImmune[BuffID.OnFire]=true; target.buffImmune[BuffID.Poisoned]=true;
        combat.OnHitNPC(target,Hit(10),10);
        Check(!target.HasBuff(BuffID.OnFire) && !target.HasBuff(BuffID.Poisoned),"attack effects respect native enemy immunity");
        target.buffImmune[BuffID.OnFire]=false; target.buffImmune[BuffID.Poisoned]=false;
        Toggle("AttackBurn",false); combat.OnHitNPC(target,Hit(10),10);
        Check(!target.HasBuff(BuffID.OnFire) && target.HasBuff(BuffID.Poisoned),"burn and poison independently toggle");
        ClearStatuses(); target.friendly=true; combat.OnHitNPC(target,Hit(10),10); target.friendly=false;
        Check(!target.HasBuff(BuffID.Poisoned),"friendly NPCs excluded from attack effects");
        combat.OnHitNPC(target,Hit(0),0);
        Check(!target.HasBuff(BuffID.Poisoned),"zero-damage events do not apply debuffs");
        Main.netMode=NetmodeID.Server; combat.OnHitNPC(target,Hit(10),10);
        Check(!target.HasBuff(BuffID.Poisoned),"server does not repeat owner attack debuff request");
        Main.netMode=NetmodeID.SinglePlayer;
        saved=new TagCompound(); A.SaveData(saved); B.LoadData(saved);
        Check(B.State.Talents["StarRetaliation"].TalentLevel==10 && NumericTalents.ActiveLevel(B.State,"StarRetaliation")==3 && !B.State.Talents["AttackBurn"].Enabled,"native save retains growing combat strength and independent enable state");
        Main.netMode=NetmodeID.Server; A.HasActionTick=false;
        SendTalentAction(A.SessionId,A.TalentRevision,TalentOperation.ToggleChild,"StatusImmunity",13);
        Check(!FunctionalTalentRegistry.ChildEnabled(A.State,"StatusImmunity","OnFire"),"server validates immunity child operation");
        var imported=StateCodec.Decode(StateCodec.Encode(A.State)); A.SessionReady=false; Receive(1,imported);
        Check(A.SessionReady && A.State.Talents.ContainsKey("StarRetaliation"),"protocol eight imports P2C catalog");
        Reset();
    }
}
