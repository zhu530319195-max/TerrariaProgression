# PROJECT_STATUS.md

## Current phase
P2-A 0.4.0 on `feat/p2a-functional-talents`, based on main `30e4da33647afc3b4d25e9b7990bbacf8150b377`. User explicitly authorized P2-A after PR #4/#5 acceptance and merge. Implements the ten first functional entries, tool efficiency and configurable points per level. Local implementation committed; official MOD and harness builds pass (0 warnings/errors), 2840 core checks pass. User explicitly authorized pushing P2-A, creating its PR and continuing automatic tests on 2026-09-13. Runtime CI and in-game acceptance pending; do not merge.

## Repository state
- PR #1–#5 are merged. PR #4/#5 merged 2026-09-13 after explicit user instruction “验收通过，可以合并 PR #4、#5”. Earlier historical statements that they await merge are superseded.
- Target unchanged: tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8.
- P1 accepted groups remain accepted; real multiplayer, special bosses, potion sickness/fishing and user-deferred tiered crit remain unverified/deferred.
- Save v3 imports v1/v2 and preserves paid points; once saved in v3, use a backup to return to older MOD versions.
- Protocol 5; all peers must update. Server confirms talent purchases, child toggles and level rewards. Native movement/mining retains the engine's owner-client behavior.
- P2-A implementation and limits: `docs/P2A_IMPLEMENTATION.md`; Chinese tests: `docs/P2A_TEST_GUIDE_zh-CN.md`.

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
- Critical chance: +10 percentage points / level (user revision 2026-09-13).
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

## User acceptance update (2026-09-13)
- Confirmed in 0.3.0: critical chance, ordinary wooden-sword swing size, tool reach, mining/wood yield, purchase and selling prices. Potion sickness and fishing lack user test conditions. The user subsequently accepted 0.3.0 as a stage. The latest message corrects the earlier report of copper yield failure.
- Tiered critical damage did not work in the user test; the user explicitly deferred fixing it because the separate critical-damage talent provides adjustment. Do not report tiered crit as accepted.
- In 0.3.0, thrusting shortswords were outside the ordinary-swing adapter. The 0.3.1 adapter now passes user testing for vanilla thrusting shortswords and spears.
- No new merge authorization for PR #4. Unpublished mining diagnostics were discarded after the correction.

## 0.3.1 user acceptance
- User confirmed successful tests for extra loot rolls (10/20 levels and original reward pools), bag quantity and stacking, vanilla thrusting shortsword/spear reach, and ordinary gun/bow/magic firing speed.
- Tested code: fcd77c0c09d06e7636e3c3b5ee9fa33a51535140. CI run 34761264199 passed 2826 core checks and 198 native runtime checks.
- This confirmation covers the four listed feature groups; it does not claim completion of deferred multiplayer, potion sickness, fishing capture yield, tiered critical damage or arbitrary mod compatibility tests.
- PR #4 and dependent PR #5 remain open. No explicit merge authorization in this feedback.

## Next implementation task
0.4.0 local test/source packages prepared. Next: push the authorized P2-A branch, create PR, run native CI, resolve any failures, then in-game acceptance. Do not begin P2-B/C or P3 until the current batch is reviewed. P2-A contains 47 numeric registry entries and 10 functional registry entries (nine binary unlocks and numeric MultiJump), 57 total.

## P1 merge authorization supersedes earlier notes
The earlier acceptance sections above record history at the time of feedback. PR #4 and #5 were subsequently explicitly authorized and merged. Their tested code is the P2-A base.
