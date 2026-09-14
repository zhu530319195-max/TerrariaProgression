using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Chat;
using Microsoft.Xna.Framework;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Networking;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class GatheringSystem : ModSystem
{
    internal static ModKeybind? ActionKey, ModeKey;
    private static ProgressionConfig Config => ModContent.GetInstance<ProgressionConfig>();
    private static readonly Dictionary<int, Job> jobs = new();
    private static readonly Dictionary<int, (Guid Session, uint Sequence, ulong Tick)> requests = new();
    private static readonly Dictionary<int, ulong> notices = new();
    private static TilePoint? permitted;
    private static int killDepth;
    private static int pickDamage;
    private static int nextPlayer;
    internal static int PendingCount => jobs.Count;
    public override void Load()
    {
        if (!Main.dedServ) {
            ActionKey = KeybindLoader.RegisterKeybind(Mod, "GatheringAction", "LeftAlt");
            ModeKey = KeybindLoader.RegisterKeybind(Mod, "GatheringMode", "G");
        }
        On_Player.ItemCheck_UseMiningTools_ActuallyUseMiningTool += UseTool;
        On_Player.GetPickaxeDamage += PickDamage;
        On_WorldGen.KillTile += GuardKill;
    }
    public override void Unload()
    {
        On_Player.ItemCheck_UseMiningTools_ActuallyUseMiningTool -= UseTool;
        On_Player.GetPickaxeDamage -= PickDamage;
        On_WorldGen.KillTile -= GuardKill;
        ActionKey = ModeKey = null; Clear();
    }
    public override void OnWorldUnload() => Clear();
    internal static void Clear() { jobs.Clear(); requests.Clear(); notices.Clear(); permitted = null; killDepth = nextPlayer = 0; }
    private static int PickDamage(On_Player.orig_GetPickaxeDamage orig, Player player, int x, int y, int power, int buffer, Tile tile)
    {
        int damage = orig(player, x, y, power, buffer, tile);
        if (permitted == new TilePoint(x, y)) pickDamage = damage;
        return damage;
    }
    private static void GuardKill(On_WorldGen.orig_KillTile orig, int x, int y, bool fail, bool effectOnly, bool noItem)
    {
        if (permitted == null) { orig(x, y, fail, effectOnly, noItem); return; }
        // Framing can recursively destroy tree branches, furniture or fossils.
        // Only this job step's single, checked tile may be destroyed now.
        if (killDepth != 0 || permitted != new TilePoint(x, y)) return;
        killDepth++;
        try { orig(x, y, fail, effectOnly, noItem); }
        finally { killDepth--; }
    }
    internal static bool Enabled(GatheringMode mode) => Config.EnableWorldGathering && mode switch {
        GatheringMode.Area => Config.EnableAreaMining, GatheringMode.Vein => Config.EnableVeinMining,
        GatheringMode.Tree => Config.EnableTreeFelling, _ => false
    };
    internal static bool NativeTree(int type) => type > 0 && type < TileID.Count && (TileID.Sets.IsATreeTrunk[type] || type == TileID.PalmTree);
    internal static bool Ore(int type) => type >= 0 && type < TileID.Sets.Ore.Length && TileID.Sets.Ore[type]
        && (type < TileID.Count || Config.EnableModOreMining);
    internal static bool CanTouch(TilePoint pos, GatheringMode mode)
    {
        int x = pos.X, y = pos.Y;
        if (!WorldGen.InWorld(x, y, 10)) return false;
        var t = Main.tile[x, y];
        if (!t.HasTile || t.IsActuated || t.RedWire || t.BlueWire || t.GreenWire || t.YellowWire || t.HasActuator) return false;
        int type = t.TileType;
        if (Config.ProtectedTileAreas.Any(a => a != null && a.Contains(x, y))) return false;
        if (mode == GatheringMode.Tree) { if (!NativeTree(type)) return false; }
        else {
            if (type >= TileID.Count && !(mode == GatheringMode.Vein && Ore(type))) return false;
            if (!Main.tileSolid[type] || Main.tileFrameImportant[type] || Main.tileAxe[type] || Main.tileHammer[type]
                || Main.tileDungeon[type] || type == TileID.LihzahrdBrick || TileID.Sets.Falling[type]) return false;
            if (mode == GatheringMode.Vein && !Ore(type)) return false;
        }
        // Preserve direct supports on all sides, including hanging objects and
        // falling blocks. Conservative frame-important detection covers mod furniture.
        for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) {
            if (dx == 0 && dy == 0) continue;
            var n = Main.tile[x + dx, y + dy];
            if (!n.HasTile) continue;
            if (dy < 0 && TileID.Sets.Falling[n.TileType]) return false;
            if (Main.tileFrameImportant[n.TileType] && !(mode == GatheringMode.Tree && NativeTree(n.TileType))) return false;
        }
        return WorldGen.CanKillTile(x, y);
    }
    private static void UseTool(On_Player.orig_ItemCheck_UseMiningTools_ActuallyUseMiningTool orig, Player player, Item item, out bool canHitWalls, int x, int y)
    {
        if (permitted != null || player.whoAmI != Main.myPlayer || !GatheringPlayer.InputAllowed || ActionKey?.Current != true) {
            orig(player, item, out canHitWalls, x, y); return;
        }
        GatheringMode mode = WorldGen.InWorld(x, y) && Main.tile[x, y].HasTile && NativeTree(Main.tile[x, y].TileType) && item.axe > 0
            ? GatheringMode.Tree : player.GetModPlayer<GatheringPlayer>().MiningMode;
        if (ExtendedTalentPlayer.Level(player, GatheringRules.Talent(mode)) <= 0) { orig(player, item, out canHitWalls, x, y); return; }
        canHitWalls = false;
        var gp = player.GetModPlayer<GatheringPlayer>();
        if (Main.netMode == NetmodeID.MultiplayerClient)
            ProgressionNetwork.SendGathering(player.GetModPlayer<ProgressionPlayer>(), mode, x, y, ++gp.Sequence);
        else Request(player, mode, x, y, player.GetModPlayer<ProgressionPlayer>().SessionId,
            player.GetModPlayer<ProgressionPlayer>().TalentRevision, ++gp.Sequence, player.selectedItem, item.type);
        // The existing ToolEfficiency hook divides this interval once in normal input.
        player.ApplyItemTime(item, mode == GatheringMode.Tree ? 1 : player.pickSpeed);
    }
    internal static int Interval(Player p, GatheringMode mode) => Math.Max(1, CombinedHooks.TotalUseTime(
        (float)(p.HeldItem.useTime * (mode == GatheringMode.Tree ? 1 : p.pickSpeed) / ToolEfficiencySystem.Factor(p)), p, p.HeldItem));
    internal static bool InReach(Player p, TilePoint pos)
    {
        // Do not read Player.tileRangeX/Y: they are static, reset per local player.
        // Use the native base plus this server-confirmed character's reach contribution.
        int bonus = (int)BigInteger.Min(ExtendedTalentPlayer.Level(p, "ToolReach"), Math.Max(Main.maxTilesX, Main.maxTilesY));
        int rx = 5 + bonus, ry = 4 + bonus;
        if (p.equippedAnyTileRangeAcc) { rx += 3; ry += 2; }
        if (Main.netMode == NetmodeID.SinglePlayer && p.whoAmI == Main.myPlayer) {
            rx = Player.tileRangeX; ry = Player.tileRangeY;
        }
        return p.position.X / 16f - rx - p.HeldItem.tileBoost <= pos.X
            && (p.position.X + p.width) / 16f + rx + p.HeldItem.tileBoost - 1 >= pos.X
            && p.position.Y / 16f - ry - p.HeldItem.tileBoost <= pos.Y
            && (p.position.Y + p.height) / 16f + ry + p.HeldItem.tileBoost - 2 >= pos.Y;
    }
    internal static bool Request(Player p, GatheringMode mode, int x, int y, Guid session, ulong revision, uint seq, int slot, int itemType)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Enum.IsDefined(mode) || !p.active || p.dead || p.CCed || p.noItems || p.noBuilding) return false;
        var state = p.GetModPlayer<ProgressionPlayer>();
        if (!state.SessionReady || session != state.SessionId || revision != state.TalentRevision || seq == 0
            || slot != p.selectedItem || itemType != p.HeldItem.type) return false;
        if (requests.TryGetValue(p.whoAmI, out var last) && last.Session == session &&
            (seq <= last.Sequence || Main.GameUpdateCount - last.Tick < (ulong)Interval(p, mode))) return false;
        requests[p.whoAmI] = (session, seq, Main.GameUpdateCount);
        if (jobs.ContainsKey(p.whoAmI)) return false;
        var origin = new TilePoint(x, y);
        if (!Enabled(mode) || ExtendedTalentPlayer.Level(p, GatheringRules.Talent(mode)) <= 0 || !InReach(p, origin) || !CanTouch(origin, mode)
            || (mode == GatheringMode.Tree ? p.HeldItem.axe <= 0 : p.HeldItem.pick <= 0)) {
            Notice(p, "GatheringBlocked"); return false;
        }
        var level = ExtendedTalentPlayer.Level(p, GatheringRules.Talent(mode));
        var job = new Job(p, mode, origin, Main.tile[x, y].TileType, level);
        if (mode == GatheringMode.Tree && !job.PrepareTree()) { Notice(p, "GatheringBlocked"); return false; }
        bool removed = Hit(p, origin, mode);
        if (!removed) return true; // The first tile still requires normal tool hits.
        job.Removed = 1;
        job.Prepare();
        if (job.Limit > 1) jobs[p.whoAmI] = job;
        return true;
    }
    // One real native pick/axe hit per work unit; no repeated damage bonus/drop replay.
    internal static bool Hit(Player p, TilePoint pos, GatheringMode mode)
    {
        var oldActor = EconomySystem.Actor; var oldPermit = permitted;
        EconomySystem.Actor = p; permitted = pos; pickDamage = 0;
        try {
            if (mode != GatheringMode.Tree) p.PickTile(pos.X, pos.Y, p.HeldItem.pick);
            else {
                int buffer = p.hitTile.HitObject(pos.X, pos.Y, 1);
                int damage = Main.tileNoFail[Main.tile[pos.X, pos.Y].TileType] ? 100 : 0;
                damage += (int)(p.HeldItem.axe * 1.2f);
                if (Main.getGoodWorld) damage = (int)(damage * 1.3);
                pickDamage = damage;
                bool done = p.hitTile.AddDamage(buffer, damage) >= 100;
                if (done) p.hitTile.Clear(buffer);
                WorldGen.KillTile(pos.X, pos.Y, fail: !done);
            }
            if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1, pos.X, pos.Y, 3);
            return !Main.tile[pos.X, pos.Y].HasTile;
        }
        finally { permitted = oldPermit; EconomySystem.Actor = oldActor; }
    }
    private static void Notice(Player p, string key)
    {
        if (notices.TryGetValue(p.whoAmI, out var tick) && Main.GameUpdateCount - tick < 120) return;
        notices[p.whoAmI] = Main.GameUpdateCount;
        if (Main.netMode == NetmodeID.Server) ChatHelper.SendChatMessageToClient(NetworkText.FromKey("Mods.TerrariaProgression.Messages." + key), Color.Goldenrod, p.whoAmI);
        else Main.NewText(Language.GetTextValue("Mods.TerrariaProgression.Messages." + key), Color.Goldenrod);
    }
    public override void PostUpdateWorld() => ProcessJobs();
    internal static void ProcessJobs()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        int budget = Math.Clamp(Config.GatheringWorkPerTick, 1, 256);
        // Round robin across owners: one player's unlimited job cannot starve others.
        while (budget-- > 0 && jobs.Count > 0) {
            int owner = jobs.Keys.Where(i => i >= nextPlayer).DefaultIfEmpty(jobs.Keys.Min()).Min();
            nextPlayer = owner + 1;
            var job = jobs[owner];
            if (!job.Step()) { jobs.Remove(owner); job.Dispose(); }
        }
    }
    private sealed class Job : IDisposable
    {
        private readonly int owner, slot, itemType, radius;
        private readonly Guid session;
        private readonly ulong revision;
        private readonly GatheringMode mode;
        private readonly TilePoint origin;
        private readonly ushort ore;
        private readonly BigInteger level;
        private IEnumerator<TilePoint>? scan;
        private readonly Queue<TilePoint> vein = new();
        private readonly HashSet<TilePoint> visited = new();
        private readonly List<TilePoint> tree = new();
        private TilePoint? current;
        private ushort currentType;
        private int attempts;
        public long Removed, Limit;
        public Job(Player p, GatheringMode mode, TilePoint origin, ushort ore, BigInteger level)
        {
            owner = p.whoAmI; slot = p.selectedItem; itemType = p.HeldItem.type;
            var s = p.GetModPlayer<ProgressionPlayer>(); session = s.SessionId; revision = s.TalentRevision;
            this.mode = mode; this.origin = origin; this.ore = ore; this.level = level;
            radius = GatheringRules.Radius(level, Math.Max(Main.maxTilesX, Main.maxTilesY));
            Limit = GatheringRules.Limit(mode, level, Config.MaxBlocksPerAction, (long)Main.maxTilesX * Main.maxTilesY);
        }
        public bool PrepareTree()
        {
            WorldGen.GetTreeBottom(origin.X, origin.Y, out int bx, out int by);
            if (!WorldGen.InWorld(bx, by, 10) || !Main.tile[bx, by].HasTile || NativeTree(Main.tile[bx, by].TileType)) return false;
            // Native tree frames identify the actual supporting trunk; adjacent trees
            // with the same tile type have a different bottom and are never included.
            for (int y = by - 1; y >= 10; y--) {
                bool any = false;
                for (int x = bx - 2; x <= bx + 2; x++) {
                    var t = Main.tile[x, y];
                    if (!t.HasTile || t.TileType != ore) continue;
                    WorldGen.GetTreeBottom(x, y, out int tx, out int ty);
                    if (tx != bx || ty != by) continue;
                    any = true; tree.Add(new(x, y));
                }
                if (!any) break;
            }
            return tree.Contains(origin) && tree.Count <= Limit && tree.All(p => CanTouch(p, GatheringMode.Tree));
        }
        public void Prepare()
        {
            if (mode == GatheringMode.Area) scan = GatheringRules.Square(origin, radius).GetEnumerator();
            else if (mode == GatheringMode.Tree) scan = tree.OrderByDescending(p => p.Y).GetEnumerator();
            else { visited.Add(origin); AddNeighbours(origin); }
        }
        private void AddNeighbours(TilePoint p)
        {
            foreach (var n in GatheringRules.Neighbours(p)) if (WorldGen.InWorld(n.X, n.Y, 10) && visited.Add(n)) vein.Enqueue(n);
        }
        public bool Step()
        {
            Player p = Main.player[owner]; var state = p.GetModPlayer<ProgressionPlayer>();
            if (!p.active || p.dead || p.CCed || p.noItems || p.noBuilding || !state.SessionReady || state.SessionId != session || state.TalentRevision != revision
                || p.selectedItem != slot || p.HeldItem.type != itemType || !Enabled(mode) || !InReach(p, origin)) return false;
            Limit = Math.Min(Limit, GatheringRules.Limit(mode, level, Config.MaxBlocksPerAction, (long)Main.maxTilesX * Main.maxTilesY));
            if (Removed >= Limit) { Notice(p, "GatheringLimit"); return false; }
            if (current == null) {
                TilePoint candidate;
                if (mode == GatheringMode.Vein) { if (!vein.TryDequeue(out candidate)) return false; }
                else { if (scan == null || !scan.MoveNext()) return false; candidate = scan.Current; }
                if (candidate == origin || !CanTouch(candidate, mode)) return true;
                if (mode != GatheringMode.Area && Main.tile[candidate.X, candidate.Y].TileType != ore) return true;
                current = candidate; currentType = Main.tile[candidate.X, candidate.Y].TileType; attempts = 0;
            }
            var pos = current.Value;
            if (!CanTouch(pos, mode) || Main.tile[pos.X, pos.Y].TileType != currentType) { current = null; return true; }
            if (Hit(p, pos, mode)) {
                Removed++; current = null;
                if (mode == GatheringMode.Vein) AddNeighbours(pos);
            }
            else if (pickDamage <= 0 || ++attempts >= 100) current = null;
            return true;
        }
        public void Dispose() => scan?.Dispose();
    }
}
