using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

// Apply the same growth to real native accessories instead of spawning duplicates.
public sealed class RetaliationProjectile : GlobalProjectile
{
    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (source is not EntitySource_ItemUse_OnHurt hurt || hurt.Entity is not Player player ||
            Main.netMode==NetmodeID.Server || player.whoAmI!=Main.myPlayer || projectile.owner!=player.whoAmI) return;
        string? talent=null;
        if (projectile.type is ProjectileID.StarCloakStar or ProjectileID.StarVeilStar or ProjectileID.BeeCloakStar or ProjectileID.ManaCloakStar &&
            (ReferenceEquals(hurt.Item,player.starCloakItem) || ReferenceEquals(hurt.Item,player.starCloakItem_starVeilOverrideItem) ||
             ReferenceEquals(hurt.Item,player.starCloakItem_beeCloakOverrideItem) || ReferenceEquals(hurt.Item,player.starCloakItem_manaCloakOverrideItem))) talent="StarRetaliation";
        else if (projectile.type is ProjectileID.Bee or ProjectileID.GiantBee && ReferenceEquals(hurt.Item,player.honeyCombItem)) talent="BeeRetaliation";
        if (talent==null) return;
        var level=ExtendedTalentPlayer.Level(player,talent);
        if (level<=0) return;
        projectile.damage=TalentMath.ScaleInt(projectile.damage,TalentMath.Level(level));
        projectile.originalDamage=projectile.damage;
    }
}
