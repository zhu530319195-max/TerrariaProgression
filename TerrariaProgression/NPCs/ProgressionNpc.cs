using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaProgression.NPCs;

public sealed class ProgressionNpc : GlobalNPC
{
    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        if (EncounterSystem.Authority) EncounterSystem.Spawn(npc, source);
    }
    public override void PostAI(NPC npc)
    {
        if (EncounterSystem.Authority) EncounterSystem.Observe(npc);
    }
    public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        if (Main.netMode == NetmodeID.SinglePlayer) EncounterSystem.ReportAttributedDamage(npc, player, damageDone);
    }
    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        // In MP player-owned projectiles report their vanilla strike through the owner.
        // Do not count it again in an entity hook. Server-owned third-party attacks can
        // use the explicit ReportAttributedDamage integration point when owner is known.
        if (Main.netMode == NetmodeID.SinglePlayer && projectile.owner >= 0 && projectile.owner < Main.maxPlayers && !projectile.hostile)
            EncounterSystem.ReportAttributedDamage(npc, Main.player[projectile.owner], damageDone);
    }
    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (EncounterSystem.Authority && npc.life <= 0) EncounterSystem.Get(npc).SawLethalHit = true;
        if (Main.netMode == NetmodeID.Server) StrikeObserver.Confirm(npc, hit);
    }
    public override void OnKill(NPC npc)
    {
        if (EncounterSystem.Authority) EncounterSystem.Killed(npc);
    }
}
