# PROJECT_STATUS.md

## Current phase
Project bootstrap / design freeze before implementation.

## Repository state
- Default branch: `main`
- Bootstrap branch: `chore/project-bootstrap`
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

## Talent categories approved
1. Base Stats
2. Recovery & Sustain
3. Combat
4. Economy & Resources
5. Utility / Accessory-like Abilities
6. Transcendent / World Interaction Abilities

## Not finalized yet
- Exact default P1 talent values.
- Exact talent point costs and cost curves.
- Exact max levels for most finite talents.
- Final UI layout and hotkeys.
- Detailed handling of segmented bosses and unusual multi-entity encounters.
- Exact loot multiplication implementation strategy.

## Next design task
Finalize P1 talent defaults: effect per level, default cost, max level, and any soft/hard caps for Base Stats, Recovery, Combat, and Economy talents.

## Next implementation task after design approval
Create the tModLoader project skeleton and P0 progression core: player save data, XP calculation, level-up loop, configurable XP cap, talent point storage, save/load, and multiplayer synchronization foundations.
