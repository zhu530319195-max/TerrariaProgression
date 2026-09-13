# AGENTS.md

## Project scope
TerrariaProgression is an independent tModLoader progression module. It must not depend on Xiaoyu visual sprites, PlayerDrawLayer replacements, hairstyles, or other character-art systems.

## Authority order
1. Explicit user instructions in the current task.
2. `docs/PROGRESSION_DESIGN_v0.1.md` for approved progression behavior.
3. `DECISIONS.md` for accepted architectural/product decisions.
4. `PROJECT_STATUS.md` for current implementation state.
5. `ROADMAP.md` for planned work.

If code and an approved design document disagree, stop and surface the mismatch rather than silently changing the design.

## Git workflow
- Do not develop directly on `main`.
- Use focused feature/fix branches and pull requests.
- Do not merge a PR until the user explicitly says the implementation has passed acceptance testing or explicitly authorizes the merge.
- Keep unrelated changes out of a PR.
- Preserve existing user changes.

## Engineering rules
- Target current supported tModLoader APIs; prefer official hooks over fragile reflection or hard-coded vanilla item/NPC lists.
- Character progression data belongs to the player save, not the world save.
- Multiplayer XP and world-changing abilities must be server-authoritative.
- Dynamic modded NPCs/items should be supported through runtime properties/APIs when practical instead of per-mod lookup tables.
- Numeric configuration must be clamped/validated at boundaries.
- Talent enable/disable is not the same as talent refund/respec.
- Refunding must return points actually paid, not points recalculated from current config.
- Avoid hidden healing/resource restoration caused by toggling max-stat talents.
- World-destructive talents must default to safe behavior and respect server controls/protected objects.

## Compatibility and safety
- Do not assume a specific content mod such as Calamity or Thorium is installed.
- Avoid duplicated XP from segmented/multipart bosses, spawned sub-entities, or repeated death callbacks.
- Do not award town-NPC XP by default.
- Statue-spawned NPC XP must obey configuration.
- Preserve character progress across death, world changes, and singleplayer/multiplayer transitions.

## Testing expectations
For every implementation PR, provide concise Chinese test steps understandable to a non-programmer. Include expected results and any known limitations. Compile success alone is not acceptance; in-game verification matters.

## Documentation discipline
When an approved rule changes, update the relevant design/status/decision documentation in the same PR whenever practical.
