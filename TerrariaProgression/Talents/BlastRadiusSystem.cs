using System;
using System.Numerics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Config;
using TerrariaProgression.Players;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace TerrariaProgression.Talents;

// One native explosion with a different terrain radius. Never resize damage,
// filter individual blocks, suppress framing, or schedule a second outer ring.
public sealed class BlastRadiusSystem : ModSystem
{
    private static ProgressionConfig Config => ModContent.GetInstance<ProgressionConfig>();
    internal static int BaseRadius(int type) => type switch {
        ProjectileID.Bomb or ProjectileID.StickyBomb or ProjectileID.BouncyBomb => 4,
        ProjectileID.Dynamite or ProjectileID.StickyDynamite or ProjectileID.BouncyDynamite => 7,
        _ => 0
    };
    internal static Vector2 TerrainOrigin(Projectile p) => p.Center - new Vector2(BaseRadius(p.type) == 4 ? 11 : 5);
    internal static int Radius(Projectile p) => BaseRadius(p.type) + (int)BigInteger.Clamp(
        ExtendedTalentPlayer.Level(Main.player[p.owner], "BlastRadius"), 0, Math.Clamp(Config.MaxBlastRadius, 7, 128) - BaseRadius(p.type));
    internal static bool ActorValid(Projectile p, Guid session) => BaseRadius(p.type) > 0 && !p.npcProj && !p.trap
        && p.owner >= 0 && p.owner < Main.maxPlayers && Main.player[p.owner].active
        && Main.player[p.owner].GetModPlayer<ProgressionPlayer>().SessionReady
        && Main.player[p.owner].GetModPlayer<ProgressionPlayer>().SessionId == session;
    internal static bool Enabled(Projectile p, Guid session) => ActorValid(p,session)
        && Config.EnableWorldGathering && Config.EnableBlastRadius
        && ExtendedTalentPlayer.Level(Main.player[p.owner], "BlastRadius") > 0;
    internal static bool Handles(Projectile p, Guid session) => ActorValid(p,session)
        && (Enabled(p,session) || ExtendedTalentPlayer.Level(Main.player[p.owner],"BasicBlockYield") > 0);
    public override void Load() => On_Projectile.ExplodeTiles += Explode;
    public override void Unload() => On_Projectile.ExplodeTiles -= Explode;
    private static void Explode(On_Projectile.orig_ExplodeTiles orig, Projectile p, Vector2 origin, int radius, int minX, int maxX, int minY, int maxY, bool walls)
    {
        if (!p.TryGetGlobalProjectile<BlastRadiusProjectile>(out var state) || !Handles(p, state.Session)) {
            orig(p, origin, radius, minX, maxX, minY, maxY, walls); return;
        }
        // The owner client's native core must not duplicate server-owned terrain.
        if (Main.netMode == NetmodeID.MultiplayerClient || state.Consumed) return;
        state.Consumed = true;
        radius = Enabled(p,state.Session) ? Radius(p) : BaseRadius(p.type);
        // Native wall framing touches neighbours one cell beyond the scan bounds.
        minX = Math.Max(1, (int)(origin.X / 16f - radius)); maxX = Math.Min(Main.maxTilesX - 2, (int)(origin.X / 16f + radius));
        minY = Math.Max(1, (int)(origin.Y / 16f - radius)); maxY = Math.Min(Main.maxTilesY - 2, (int)(origin.Y / 16f + radius));
        if (!float.IsFinite(origin.X) || !float.IsFinite(origin.Y) || minX > maxX || minY > maxY) return;
        walls = p.ShouldWallExplode(origin, radius, minX, maxX, minY, maxY);
        var old = EconomySystem.Actor; EconomySystem.Actor = Main.player[p.owner];
        try { orig(p, origin, radius, minX, maxX, minY, maxY, walls); }
        finally { EconomySystem.Actor = old; }
    }
}

public sealed class BlastRadiusProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    internal Guid Session;
    internal bool Consumed;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => BlastRadiusSystem.BaseRadius(entity.type) > 0;
    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        Consumed = false; Session = Guid.Empty;
        if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers) {
            var state = Main.player[projectile.owner].GetModPlayer<ProgressionPlayer>();
            if (state.SessionReady) Session = state.SessionId;
        }
    }
    public override void OnKill(Projectile p, int timeLeft)
    {
        // In vanilla only the owner client executes ExplodeTiles from Kill.
        // Run the same native terrain operation once on the dedicated server.
        if (Main.netMode != NetmodeID.Server || Consumed || !BlastRadiusSystem.Handles(p, Session)) return;
        p.ExplodeTiles(BlastRadiusSystem.TerrainOrigin(p), BlastRadiusSystem.BaseRadius(p.type), 0, 0, 0, 0, false);
    }
}
