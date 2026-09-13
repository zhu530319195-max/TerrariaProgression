# PROJECT_STATUS.md

## Current phase
P1 refinement 0.3.1 on `feat/p1-loot-and-reach`, based on PR #4 head b2eb14f1671eacb30ad08934d3618428aa1e54af. PR #4 / 0.3.0 is user-accepted as a stage; not merged. New scope: extra loot rolls replacing DropChance semantics, BagQuantity, vanilla thrust/spear reach and firing-speed verification. 46 numeric entries. User confirmed all four 0.3.1 feature groups passed in-game testing; PR #5 awaits explicit merge authorization. See `docs/P1_REFINEMENT_0.3.1.md` for validation and limits. No P2/P3.

## Repository state
- Default branch: `main`; PR #1, #2 and #3 merged with explicit user approval.
- P1 completion branch: `feat/p1-numeric-completion`, based on main `5f23b0fa544726106e88cb2b9d5db999c7a8b797`.
- Official target unchanged: tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8.
- P0 user-confirmed: correct XP; death/restart/world-change persistence. Real multiplayer contribution and segmented/multi-stage Boss tests deferred by user.
- P1 save format v2 imports v1 and compresses historical costs; old P0 cannot load a newly saved v2 character.
- P1 numeric operations are server-confirmed. GUI: P (rebindable) or `/tptalents`.
- See `docs/P1_IMPLEMENTATION.md`, `docs/P1_VALIDATION.md` and `docs/P1_TEST_GUIDE_zh-CN.md`.
- Do not merge P1 until separately authorized after in-game acceptance.

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
0.3.1 test and source packages delivered; the four feature groups passed real-game testing. Await explicit merge authorization for PR #4 and PR #5. Exact jump height, other special projectile reach and multiplayer private drops remain scoped follow-ups; tiered crit remains deferred.

## Later implementation task
Build the P2 `FunctionalTalentRegistry` before implementing the first permanent utility effects. Add `AccessoryTalentScanner` after the registry and core P2 effects are stable; the scanner must not block initial P2 delivery.
