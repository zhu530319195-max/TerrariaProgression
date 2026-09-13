# TerrariaProgression

A standalone Terraria/tModLoader progression module focused on infinite character growth, configurable XP, reversible talents, and optional high-power utility/world interaction abilities.

## Project status

Design/bootstrap phase. Gameplay code has not been implemented yet.

The authoritative design baseline is maintained in:

- `docs/PROGRESSION_DESIGN_v0.1.md`
- `DECISIONS.md`
- `PROJECT_STATUS.md`
- `ROADMAP.md`

## Core direction

- Infinite character level
- Character-bound progression across worlds
- No XP or level loss on death
- XP derived from NPC maximum life
- Configurable XP-per-HP multiplier
- Configurable per-level XP requirement cap (default 50,000)
- Talent points gained on level-up
- Numeric talents are repeatable without a level cap by default
- Lv.10 is the default strong-state balance target; higher levels may become intentionally extreme
- Normal numeric talents usually cost 1 point per level
- One-time utility/function unlocks cost 2 talent points by default
- Talent effects can be disabled without refunding them
- Refund/respec is free and reversible
- Multiplayer XP and world edits use server authority

## Workflow

Development should use feature branches and pull requests. Do not develop directly on `main`. User-facing gameplay changes should be tested in tModLoader before merge whenever practical.
