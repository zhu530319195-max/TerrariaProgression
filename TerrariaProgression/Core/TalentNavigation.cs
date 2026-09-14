using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TerrariaProgression.Core;

// Display taxonomy is independent of legacy category numbers and saved talent IDs.
public sealed record TalentMenuGroup(string Id, string CategoryId, IReadOnlyList<string> TalentIds);
public static class TalentNavigation
{
    public static readonly IReadOnlyList<string> Categories = Array.AsReadOnly(new[] {
        "Survival", "Battle", "Movement", "Gathering", "Building", "Trading"
    });
    private static TalentMenuGroup G(string category, string id, params string[] talents) => new(id, category, Array.AsReadOnly(talents));
    public static readonly IReadOnlyList<TalentMenuGroup> Groups = Array.AsReadOnly(new[] {
        G("Survival", "Vitals", "MaxLife", "MaxMana"),
        G("Survival", "Recovery", "LifeRegen", "NaturalLifeRegen", "ManaRegen", "NaturalManaRegen", "Healing", "ManaRestoration", "HeartRecovery", "StarRecovery"),
        G("Survival", "Defense", "Defense", "KnockbackResistance", "KnockbackImmunity", "Invulnerability"),
        G("Survival", "Protection", "Breath", "UnderwaterBreathing", "LavaImmunity", "HotTileImmunity", "StatusImmunity", "DebuffDuration", "PotionDuration"),
        G("Battle", "Offense", "Damage", "AttackSpeed", "CritChance", "CritDamage", "ArmorPenetration"),
        G("Battle", "Handling", "MeleeRange", "ProjectileSpeed", "Knockback"),
        G("Battle", "Summoning", "AmmoSaving", "ManaSaving", "Minions", "Sentries"),
        G("Battle", "Afflictions", "AfflictionDamage", "AttackBurn", "AttackPoison", "AttackFrostburn", "AttackCursedInferno", "AttackVenom", "AttackIchor"),
        G("Battle", "Retaliation", "StarRetaliation", "BeeRetaliation", "PanicSpeed"),
        G("Movement", "Ground", "MoveSpeed", "Acceleration", "Dash", "IceTraction"),
        G("Movement", "Jumping", "JumpSpeed", "MultiJump", "WallClimb", "WallSlide", "NoFallDamage"),
        G("Movement", "Flight", "FlightTime", "FlightSpeed", "UnlimitedFlight", "SwimSpeed", "WaterWalking", "LavaWalking"),
        G("Movement", "Vision", "NightVision", "SelfLight", "DangerSense", "AllInformation"),
        G("Gathering", "Resources", "MiningYield", "WoodYield", "HerbYield", "GemYield"),
        G("Gathering", "Fishing", "FishingPower", "FishingYield", "BaitSaving", "CrateChance", "FishingLine", "LavaFishing"),
        G("Gathering", "Mining", "AreaMining", "VeinMining", "TreeFelling", "BlastRadius"),
        G("Gathering", "Harvest", "AreaHarvest", "AutoReplant", "PickupRange"),
        G("Building", "Tools", "ToolReach", "ToolSpeed", "PickPower", "AxePower"),
        G("Building", "Construction", "BuildReach", "PlacementSpeed", "WallPlacementSpeed", "AutoPaint", "BuildingRuler"),
        G("Trading", "Loot", "Coins", "LootQuantity", "DropChance", "BagQuantity"),
        G("Trading", "Commerce", "BuyDiscount", "SellPrice", "ReforgeDiscount")
    });
    private static readonly Dictionary<string, TalentMenuGroup> byTalent = Groups.SelectMany(g => g.TalentIds.Select(id => (id, g))).ToDictionary(x => x.id, x => x.g, StringComparer.Ordinal);
    public static TalentMenuGroup? Find(string talentId) => byTalent.GetValueOrDefault(talentId);
    public static bool ValidScope(string id, bool group) => group ? Groups.Any(g => g.Id == id) : Categories.Contains(id);
    public static bool InScope(string talentId, string id, bool group) => Find(talentId) is { } g && (group ? g.Id == id : g.CategoryId == id);
    public static BigInteger RefundAmount(ProgressionState state, string scope, bool group) => state.Talents.Where(p => InScope(p.Key, scope, group)).Aggregate(BigInteger.Zero, (sum,p) => sum + p.Value.InvestedPoints);
    public static IReadOnlyList<string> Visible(ProgressionState state, string category, string group, string query, bool ownedOnly, bool enabledOnly, Func<string,string> name)
    {
        query = query.Trim();
        return Groups.SelectMany(g => g.TalentIds).Where(id =>
            (query.Length > 0 ? name(id).Contains(query, StringComparison.OrdinalIgnoreCase) || id.Contains(query, StringComparison.OrdinalIgnoreCase)
                : InScope(id, group.Length > 0 ? group : category, group.Length > 0)) &&
            (!ownedOnly || state.Talents.ContainsKey(id)) &&
            (!enabledOnly || (state.Talents.TryGetValue(id, out var t) && t.Enabled && NumericTalents.EffectiveLevel(t) > 0))).ToArray();
    }
}
