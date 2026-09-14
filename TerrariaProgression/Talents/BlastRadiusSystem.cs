using System;
using System.Collections.Generic;
using System.Numerics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Players;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace TerrariaProgression.Talents;

// Only the additional terrain annulus is ours. Native damage, core tiles and
// walls retain their own vanilla execution, including vanilla network ownership.
public sealed class BlastRadiusSystem : ModSystem
{
    private static readonly Queue<Job> jobs = new();
    private static ProgressionConfig Config => ModContent.GetInstance<ProgressionConfig>();
    internal static int PendingCount => jobs.Count;
    internal static int BaseRadius(int type) => type switch {
        ProjectileID.Bomb or ProjectileID.StickyBomb or ProjectileID.BouncyBomb => 4,
        ProjectileID.Dynamite or ProjectileID.StickyDynamite or ProjectileID.BouncyDynamite => 7,
        _ => 0
    };
    // Kill's visual branch resizes bombs to 22 and dynamite to 10 before using
    // position as the terrain origin. Derive the same origin from stable Center.
    internal static Vector2 TerrainOrigin(Projectile p) => p.Center - new Vector2(BaseRadius(p.type) == 4 ? 11 : 5);
    internal static void Clear() { while (jobs.TryDequeue(out var job)) job.Dispose(); }
    public override void OnWorldUnload() => Clear();
    public override void Unload() => Clear();
    internal static bool Enqueue(Projectile projectile, Guid session)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || BaseRadius(projectile.type) == 0
            || projectile.owner < 0 || projectile.owner >= Main.maxPlayers) return false;
        var p = Main.player[projectile.owner];
        var state = p.GetModPlayer<ProgressionPlayer>();
        var level = ExtendedTalentPlayer.Level(p, "BlastRadius");
        var center = TerrainOrigin(projectile);
        if (!Valid(p, session) || level <= 0 || !float.IsFinite(center.X) || !float.IsFinite(center.Y)
            || center.X < 0 || center.Y < 0 || center.X >= Main.maxTilesX * 16f || center.Y >= Main.maxTilesY * 16f) return false;
        if (jobs.Count >= 128) { GatheringSystem.Notice(p, "BlastQueueFull"); return false; }
        jobs.Enqueue(new Job(p, state.TalentRevision, projectile.type, center, level));
        return true;
    }
    private static bool Valid(Player p, Guid session) => Config.EnableWorldGathering && Config.EnableBlastRadius
        && p.active && !p.dead && !p.noBuilding && p.GetModPlayer<ProgressionPlayer>().SessionReady
        && p.GetModPlayer<ProgressionPlayer>().SessionId == session;
    internal static void ProcessOne()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !jobs.TryDequeue(out var job)) return;
        if (job.Step()) jobs.Enqueue(job); else job.Dispose();
    }
    private sealed class Job : IDisposable
    {
        private readonly int owner, inner, outer;
        private readonly Guid session;
        private readonly ulong revision;
        private readonly Vector2 center;
        private readonly IEnumerator<TilePoint> scan;
        // A snapshot, never the reusable Main.projectile slot. CanExplodeTile in
        // this pinned engine uses native tile rules and TileLoader.CanExplode.
        private readonly Projectile probe;
        private readonly long limit;
        private long removed;
        internal Job(Player p, ulong revision, int type, Vector2 origin, BigInteger level)
        {
            owner = p.whoAmI; session = p.GetModPlayer<ProgressionPlayer>().SessionId;
            this.revision = revision; inner = BaseRadius(type);
            outer = inner + GatheringRules.Radius(level, 2 * Math.Max(Main.maxTilesX, Main.maxTilesY));
            center = origin / 16f;
            scan = GatheringRules.Square(new((int)center.X, (int)center.Y), outer).GetEnumerator();
            probe = new Projectile { type = type, owner = owner };
            limit = Config.MaxBlocksPerAction > 0 ? Config.MaxBlocksPerAction : (long)Main.maxTilesX * Main.maxTilesY;
        }
        internal bool Step()
        {
            var p = Main.player[owner];
            if (!Valid(p, session) || p.GetModPlayer<ProgressionPlayer>().TalentRevision != revision
                || ExtendedTalentPlayer.Level(p, "BlastRadius") <= 0) return false;
            long currentLimit = Config.MaxBlocksPerAction > 0 ? Math.Min(limit, Config.MaxBlocksPerAction) : limit;
            if (removed >= currentLimit) { GatheringSystem.Notice(p, "BlastLimitReached"); return false; }
            // Every visited cell costs one unit, even protected/empty/outside cells.
            if (!scan.MoveNext()) return false;
            var pos = scan.Current;
            double dx = pos.X - center.X, dy = pos.Y - center.Y, distance = dx * dx + dy * dy;
            if (distance < inner * inner || distance >= (double)outer * outer) return true;
            if (GatheringSystem.CanTouch(pos, GatheringMode.Area) && probe.CanExplodeTile(pos.X, pos.Y)
                && GatheringSystem.Explode(p, pos)) removed++;
            return true;
        }
        public void Dispose() => scan.Dispose();
    }
}

public sealed class BlastRadiusProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private Guid session;
    private bool consumed;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => BlastRadiusSystem.BaseRadius(entity.type) > 0;
    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        consumed = false; session = Guid.Empty;
        if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers) {
            var state = Main.player[projectile.owner].GetModPlayer<ProgressionPlayer>();
            if (state.SessionReady) session = state.SessionId;
        }
    }
    public override void OnKill(Projectile projectile, int timeLeft)
    {
        if (consumed || session == Guid.Empty) return;
        consumed = true;
        BlastRadiusSystem.Enqueue(projectile, session);
    }
}
