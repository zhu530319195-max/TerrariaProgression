using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class ExtraLootSystem : ModSystem
{
    internal static bool ExtraPass;
    private static int depth;
    private static int ruleSteps;
    private sealed class ExtraRuleBudgetException : Exception { }
    private static bool warnedLimit, warnedRule;
    public override void Load() => On_ItemDropResolver.ResolveRule += Resolve;
    public override void Unload() { On_ItemDropResolver.ResolveRule -= Resolve; Clear(); }
    public override void OnWorldUnload() => Clear();
    private static void Clear() { ExtraPass = warnedLimit = warnedRule = false; depth = 0; }
    internal static bool OwnerCanRoll(DropAttemptInfo info) => !info.IsInSimulation && info.player?.active == true &&
        (info.npc != null ? Main.netMode != NetmodeID.MultiplayerClient :
            BagLootSystem.IsContainer(info.item) && info.player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server);
    internal static void Run(On_ItemDropResolver.orig_TryDropping orig, ItemDropResolver resolver, DropAttemptInfo info)
    {
        bool previous = ExtraPass;
        depth++;
        try {
            orig(resolver, info); // Original event exactly once, including unknown rules.
            if (depth != 1 || previous || !OwnerCanRoll(info)) return;
            var requested = TalentMath.ExtraRolls(ExtendedTalentPlayer.Level(info.player, "DropChance"), info.rng.NextDouble());
            int limit = ModContent.GetInstance<ProgressionConfig>().MaxExtraLootRollsPerEvent;
            if (requested > limit) WarnLimit();
            int rolls = (int)System.Numerics.BigInteger.Min(requested, limit);
            ExtraPass = true;
            ruleSteps = 0;
            for (int i = 0; i < rolls; i++) {
                if (Main.netMode != NetmodeID.MultiplayerClient && !HasRoom()) { WarnLimit(); break; }
                try { orig(resolver, info); }
                catch (ExtraRuleBudgetException) { WarnLimit(); break; }
            }
        }
        finally { depth--; ExtraPass = previous; }
    }
    private static ItemDropAttemptResult Resolve(On_ItemDropResolver.orig_ResolveRule orig, ItemDropResolver resolver, IItemDropRule rule, DropAttemptInfo info)
    {
        // Native crate rules may retry until one child succeeds. An unsupported
        // or impossible child set must not trap extra passes in an infinite loop.
        if (ExtraPass && ++ruleSteps > 100000) throw new ExtraRuleBudgetException();
        // Exact, documented native rule families. Unknown overrides and stateful
        // rules execute in the original event only; never infer them from tooltips.
        if (ExtraPass && !Replayable(rule)) {
            if (!warnedRule) {
                warnedRule = true;
                ModContent.GetInstance<TerrariaProgression>().Logger.Info($"Extra loot skipped unsupported rule {rule.GetType().FullName}; original drop retained.");
            }
            return new ItemDropAttemptResult { State = ItemDropAttemptResultState.DoesntFillConditions };
        }
        return orig(resolver, rule, info);
    }
    internal static bool Replayable(IItemDropRule rule)
    {
        Type type = rule.GetType();
        if (type.Assembly != typeof(CommonDrop).Assembly) return false;
        return type.Name is "CommonDrop" or "CommonDropNotScalingWithLuck" or "ItemDropWithConditionRule" or
            "LeadingConditionRule" or "OneFromOptionsDropRule" or "OneFromOptionsNotScalingWithLuckDropRule" or
            "FewFromOptionsDropRule" or "FewFromOptionsNotScalingWithLuckDropRule" or
            "DropBasedOnExpertMode" or "DropBasedOnMasterMode" or "DropBasedOnExpertModeAndMasterMode" or
            "OneFromRulesRule" or "AlwaysAtleastOneSuccessDropRule" or "SequentialRulesNotScalingWithLuckRule" or
            "DropNothing" or "DropLocalPerClientAndResetsNPCMoneyTo0" or "DropPerPlayerOnThePlayer";
    }
    internal static bool HasRoom(int required = 1)
    {
        for (int i = 0; i < Main.maxItems; i++)
            if (!Main.item[i].active && Main.timeItemSlotCannotBeReusedFor[i] == 0 && --required == 0) return true;
        return false;
    }
    internal static bool AllowExtraSpawn(IEntitySource source, int type, bool privateSpawn)
    {
        if (Main.netMode == NetmodeID.Server && privateSpawn) return false;
        if (type <= 0 || !ContentSamples.ItemsByType.TryGetValue(type, out var sample)) return false;
        NPC? npc = source is EntitySource_Loot loot ? loot.Entity as NPC : null;
        if (!EconomySystem.Allowed(sample, npc)) return false;
        if (Main.netMode != NetmodeID.MultiplayerClient && !HasRoom()) { WarnLimit(); return false; }
        return true;
    }
    internal static void WarnLimit()
    {
        if (warnedLimit) return;
        warnedLimit = true;
        ModContent.GetInstance<TerrariaProgression>().Logger.Warn("Extra loot reached the configured roll budget or native item capacity; excess was not generated. Talent levels and points were unchanged.");
        if (!Main.dedServ) Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.TerrariaProgression.Messages.LootLimit"));
    }
}
