# P2-A 0.4.0 implementation and validation

Base: main 30e4da33647afc3b4d25e9b7990bbacf8150b377; PR #4/#5 explicitly approved and merged. Reuses the interrupted P2-A working tree without discarding its changes.

## Scope
47 numeric registry entries (P1's 46 plus ToolSpeed) and 10 functional entries (nine 2-point binary unlocks plus 1-point/level MultiJump), 57 combined entries. Explicit registry declares implementation, group, stacking and authority; no accessory scanner or unapproved P2-B/C/P3 mechanics.

Native flags: noFallDmg, autoJump, waterWalk2, lavaImmune, fireWalk, noKnockback. Lava-only surface walking sets native waterWalk near a lava surface under normal gravity; it never grants damage immunity. Underwater breathing suspends CheckDrowning without refilling resources or granting the special-seed Gills air-drowning penalty. This suppresses the entire native drowning helper while active, including its equipment side-effects; unfamiliar custom drowning mechanics are not guaranteed.

Information composite supplies 12 native fields, reasserted after native team-info refresh; free persistent child toggles. Toggling off never clears flags supplied by equipment/team. The UI shows correct one-time costs and a scrollable child list, retaining fixed action buttons at high scale.

MultiJump uses native ExtraJump after CloudInABottle, adds one jump per level, supports active intensity. O(1) consumed counter, reusable Available, CanStart limit, native refresh restores uses. Equipment jumps stack. No in-air refill from purchase/enable. Native grapple refresh remains native behavior.

ToolSpeed divides native tool action intervals by 1+0.2L (L=current active level). Scope is ItemCheck_UseMiningTools; ApplyItemTime handles picks/axes/tile pounding, wall-only direct itemTime assignment handled separately after an actual PickWall. Tool power, world protection, normal item/placement timings, hit damage and resource rules remain native. Minimum one-frame timing and other engine/custom-tool behavior bound extreme speed. No mass world editing added.

## Save and authority
DataVersion 3: lifetime TotalTalentPointsEarned plus disabled child IDs. Reads v1/v2; old earned points = Level−1; preserves paid-cost runs/intensity/toggles. Validates available+spent=actually earned instead of recomputing from today's reward setting. Upgrade/refund/toggle transactions remain atomic; binary cap enforced; unknown imported child IDs refused.

Protocol 5; same version required. Server confirms talent operations and grants configured BigInteger rewards. Config text entry accepts nonnegative integers, defaults1, 0 permitted, invalid→1; changes future awards only. No gameplay upper cap; existing 1024-byte integer/60000-byte snapshot protection is retained. At transport capacity an award fails before commit, not silently clamped. Offline save authenticity/native owner-client movement, mining, damage and bag boundaries are unchanged; this is not new anti-cheat or server replay of tool messages.

## Validation
Local official MOD build: 0 errors, 0 warnings. Core: 2840 checks passed, including v1/v2 migration, historic reward conservation, huge/zero rewards, binary costs/refunds/caps, child state and jump intensity.
Runtime harness adds actual native config/XP/save, equip/info/drowning hooks, jump lifecycle and real native tool interval checks (pick/axe/hammer), attack interval isolation and pick-power protection. Runtime results must be recorded from CI; local game launch is blocked by the environment's /proc access restriction, so local compilation is not called runtime success.

Real keyboard input, lava-surface collision, 200% GUI rendering, two actual clients and arbitrary third-party compatibility still require in-game acceptance. P0 multiplayer/special bosses and P1 deferred crit/potion/fishing tests remain deferred.

API reference: [official ExtraJump](https://docs.tmodloader.net/docs/stable/class_extra_jump.html), plus the pinned v2026.07.3.0 native assembly used by build and runtime tests.

## Authorization / handoff
User explicitly authorized pushing `feat/p2a-functional-talents`, creating its PR and continuing automatic tests on 2026-09-13, with no merge. The earlier automatic-review block is resolved by this instruction. Runtime CI remains pending until actual run evidence is recorded below.
