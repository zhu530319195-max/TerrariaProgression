using System;
using System.Numerics;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;

namespace TerrariaProgression.Players;

public sealed class CombatUtilityPlayer : ModPlayer
{
    internal static readonly (string Child, int Buff)[] Immunities = {
        ("Poisoned", BuffID.Poisoned), ("Bleeding", BuffID.Bleeding), ("Slow", BuffID.Slow),
        ("Weak", BuffID.Weak), ("BrokenArmor", BuffID.BrokenArmor), ("Silenced", BuffID.Silenced),
        ("Cursed", BuffID.Cursed), ("Confused", BuffID.Confused), ("Darkness", BuffID.Darkness),
        ("Chilled", BuffID.Chilled), ("Frozen", BuffID.Frozen), ("Stoned", BuffID.Stoned), ("OnFire", BuffID.OnFire)
    };
    private BigInteger Level(string id) => ExtendedTalentPlayer.Level(Player, id);
    private bool IsOwner => Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server;
    public override void UpdateEquips()
    {
        var progression=Player.GetModPlayer<ProgressionPlayer>();
        if (!progression.SessionReady) return;
        foreach (var (child,buff) in Immunities)
            if (FunctionalTalentRegistry.ChildEnabled(progression.State,"StatusImmunity",child)) {
                Player.buffImmune[buff]=true;
                if (IsOwner) Player.ClearBuff(buff);
            }
        // Native OnHurt applies Panic; no buff is granted by purchasing/toggling.
        if (Level("PanicSpeed")>0) Player.panic=true;
    }
    public override void PostHurt(Player.HurtInfo info)
    {
        // Vanilla owns player hurt/projectile creation on the affected client.
        // Remote/server copies must never spawn another set of retaliation shots.
        if (!IsOwner || !Player.active || Player.dead || Player.statLife<=0 || info.Damage<=0) return;
        if (Level("StarRetaliation")>0 && (Player.starCloakItem==null || Player.starCloakItem.IsAir) &&
            (info.CooldownCounter==-1 || info.CooldownCounter==1)) SpawnStars(info);
        if (Level("BeeRetaliation")>0 && (Player.honeyCombItem==null || Player.honeyCombItem.IsAir)) SpawnBees(info);
    }
    private void SpawnStars(Player.HurtInfo info)
    {
        var source=Player.GetSource_OnHurt(info.DamageSource,"TerrariaProgression.StarRetaliation");
        int nativeDamage=75*(Main.masterMode ? 3 : Main.expertMode ? 2 : 1);
        int damage=TalentMath.ScaleInt(nativeDamage,TalentMath.Level(Level("StarRetaliation")));
        // Constant native count: unlimited levels grow damage, not projectile loops.
        for (int i=0;i<3;i++) {
            var origin=Player.position+new Vector2(Main.rand.Next(-400,400),-Main.rand.Next(500,800));
            var direction=Player.Center-origin; direction.X+=Main.rand.Next(-100,101);
            direction=Vector2.Normalize(direction)*23;
            Projectile.NewProjectile(source,origin,direction,ProjectileID.StarCloakStar,damage,5,Player.whoAmI,0,Player.position.Y);
        }
    }
    private void SpawnBees(Player.HurtInfo info)
    {
        int count=1+(Main.rand.Next(3)==0 ? 1 : 0)+(Main.rand.Next(3)==0 ? 1 : 0);
        if (Player.strongBees && Main.rand.Next(3)==0) count++;
        float nativeDamage=(Player.strongBees ? 18 : 13)*(Main.masterMode ? 2f : Main.expertMode ? 1.5f : 1f);
        int damage=TalentMath.ScaleInt(Player.beeDamage((int)nativeDamage),TalentMath.Level(Level("BeeRetaliation")));
        var source=Player.GetSource_OnHurt(info.DamageSource,"TerrariaProgression.BeeRetaliation");
        for (int i=0;i<count;i++) {
            var velocity=new Vector2(Main.rand.Next(-35,36),Main.rand.Next(-35,36))*.02f;
            Projectile.NewProjectile(source,Player.position,velocity,Player.beeType(),damage,Player.beeKB(0),Player.whoAmI);
        }
        // Deliberately omit Honey Comb's separate healing buff: this talent buys bees.
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!IsOwner || Player.dead || damageDone<=0 || !target.active || target.life<=0 ||
            target.friendly || target.townNPC || target.immortal || target.dontTakeDamage) return;
        ApplyAttackDebuff(target,"AttackBurn",BuffID.OnFire);
        ApplyAttackDebuff(target,"AttackPoison",BuffID.Poisoned);
    }
    private void ApplyAttackDebuff(NPC target, string talent, int buff)
    {
        int duration=TalentMath.AddInt(0,Level(talent)*120); // two seconds per active level
        if (duration>0) target.AddBuff(buff,duration); // native immunity, max refresh and sync
    }
}
