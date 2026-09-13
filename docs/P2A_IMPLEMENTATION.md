# P2-A 0.4.1 implementation and validation

Base: main 30e4da33647afc3b4d25e9b7990bbacf8150b377; PR #4/#5 explicitly approved and merged. Reuses the interrupted P2-A working tree without discarding its changes.

## Scope
47 numeric registry entries (P1's 46 plus ToolSpeed) and 9 functional entries (eight 2-point binary unlocks plus 1-point/level MultiJump), 56 combined entries. Explicit registry declares implementation, group, stacking and authority; no accessory scanner or unapproved P2-B/C/P3 mechanics.

Native flags: noFallDmg, waterWalk2, lavaImmune, fireWalk, noKnockback. Lava-only surface walking sets native waterWalk near a lava surface under normal gravity; it never grants damage immunity. Underwater breathing suspends CheckDrowning without refilling resources or granting the special-seed Gills air-drowning penalty. This suppresses the entire native drowning helper while active, including its equipment side-effects; unfamiliar custom drowning mechanics are not guaranteed.

Information composite supplies 12 native fields, reasserted after native team-info refresh; free persistent child toggles. Toggling off never clears flags supplied by equipment/team. The UI shows correct one-time costs and a scrollable child list, retaining fixed action buttons at high scale.

MultiJump uses native ExtraJump after CloudInABottle, adds one jump per level, supports active intensity. O(1) consumed counter, reusable Available, CanStart limit, native refresh restores uses. Equipment jumps stack. No in-air refill from purchase/enable. Native grapple refresh remains native behavior.

ToolSpeed divides native tool action intervals by 1+0.2L (L=current active level). Scope is ItemCheck_UseMiningTools; ApplyItemTime handles picks/axes/tile pounding, wall-only direct itemTime assignment handled separately after an actual PickWall. Tool power, world protection, normal item/placement timings, hit damage and resource rules remain native. Minimum one-frame timing and other engine/custom-tool behavior bound extreme speed. No mass world editing added.

## Save and authority
DataVersion 3: lifetime TotalTalentPointsEarned plus disabled child IDs. Reads v1/v2; old earned points = Level−1; preserves paid-cost runs/intensity/toggles. Validates available+spent=actually earned instead of recomputing from today's reward setting. Upgrade/refund/toggle transactions remain atomic; binary cap enforced; unknown imported child IDs refused.

Protocol 6; same version required. Server confirms talent operations and grants configured BigInteger rewards. Config text entry accepts nonnegative integers, defaults1, 0 permitted, invalid→1; changes future awards only. No gameplay upper cap; existing 1024-byte integer/60000-byte snapshot protection is retained. At transport capacity an award fails before commit, not silently clamped. Offline save authenticity/native owner-client movement, mining, damage and bag boundaries are unchanged; this is not new anti-cheat or server replay of tool messages.

## Validation
0.4.0 baseline: official MOD build 0 errors, 0 warnings; 2840 core checks passed, including v1/v2 migration, historic reward conservation, huge/zero rewards, binary costs/refunds/caps, child state and jump intensity.
Runtime harness adds actual native config/XP/save, equip/info/drowning hooks, jump lifecycle and real native tool interval checks (pick/axe/hammer), attack interval isolation and pick-power protection. Runtime evidence comes from GitHub CI; local game launch is blocked by the environment's /proc access restriction.

Real keyboard input, lava-surface collision, 200% GUI rendering, two actual clients and arbitrary third-party compatibility still require in-game acceptance. P0 multiplayer/special bosses and P1 deferred crit/potion/fishing tests remain deferred.

API reference: [official ExtraJump](https://docs.tmodloader.net/docs/stable/class_extra_jump.html), plus the pinned v2026.07.3.0 native assembly used by build and runtime tests.

## Authorization / handoff
User explicitly authorized pushing `feat/p2a-functional-talents`, creating its PR and continuing automatic tests on 2026-09-13, with no merge. The earlier automatic-review block is resolved by this instruction. PR #6 is open and must remain unmerged pending user acceptance/authorization.

## Successful CI evidence
[Run 34764075680](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34764075680), code commit `55569cf25cc237aeb5576357fd761e502c4ffaef`, passed official MOD/harness compilation (0 warnings/errors), 2840 core checks and **249 native runtime checks**. The run includes P0/P1 regressions, native ExtraJump start/consume/refresh, child toggles and protocol5 server checks, drowning disable/restore, and configured large/zero point rewards with native SaveData/LoadData.

Native tool action intervals at ToolSpeed Lv10: copper pickaxe 15→5 frames, copper axe 21→7, wooden hammer wall hit 12→4. Ordinary attack interval unchanged, copper pickaxe still cannot break Lihzahrd brick, active ToolSpeed5 yields factor2. This validates native action intervals, not arbitrary custom channel-tool loops or rendered animation speed. No production-code fix was needed after the first CI run.

The above run describes the earlier 0.4.0 baseline. Updated PR HEADs must also pass their triggered workflow; no automated test result substitutes for real GUI/controller/multiplayer acceptance.

## 0.4.1 user acceptance and retirement
User confirmed all other P2-A functions passed in-game testing and requested cancellation of AutoJump. Remove the registry entry, effect and localized UI entries. Decode validates the original save ledger first, then refunds AutoJump's actual cost and removes it; this shared path covers SaveData/LoadData and network imports, and is idempotent. Existing equipment behavior is untouched. Save schema stays v3; protocol becomes6 and all peers must update. The 0.4.0 CI evidence above remains historical; the new retirement/migration CI must pass separately. No merge authorization was given.

0.4.1 local checks: 2848 core assertions pass; official MOD and native harness build with 0 warnings/errors. Updated CI covers native retirement refunds, repeat-load idempotence, server import and preservation of real equipment auto-jump.

0.4.1 retirement verification: [Actions 34765054630](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34765054630) on code `14cd3658a1459c10b49c3f47a29efbd3f17dfeb7` passed all 2848 core and 252 native checks, including existing-character refund, repeat-load idempotence, server import, and preservation of real equipment effects. MOD/harness builds: 0 warnings/errors. User acceptance of the other P2-A functions is retained. PR #6 remains unmerged.
