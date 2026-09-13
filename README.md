# TerrariaProgression

A standalone Terraria/tModLoader infinite progression framework.

## Current status

Design/bootstrap phase. No gameplay code is implemented yet.

The authoritative design baseline is:

- `docs/PROGRESSION_DESIGN_v0.1.md`
- `DECISIONS.md`
- `PROJECT_STATUS.md`
- `ROADMAP.md`

## Core direction

- Infinite character level.
- Character-bound progression persists across worlds and multiplayer sessions.
- Kill XP is derived from NPC maximum life with configurable scaling.
- Per-level XP requirement has a configurable default cap of 50,000.
- Level-ups grant talent points.
- Numeric talents are infinitely repeatable by default.
- Default balance target: Lv.1–3 is noticeable, Lv.5 is clearly strong, Lv.10 is very strong; levels above 10 continue scaling without a conventional balance guarantee.
- Talents can be disabled without refunding, or rolled back to refund the points actually paid.
- Six talent families: Base Stats, Recovery, Combat, Economy/Resources, Utility, and Transcendent/World Interaction.
- Dangerous world-changing effects are server-authoritative and independently toggleable.

## Development workflow

`main` is treated as the stable branch. Features are developed on dedicated branches and proposed through pull requests. Gameplay changes should not be merged until implementation/build checks and the requested in-game acceptance testing are complete.

## Scope separation

This repository does not contain Xiaoyu visual replacement, hair, sprite, or PlayerDrawLayer work. Progression is intentionally developed as an independent module so it can run on its own or be integrated with a visual mod later.
