# P1-A 0.2.0 — numeric talents and UI

Scope: 21 implemented numeric talents, a functional categorized panel, server-authorized operations, and P0 character migration. This is the first P1 increment, not the full P1 catalog. P0 XP rules and P1 approved per-level values are unchanged.

## Modules

- `Core/NumericTalents.cs`: explicit registry, category/operation/result contracts, atomic intent processing, numeric and recovery helpers. Every entry costs one point and has MaxLevel=0. No generated dummy talents.
- `Core/ProgressionState.cs` / `StateCodec.cs`: DataVersion=2 uses consecutive paid-cost runs `(Cost, Count)` with BigInteger counts. Equal costs coalesce; refund-one consumes the latest run, refund-all returns the exact sum. This removes the old per-level allocation / 10,000-level history constraint for constant-price talents. Different historical prices are retained.
- `Players/NumericTalentPlayer.cs`: ModifyMaxStats, PostUpdateEquips, PostUpdateRunSpeeds, life regeneration, item healing, ammo consumption, critical damage and mana recovery. Generic DamageClass inheritance is preserved.
- `Talents/NumericTalentItem.cs`: GlobalItem.GrabRange multiplies original pickup distance.
- `UI/TalentUISystem.cs` / `TalentUIState.cs`: native UIState/UserInterface, category list, scrollable detail and operation controls, P key (rebindable), `/tptalents`, English/Chinese. Client-side loading only; lifecycle closes on death/world exit. Both lists scroll; buttons are disabled when unavailable or awaiting confirmation.
- `Players/ProgressionPlayer.cs` / `Networking/ProgressionNetwork.cs`: intent validation, atomic replacement, per-session revision, result acknowledgement and resync. Normal debugging permissions retained.

## Save migration and resource handling

Outer ModPlayer tags and binary payloads accept DataVersion=1 and 2. Legacy P0 point/XP fields and cost lists migrate to v2 in memory and save as v2. Future/corrupt versions are rejected without a silent reset. Keep character backups; P0 cannot read new v2 data. Original design files remain authoritative and unchanged.

The existing transport bounds (60,000-byte snapshot, 1,024 bytes per integer, 512 IDs and 10,000 distinct cost runs) are memory/network protections, not talent level caps. Mutation takes place on a validated copy; insufficient points, invalid intent or transport capacity errors leave the original untouched. Incoming talent IDs must be registered and intensity is currently unsupported, so imported P1 talents cannot introduce hidden behaviors. Offline save authenticity has the same P0 trust boundary.

Maximum life/mana use ModifyMaxStats. No upgrade/toggle command heals. When the native frame rebuilds maximum stats, current resources are clipped downward only. Life restoration uses native regeneration; fixed mana recovery uses a separate fractional 60-tick accumulator; natural mana supplements the final positive native `manaRegen` at +10% per level, retaining its delay and motion/buff calculations. Each addition is O(1), clips to maximum mana, never banks overflow and clears on death/world entry. This does not duplicate or replace vanilla's base recovery.

Only character-owned mana is restored locally, as with Terraria's normal resource synchronization. Server authority covers talent purchases and synchronized levels, while native combat/resource prediction remains owner-client driven. The MOD does not claim a server-side reimplementation of Terraria combat.

## Network protocol v2

All packets start with protocol byte + message byte. A length-prefixed StateCodec payload keeps snapshots bounded even when tModLoader supplies a shared reader.

| Message | Direction | Fields / handling |
|---|---|---|
| JoinCharacter (1) | client → server | validated character save, once per active session; only registered P1 entries accepted |
| Snapshot (2) | server → client | player slot, session GUID, talent revision (UInt64), request acknowledgement (UInt32), result, encoded state |
| TalentAction (3) | client → server | session GUID, expected revision, request ID, operation, registered talent ID/category, bounded count (1–100) |
| RequestSnapshot (4) | client → server | read-only refresh of sender's confirmed state |

Sender identity comes from tModLoader, never a request's player slot. No client XP/point/price/target-level fields exist. Old-session and stale-revision requests cannot spend/refund. Successful mutations increment revision. Server ignores client-authored snapshots. Successful talent changes broadcast confirmed state so remote stat calculations have the same levels; existing players are synchronized to an importing client. XP-only awards continue to send privately to the owner.

Client UI permits one pending operation, with a short cooldown; server bounds mutation work to one request per six simulation ticks. Dropped/rate-limited requests can be recovered with read-only resync after five seconds, with no automatic replay. Read-only resync is rate limited to once per second. TCP preserves order; an older talent revision cannot replace a newer one within a session.

## Stacking and compatibility

- Base HP/MP: additive through permanent-stat modifiers; defense, generic damage, attack speed, armor penetration, critical multiplier, knockback and summon capacities: additive.
- Run speed and acceleration: separate factors after equipment at the documented hook, avoiding vanilla boots erasing a speed bonus; native mounts may override their movement afterward.
- Natural recovery and healing amounts: multiplication of the corresponding original component. Hearts and stars remain separate future talents.
- Ammo consumption chance ×0.93^level; mana cost ×0.95^level. No negative costs. Native rounding / minimum-use-time rules still apply.
- CritDamage adds .05 per level to native HitModifiers.CritDamage (×2 becomes ×2.5 at Lv.10). It does not enable crits for damage classes that cannot crit, override DisableCrit, or implement tiered crit chance.
- Runtime values ultimately enter Terraria int/float fields: conversion is finite/saturating where applicable; max-stat additions reserve integer headroom. BigInteger progression remains unlimited. Extreme levels and combinations with other mods can still exceed engine physics/combat precision, entity counts, or another mod's arithmetic. They are not a guarantee of unbounded physical simulation.

## Remaining P1 work (not changed or silently substituted)

The approved full catalog remains in the design / roadmap. Drop chance must modify rule success separately from quantities; `ItemDropRule` has conditional, chained, per-player and third-party rules, so multiplying NPC drops after the fact would not implement the approved probability formula. Mining/wood attribution also needs its own authoritative source handling. These need a separate P1 implementation and tests, rather than a fake universal multiplier.

Crit chance includes half-percentage levels and >100% tiered behavior. Native crit resolution and third-party DisableCrit/crit modifiers need a dedicated compatibility path; ordinary capped vanilla crit chance would not fulfill the approved infinite scaling. CritChance is therefore not registered in this increment. Other pending P1 entries include jump/breath/knockback resistance, pickup healing, duration reductions, projectile speed, invulnerability frames and shop pricing. P2 registry/effects/scanner and P3 world editing are not implemented.

## Evidence

Official pinned target: tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8. API references: [ModPlayer](https://docs.tmodloader.net/docs/stable/class_mod_player.html), [Player](https://docs.tmodloader.net/docs/stable/class_player.html), [ModSystem](https://docs.tmodloader.net/docs/stable/class_mod_system.html). Build against the actual official release assemblies; unit tests and native harness cover mutations and effects. See `P1_VALIDATION.md` and the Chinese test guide for the exact evidence and outstanding in-game checks.

## 0.2.1 UI refinement

No talent math, save format, gameplay hooks, configuration, or network protocol changes. UI keeps the game's scale / input coordinate system. Category columns respond to viewport width; per-talent actions are outside the scroll container; dynamic text height replaces 400px fixed detail cards. Selection is highlighted and scrollbars draw only for overflowing content. Resync appears only when a multiplayer operation has timed out.
