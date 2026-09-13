# PROJECT_STATUS.md

## Current phase
Project bootstrap / design freeze before implementation.

## Repository state
- Default branch: `main`
- Bootstrap branch: `chore/project-bootstrap`
- Open bootstrap PR: #1
- No gameplay code implemented yet.
- Progression design baseline exists in `docs/PROGRESSION_DESIGN_v0.1.md`.

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

## Still not finalized
- Final UI layout and hotkeys.
- Detailed segmented/multi-entity boss settlement handling.
- Exact runtime implementation for dynamic loot probability/quantity modification across vanilla and third-party drop rules.
- Exact compatibility behavior for over-100% critical chance with third-party crit systems.
- Any deliberate exceptions to the default 2-point price for future unusually powerful one-time unlocks.

## Next design task
Review the remaining utility/transcendent catalog for missing abilities or intentional exceptions before implementation begins.

## Next implementation task after design approval
Create the tModLoader project skeleton and P0 progression core: player save data, XP calculation, level-up loop, configurable XP cap, talent point storage, save/load, refund bookkeeping, toggle state, configuration scaffolding, and multiplayer synchronization foundations.
