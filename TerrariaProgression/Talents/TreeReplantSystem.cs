using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class TreeReplantSystem : ModSystem
{
    private sealed class Harvest(Player player, TilePoint root, ushort type, HashSet<TilePoint> tiles)
    {
        public readonly Player Player = player;
        public readonly Guid Session = player.GetModPlayer<ProgressionPlayer>().SessionId;
        public readonly TilePoint Root = root;
        public readonly ushort Type = type;
        public readonly HashSet<TilePoint> Tiles = tiles;
        public readonly Dictionary<int, Item> Drops = new();
        public ulong LastTick = Main.GameUpdateCount;
    }
    private static readonly Dictionary<int, Harvest> pending = new();
    private static Harvest? current;
    internal static int PendingCount => pending.Count;
    public override void Load() {
        On_WorldGen.KillTile += Kill;
        On_Item.TryCombiningIntoNearbyItems += Combine;
    }
    public override void Unload() {
        On_WorldGen.KillTile -= Kill;
        On_Item.TryCombiningIntoNearbyItems -= Combine;
        Clear();
    }
    public override void OnWorldUnload() => Clear();
    internal static void Clear() { pending.Clear(); current = null; }
    internal static void Cancel(int owner)
    {
        pending.Remove(owner);
        if (current?.Player.whoAmI == owner) current = null;
    }
    internal static bool Enabled(Player p) => Main.netMode != NetmodeID.MultiplayerClient && p.active && !p.dead && !p.CCed && !p.noItems && !p.noBuilding
        && ModContent.GetInstance<ProgressionConfig>().EnableWorldGathering && ModContent.GetInstance<ProgressionConfig>().EnableAutoReplantTree
        && ExtendedTalentPlayer.Level(p,"AutoReplantTree") > 0;
    internal static int Seed(int type) => type switch {
        TileID.Trees or TileID.PalmTree => ItemID.Acorn,
        TileID.TreeTopaz => ItemID.GemTreeTopazSeed, TileID.TreeAmethyst => ItemID.GemTreeAmethystSeed,
        TileID.TreeSapphire => ItemID.GemTreeSapphireSeed, TileID.TreeEmerald => ItemID.GemTreeEmeraldSeed,
        TileID.TreeRuby => ItemID.GemTreeRubySeed, TileID.TreeDiamond => ItemID.GemTreeDiamondSeed,
        TileID.TreeAmber => ItemID.GemTreeAmberSeed, _ => 0
    };
    private static Harvest? Capture(Player p, int x, int y)
    {
        if (!Enabled(p) || p.HeldItem.axe <= 0 || !WorldGen.InWorld(x,y,10) || !Main.tile[x,y].HasTile || Seed(Main.tile[x,y].TileType) == 0) return null;
        ushort type = Main.tile[x,y].TileType;
        WorldGen.GetTreeBottom(x,y,out int bx,out int by);
        if (!WorldGen.InWorld(bx,by,10) || !Main.tile[bx,by].HasTile || GatheringSystem.NativeTree(Main.tile[bx,by].TileType)) return null;
        var root = new TilePoint(bx,by);
        // A new actual cutter cannot spend another cutter's reserved fresh harvest.
        foreach (int owner in pending.Where(pair => pair.Key != p.whoAmI && pair.Value.Root == root).Select(pair => pair.Key).ToArray()) pending.Remove(owner);
        if (pending.TryGetValue(p.whoAmI,out var existing) && existing.Root == root && existing.Type == type && existing.Session == p.GetModPlayer<ProgressionPlayer>().SessionId) return existing;
        var tiles = new HashSet<TilePoint>();
        for (int ty = by-1; ty >= 10; ty--) {
            bool found = false;
            for (int tx = bx-2; tx <= bx+2; tx++) {
                var tile = Main.tile[tx,ty];
                if (!tile.HasTile || tile.TileType != type) continue;
                WorldGen.GetTreeBottom(tx,ty,out int rx,out int ry);
                if (rx == bx && ry == by) { tiles.Add(new(tx,ty)); found = true; }
            }
            if (!found) break;
        }
        if (!tiles.Contains(new(x,y))) return null;
        return pending[p.whoAmI] = new(p,root,type,tiles);
    }
    private static void Kill(On_WorldGen.orig_KillTile orig, int x,int y,bool fail,bool effectOnly,bool noItem)
    {
        if (!fail && !effectOnly && !noItem)
            foreach (var pair in pending.ToArray())
                if (pair.Value.Tiles.Contains(new(x,y)) && !ReferenceEquals(pair.Value.Player,EconomySystem.Actor)) pending.Remove(pair.Key);
        var old = current;
        if (old == null && !fail && !effectOnly && !noItem && EconomySystem.Actor is Player p) current = Capture(p,x,y);
        try { orig(x,y,fail,effectOnly,noItem); if (current != null) current.LastTick = Main.GameUpdateCount; }
        finally { current = old; }
    }
    internal static void RecordDrop(IEntitySource source,int index)
    {
        if (current is not { } h || index < 0 || index >= Main.maxItems || source is not EntitySource_TileBreak tile || !h.Tiles.Contains(new(tile.TileCoords.X,tile.TileCoords.Y))) return;
        if (Main.item[index].type == Seed(h.Type)) h.Drops[index] = Main.item[index];
    }
    private static void Combine(On_Item.orig_TryCombiningIntoNearbyItems orig,Item item,int index)
    {
        if (pending.Values.Any(h => h.Drops.TryGetValue(index,out var captured) && ReferenceEquals(captured,item))) return;
        orig(item,index);
    }
    public override void PostUpdateWorld() => Process();
    internal static void Process()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        foreach (var pair in pending.ToArray()) {
            var h = pair.Value; var p = h.Player;
            if (!Enabled(p) || p.GetModPlayer<ProgressionPlayer>().SessionId != h.Session || Main.GameUpdateCount-h.LastTick > 1800) { pending.Remove(pair.Key); continue; }
            if (GatheringSystem.HasJob(pair.Key)) continue;
            if (h.Tiles.Any(pos => Main.tile[pos.X,pos.Y].HasTile && Main.tile[pos.X,pos.Y].TileType == h.Type)) continue;
            pending.Remove(pair.Key); // Completion consumes the opportunity, including a failed placement.
            Replant(p,h.Root,h.Type,h.Drops);
        }
    }
    internal static bool Replant(Player p,TilePoint root,int treeType,IReadOnlyDictionary<int,Item> fresh)
    {
        if (!Enabled(p) || !WorldGen.InWorld(root.X,root.Y,10) || Seed(treeType) == 0) return false;
        int x=root.X,y=root.Y-1;
        var config=ModContent.GetInstance<ProgressionConfig>();
        for (int row=y-1;row<=y;row++) {
            var tile=Main.tile[x,row];
            if (tile.HasTile || tile.HasActuator || tile.IsActuated || tile.RedWire || tile.BlueWire || tile.GreenWire || tile.YellowWire
                || config.ProtectedTileAreas.Any(a=>a!=null&&a.Contains(x,row))) return false;
        }
        int seed=Seed(treeType), slot=-1, worldIndex=-1;
        Item? supply=null;
        foreach(var pair in fresh)
            if(pair.Key>=0&&pair.Key<Main.maxItems&&ReferenceEquals(Main.item[pair.Key],pair.Value)&&pair.Value.active&&pair.Value.type==seed&&pair.Value.stack>0) {supply=pair.Value;worldIndex=pair.Key;break;}
        if(supply==null) for(int i=0;i<50;i++) if(p.inventory[i].type==seed&&p.inventory[i].stack>0) {supply=p.inventory[i];slot=i;break;}
        if(supply==null) return false;
        // Use the vanilla seed's object definition and placement validation, not a
        // special axe's UseItem or third-party sapling injection.
        int sapling=supply.createTile, style=supply.placeStyle;
        if(sapling is not (TileID.Saplings or TileID.GemSaplings) || !TileLoader.CanPlace(x,y,sapling)
            || !TileObject.CanPlace(x,y,sapling,style,p.direction,out var data)) return false;
        if(!TileObject.Place(data)) return false;
        WorldGen.SquareTileFrame(x,y);
        if(!Main.tile[x,y].HasTile||Main.tile[x,y].TileType!=sapling) return false;
        supply.stack--;
        if(supply.stack<=0) supply.TurnToAir();
        if(Main.netMode==NetmodeID.Server) {
            NetMessage.SendTileSquare(-1,x,y,5);
            if(slot>=0) NetMessage.SendData(MessageID.SyncEquipment,-1,-1,null,p.whoAmI,slot,p.inventory[slot].prefix);
            if(worldIndex>=0) NetMessage.SendData(MessageID.SyncItem,-1,-1,null,worldIndex);
        }
        return true;
    }
}
