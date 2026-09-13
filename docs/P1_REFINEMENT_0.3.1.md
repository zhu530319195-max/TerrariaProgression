# P1 refinement 0.3.1

## Dependency and scope

`feat/p1-loot-and-reach` starts at PR #4 head b2eb14f1671eacb30ad08934d3618428aa1e54af. PR #4 / 0.3.0 is user-accepted but remains unmerged. This PR depends on #4; retarget/rebase after its authorized merge. No auto-merge.

46 registered numeric entries. DropChance retains its save ID but changes display and semantics to extra rolls. BagQuantity is new, +10%/level, cost 1, unlimited. DataVersion stays 2; protocol becomes 4 so a stale client cannot submit a catalog containing unknown talent IDs. No P2/P3, world-chest enhancement, quality/prefix changes or tiered-crit fix.

## Extra rolls

`floor(Level/10) + Bernoulli((Level%10)/10)` additional full native resolver passes. Each pass uses original RNG, conditions, difficulty, options and success/failure chains. There is no per-NPC weapon table. Original passes always run once, unknown rules included. Extra passes permit exact native rule families listed by ExtraLootSystem.Replayable; subclass overrides from content mods are not guessed. Rules outside the resolver, NPC death/XP/progression callbacks, and item consumption are not replayed.

Original ordinary quantity may stack with extra rolls. Disabled category outputs are suppressed only during extra passes; original output remains. NPC rewards use the native drop player on server/singleplayer. Boss private/per-recipient paths are suppressed during server extra passes, preserving original allocation. Common ordinary private-spawn protection from 0.3.0 remains.

Original bag/crate opening is owner-client authoritative in Terraria. Extra rolls and BagQuantity follow that flow using the existing server-confirmed character talents. This does not add a client packet requesting free rewards, but is not server validation of item consumption or an anti-cheat claim. Only vanilla BossBag / IsFishingCrate containers are included; player storage chests, other gifts and unknown mod containers are excluded.

Native repeat-until-success crate rules are supported. Extra passes have a 100000 resolved-rule-call guard to prevent unsupported/impossible nested rules from looping forever. Config MaxExtraLootRollsPerEvent defaults to 1000, range 1–100000, plus native item-capacity checks. Limits abort excess with a notice/log without changing levels or points. Original rule behavior is untouched. Partial rewards already generated before a guard remain; no transactional replay/refund claim.

## Bag quantity

Only EntitySource_ItemOpen for supported containers is multiplied, before native NewItem creates/broadcasts the item. Every output independently uses existing probabilistic quantity math. Non-stackable output is emitted as separate items; overflow is emitted before the primary return so multiplayer slot 400 remains valid for the caller's final SyncItem. Overflow is bounded by native item capacity and avoids reserved item slots in singleplayer.

Native int-ID QuickSpawnItem and native item-drop tables are covered, including ordinary developer-armor spawns from Boss bags. Clone-based custom QuickSpawnItem paths may overwrite stack after item creation and are deliberately not claimed supported; unknown mod containers are excluded. All loot from one bag does not become a whole new bag: no recursive open or additional consumption occurs.

## Thrust reach

GlobalProjectile adapts vanilla ProjectileID entries with native Spear / ShortSword AI only. Each frame restores the previous transform before native AI, then scales native center offset and rendered scale around the owner's mounted anchor. Native timings and secondary projectile behavior remain. Damage broad-phase and spear extension collision receive matching transforms. Current MeleeRange intensity and disable are honored. No guessed third-party projectile adaptation or new network authority claims; each peer uses synchronized character state.

Actual pixel/Rectangle/map numeric limits remain. Infinite talent levels do not imply infinite representable geometry. Other mods overriding AI, drawing or collision can need explicit integration.

## Validation

Production and isolated harness compile against official tModLoader v2026.07.3.0 / Terraria 1.4.4.9 with .NET 8. Core checks include old saves/costs and exact additional-roll boundaries. Native harness checks actual resolver pools/conditions/chains, category exclusion, unknown rules, budgets, real crate/Boss bag contents, MP outgoing item copies, thrust collision/restoration, and CombinedHooks use intervals for gun/bow/magic. Code fcd77c0c09d06e7636e3c3b5ee9fa33a51535140 passed [CI run 34761264199](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34761264199): 2826 core checks and 198 native runtime checks.

The user subsequently confirmed all four delivered feature groups passed in-game testing: extra loot rolls, bag quantity and stacking, vanilla thrusting shortsword/spear reach, and ordinary gun/bow/magic firing speed. This is acceptance of those features, not new evidence for every GUI scaling setting or two physical multiplayer clients. PR #5 remains unmerged pending explicit authorization; it depends on unmerged PR #4.

P0 XP/segmented settlement remains unchanged. Exact +4% physical jump height, other special attack families, arbitrary mod rule compatibility and multiplayer private rewards are not completed by this batch.
