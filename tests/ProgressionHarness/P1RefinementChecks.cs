using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using TerrariaProgression.Core;
using TerrariaProgression.Talents;

namespace ProgressionHarness;

public sealed partial class RuntimeChecks
{
    private void RunP1Refinement()
    {
        Reset(); A.Award(100000000 * Experience.Scale);
        var p = A.Player; var npc = Spawn(10000); npc.lastInteraction = 0;
        void ClearItems() { foreach (var item in Main.item) item.active = false; Array.Clear(Main.timeItemSlotCannotBeReusedFor); }
        int Count(int type) => Main.item.Take(Main.maxItems).Where(i => i.active && i.type == type).Sum(i => i.stack);
        var db = new ItemDropDatabase();
        db.RegisterToNPC(npc.netID, ItemDropRule.OneFromOptions(1, ItemID.CopperShortsword, ItemID.Spear, ItemID.WoodenBow));
        var solver = new ItemDropResolver(db);
        var info = new DropAttemptInfo { npc = npc, player = p, rng = new UnifiedRandom(42) };
        foreach (int level in new[] { 0, 10, 20 }) {
            if (level > 0) A.ApplyTalent(TalentOperation.Upgrade, "DropChance", TalentCategory.Economy, 10);
            ClearItems(); solver.TryDropping(info);
            Check(Count(ItemID.CopperShortsword) + Count(ItemID.Spear) + Count(ItemID.WoodenBow) == 1 + level / 10,
                "native three-option pool independently rolls " + (1 + level / 10) + " weapons");
        }
        A.ApplyTalent(TalentOperation.Upgrade, "LootQuantity", TalentCategory.Economy, 10);
        ClearItems(); solver.TryDropping(info);
        Check(Count(ItemID.CopperShortsword) + Count(ItemID.Spear) + Count(ItemID.WoodenBow) == 6,
            "three draws stack with doubled quantity exactly once");
        Config.BoostWeapons = false; ClearItems(); solver.TryDropping(info);
        Check(Count(ItemID.CopperShortsword) + Count(ItemID.Spear) + Count(ItemID.WoodenBow) == 1,
            "excluded weapons keep original roll and suppress extra rewards");
        Config.BoostWeapons = true;
        A.ApplyTalent(TalentOperation.Disable, "LootQuantity", TalentCategory.Economy, 1);
        db = new ItemDropDatabase();
        var condition = new LeadingConditionRule(new Conditions.NotExpert());
        var fail = ItemDropRule.Common(ItemID.Wood, int.MaxValue);
        fail.OnFailedRoll(ItemDropRule.Common(ItemID.StoneBlock));
        condition.OnSuccess(fail); db.RegisterToNPC(npc.netID, condition);
        solver = new ItemDropResolver(db);
        int originalMode = Main.GameMode;
        ClearItems(); Main.GameMode = GameModeID.Expert; info.IsExpertMode = true; solver.TryDropping(info);
        Check(Count(ItemID.Wood) + Count(ItemID.StoneBlock) == 0, "extra draws preserve failed difficulty conditions");
        ClearItems(); Main.GameMode = GameModeID.Normal; info.IsExpertMode = false; solver.TryDropping(info);
        Check(Count(ItemID.StoneBlock) == 3 && Count(ItemID.Wood) == 0, "failure chain resolves independently each draw");
        Main.GameMode = originalMode;
        db = new ItemDropDatabase(); db.RegisterToNPC(npc.netID, new UnknownDrop()); solver = new ItemDropResolver(db);
        ClearItems(); solver.TryDropping(info);
        Check(Count(ItemID.DirtBlock) == 1, "unknown rule executes original only without guessed replay");
        db = new ItemDropDatabase(); db.RegisterToNPC(npc.netID, ItemDropRule.Common(ItemID.Wood)); solver = new ItemDropResolver(db);
        info.IsInSimulation = true; ClearItems(); solver.TryDropping(info);
        Check(Count(ItemID.Wood) == 1, "simulation does not gain additional passes"); info.IsInSimulation = false;
        Config.MaxExtraLootRollsPerEvent = 1; ClearItems(); solver.TryDropping(info);
        Check(Count(ItemID.Wood) == 2 && A.State.Talents["DropChance"].TalentLevel == 20, "roll budget bounds work without truncating levels");
        Config.MaxExtraLootRollsPerEvent = 1000;
        A.ApplyTalent(TalentOperation.Disable, "DropChance", TalentCategory.Economy, 1);
        A.ApplyTalent(TalentOperation.Upgrade, "BagQuantity", TalentCategory.Economy, 10);
        ClearItems(); p.QuickSpawnItem(p.GetSource_OpenItem(ItemID.WoodenCrate), ItemID.IronBar, 8);
        Check(Count(ItemID.IronBar) == 16, "eight actual crate contents become sixteen");
        ClearItems(); p.QuickSpawnItem(p.GetSource_OpenItem(ItemID.KingSlimeBossBag), ItemID.SlimeGun);
        Check(Count(ItemID.SlimeGun) == 2 && Main.item.Take(Main.maxItems).Where(i => i.active).All(i => i.stack <= i.maxStack),
            "bag equipment duplication uses separate legal stacks");
        ClearItems(); p.QuickSpawnItem(p.GetSource_Misc("CI ordinary transfer"), ItemID.IronBar, 8);
        Check(Count(ItemID.IronBar) == 8, "ordinary transfers do not receive bag quantity");
        A.ApplyTalent(TalentOperation.Disable, "BagQuantity", TalentCategory.Economy, 1);
        ClearItems(); p.QuickSpawnItem(p.GetSource_OpenItem(ItemID.WoodenCrate), ItemID.IronBar, 8);
        Check(Count(ItemID.IronBar) == 8, "disabled bag talent preserves base contents");
        A.ApplyTalent(TalentOperation.Enable, "BagQuantity", TalentCategory.Economy, 1);
        ClearItems(); p.OpenFishingCrate(ItemID.WoodenCrate);
        Check(Main.item.Take(Main.maxItems).Any(i => i.active) && Main.item.Take(Main.maxItems).Where(i => i.active)
            .GroupBy(i => i.type).All(g => g.Sum(i => i.stack) % 2 == 0), "actual wooden crate table produces doubled contents");
        ClearItems(); p.OpenBossBag(ItemID.KingSlimeBossBag);
        Check(Main.item.Take(Main.maxItems).Any(i => i.active) && Main.item.Take(Main.maxItems).Where(i => i.active)
            .GroupBy(i => i.type).All(g => g.Sum(i => i.stack) % 2 == 0), "actual boss bag table produces doubled contents");
        db = new ItemDropDatabase();
        db.RegisterToItem(ItemID.WoodenCrate, ItemDropRule.AlwaysAtleastOneSuccess(ItemDropRule.OneFromOptions(1, ItemID.Wood, ItemID.StoneBlock)));
        solver = new ItemDropResolver(db);
        var bagInfo = new DropAttemptInfo { player = p, item = ItemID.WoodenCrate, rng = new UnifiedRandom(7) };
        A.ApplyTalent(TalentOperation.Enable, "DropChance", TalentCategory.Economy, 1);
        ClearItems(); solver.TryDropping(bagInfo);
        Check(Count(ItemID.Wood) + Count(ItemID.StoneBlock) == 6, "crate retry wrapper supports three independent draws and doubled contents");
        var tag = new TagCompound(); A.SaveData(tag); B.LoadData(tag);
        Check(B.State.Talents["DropChance"].TalentLevel == 20 && B.State.Talents["BagQuantity"].TalentLevel == 10 && NumericTalents.ValidateImported(B.State),
            "legacy drop ID and new bag talent round trip through native save");
        Main.netMode = NetmodeID.Server;
        Check(!BagLootSystem.CanBoost(p.GetSource_OpenItem(ItemID.WoodenCrate)), "server does not reapply owner-client bag quantity");
        Main.netMode = NetmodeID.MultiplayerClient;
        Check(!ExtraLootSystem.OwnerCanRoll(info), "client cannot replay NPC drops");
        var transport = new RecordingSocket(); Netplay.Connection.Socket = transport;
        p.QuickSpawnItem(p.GetSource_OpenItem(ItemID.KingSlimeBossBag), ItemID.SlimeGun);
        Check(transport.Sent == 2 && Main.item[Main.maxItems].stack == 1, "client sends both equipment copies without corrupting temporary item slot");
        Main.netMode = NetmodeID.SinglePlayer;
        RunThrustAndSpeed();
    }
    private sealed class UnknownDrop : CommonDrop { public UnknownDrop() : base(ItemID.DirtBlock, 1) { } }
    private void RunThrustAndSpeed()
    {
        Reset(); A.Award(100000000 * Experience.Scale); var p = A.Player;
        p.Center = new Vector2(Main.spawnTileX * 16, Main.spawnTileY * 16); p.direction = 1;
        A.ApplyTalent(TalentOperation.Upgrade, "MeleeRange", TalentCategory.Combat, 10);
        foreach (int itemType in new[] { ItemID.CopperShortsword, ItemID.Spear }) {
            p.inventory[0] = new Item(itemType); p.selectedItem = 0;
            p.itemAnimationMax = p.itemAnimation = 30;
            int index = Projectile.NewProjectile(p.GetSource_ItemUse(p.HeldItem), p.Center, new Vector2(2,0), p.HeldItem.shoot, 10, 0, p.whoAmI);
            var projectile = Main.projectile[index];
            Check(ThrustRangeProjectile.Supported(projectile), "native thrust family supported: " + itemType);
            float initialScale = projectile.scale;
            for (int i = 0; i < 5; i++) ProjectileLoader.ProjectileAI(projectile);
            Check(Math.Abs(projectile.scale - initialScale * 2) < .001, "thrust scale doubles without per-tick accumulation: " + itemType);
            var box = projectile.Hitbox; ProjectileLoader.ModifyDamageHitbox(projectile, ref box);
            Check(box.Width >= projectile.width * 2, "native damage broad phase expands with visible thrust");
            var target = new Rectangle((int)projectile.Center.X + projectile.width / 2 + 1, (int)projectile.Center.Y, 1, 1);
            Check(!projectile.Hitbox.Intersects(target) && projectile.Colliding(box,target), "thrust hits target beyond old hitbox");
            A.ApplyTalent(TalentOperation.Disable, "MeleeRange", TalentCategory.Combat, 1);
            ProjectileLoader.ProjectileAI(projectile);
            Check(Math.Abs(projectile.scale - initialScale) < .001, "disable restores native thrust scale");
            A.ApplyTalent(TalentOperation.Enable, "MeleeRange", TalentCategory.Combat, 1);
            projectile.active = false;
        }
        foreach (int type in new[] { ItemID.Minishark, ItemID.WoodenBow, ItemID.WandofSparking }) {
            var weapon = new Item(type);
            p.ResetEffects(); PlayerLoader.PostUpdateEquips(p);
            int original = CombinedHooks.TotalUseTime(weapon.useTime,p,weapon);
            A.ApplyTalent(TalentOperation.Upgrade, "AttackSpeed", TalentCategory.Combat, 10);
            p.ResetEffects(); PlayerLoader.PostUpdateEquips(p);
            int faster = CombinedHooks.TotalUseTime(weapon.useTime,p,weapon);
            Check(faster < original && Math.Abs(p.GetWeaponAttackSpeed(weapon) - 1.3f) < .001,
                $"native weapon use interval shortened: item={type}, frames={original}->{faster}");
            A.ApplyTalent(TalentOperation.RefundTalent, "AttackSpeed", TalentCategory.Combat, 1);
        }
    }
}
