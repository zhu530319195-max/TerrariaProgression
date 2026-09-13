# PROJECT_STATUS.md

## Current phase
P0 Progression Core 0.1.0 implemented and automatically verified. On 2026-09-13 the user confirmed singleplayer XP, death persistence, restart persistence and world-change persistence, and explicitly authorized merging. Real multiplayer contribution and segmented/multi-stage Boss tests are deferred by the user, not passed.

## Repository state
- Default branch: `main`
- Bootstrap branch: `chore/project-bootstrap`
- Bootstrap documentation: PR #1 (dependency of P0 PR #2).
- Integration order authorized by the user: bootstrap PR #1 into `main`, then P0 PR #2 retargeted from `chore/project-bootstrap` to `main`.
- Official target: tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8.
- Local official compilation and `.tmod` packaging: 0 errors, 0 warnings.
- 1,537 core automated assertions and 28 native tModLoader runtime assertions passed in GitHub Actions run 34751256027. Actual two-client play and segmented/multi-stage Boss encounters remain deferred validation items. See `docs/P0_VALIDATION.md`.
- P0 PR: #2; user merge authorization recorded on 2026-09-13. See `docs/P0_VALIDATION.md` for the accepted scope and deferred checks.
- Progression design baseline exists in `docs/PROGRESSION_DESIGN_v0.1.md`.
- Functional/utility talent baseline exists in `docs/FUNCTIONAL_TALENTS_v0.1.md`.

## Confirmed core rules
- Initial level: 1.
- Level cap: none.
- Every level grants talent points; default is 1 point per level.
- Death does not reduce level or experience.
- Progress persists when changing worlds and when moving between singleplayer and multiplayer.
- Base kill XP uses NPC maximum life (`lifeMax`) rather than remaining life.
- XP per NPC max-HP point is configurable from 0.01 to 10.00; default 1.00.
- Per-level XP requirement follows a quadratic curve with a configurable cap; default cap is 50,000 XP and `0` means uncapped.
- Statue-spawned NPC experience is configurable by multiplier.
- Town NPCs give no XP by default.
- Multiplayer XP is based on participation/damage contribution rather than last-hit ownership; server is authoritative.
- Talents can be upgraded, disabled/enabled, and refunded. Disable does not refund points.
- Refunds return the points actually paid.

## Talent model approved
- Six categories: Base Stats; Recovery & Sustain; Combat; Economy & Resources; Utility/Accessory-like Abilities; Transcendent/World Interaction.
- Numeric talents default to unlimited levels (`MaxLevel = 0`).
- Pure binary functionality uses one-time unlocks.
- One-time utility/function unlocks use a unified default price of 2 talent points.
- Toggle/mode talents may combine unlimited progression with a separately adjustable current strength.
- Numeric talent cost is normally fixed rather than increasing with level.
- Default balance target: Lv.1–3 immediately noticeable, Lv.5 clearly strong, Lv.10 very strong; beyond Lv.10 balance is intentionally not guaranteed.

## Approved P1 default examples
- Max HP: +25 / level.
- Max MP: +20 / level.
- Defense: +4 / level.
- Movement speed: +5% / level.
- Fixed life regeneration: +1 HP/s / level.
- Fixed mana regeneration: +2 MP/s / level.
- Global damage: +5% / level.
- Global attack speed: +3% / level.
- Critical chance: +2.5 percentage points / level.
- Critical damage multiplier: +5% / level.
- Armor penetration: +3 / level.
- Monster coin gain: +10% / level.
- Loot/resource quantity talents: generally +10% / level.
- Full formulas and the rest of the current defaults are authoritative in `docs/PROGRESSION_DESIGN_v0.1.md`.

## Functional talent architecture approved
- Use `FunctionalTalentRegistry` as the single explicit registration source for utility/accessory-like powers.
- Prefer `NativeFlag` and `NativeSystem` implementations.
- Use `AccessoryBridge` only for explicit, tested vanilla whitelist entries.
- Use `Custom` for mechanics that cannot be safely represented by stable native hooks.
- Use `Composite` for grouped abilities such as All Information and common status immunity.
- Unknown accessories are never automatically turned into purchasable talents.
- `AccessoryTalentScanner` is a development-time candidate finder only; it does not infer or execute unknown effects.
- Third-party accessory auto-import is experimental and disabled by default.
- Boolean/special effects default to non-duplicating behavior when a real equipped item already provides the same effect.

## Approved first functional groups
- Movement: no fall damage, auto jump, dash, wall climb/slide, ice traction, water/lava surface movement, optional unlimited flight.
- Environment: underwater breathing, lava immunity, hot-tile immunity, danger/trap sensing, optional night vision/light functions.
- Information: one 2-point All Information composite unlock with individually toggleable readouts.
- Immunity: one 2-point common status-immunity composite; knockback immunity remains a separate 2-point toggle.
- Building/tool assistance: numeric reach/speed talents plus selected binary helpers after compatibility testing.
- Fishing/collection convenience: line protection, lava fishing eligibility and other approved utility effects.
- Combat-trigger accessory effects remain in the Combat page rather than bloating the Utility page.

## Still not finalized
- Final UI layout and hotkeys.
- Detailed segmented/multi-entity boss settlement handling.
- Exact runtime implementation for dynamic loot probability/quantity modification across vanilla and third-party drop rules.
- Exact compatibility behavior for over-100% critical chance with third-party crit systems.
- Exact implementation mappings (`NativeFlag` vs `NativeSystem` vs `AccessoryBridge` vs `Custom`) for every P2 functional entry; these are technical tasks, not unresolved product rules.

## Design status
The core progression rules, P1 default balance direction, utility unlock pricing, and functional talent architecture are sufficiently defined to begin implementation. Remaining design questions can be handled as targeted follow-ups without blocking P0.

## Next implementation task
Address any reported P0 issues. P1 is the next planned phase, but requires a separate implementation instruction; P0 merge authorization does not start P1. P0 contains the project skeleton, character save, XP/level/points, generic paid-cost/toggle model, config, owner snapshots and server contribution settlement. No formal talents or effects are registered.

## Later implementation task
Build the P2 `FunctionalTalentRegistry` before implementing the first permanent utility effects. Add `AccessoryTalentScanner` after the registry and core P2 effects are stable; the scanner must not block initial P2 delivery.
