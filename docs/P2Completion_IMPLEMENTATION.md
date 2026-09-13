# P2 completion batch 1 — 0.7.0

Based on accepted P2-C / PR #8 merge `936f076ab16d827dc99805155ee0e34ab5be73bb`. Eight new entries, 79 total. Save v3 retained; protocol 9 requires all peers on 0.7.0. This batch awaits its own in-game acceptance and must not be merged automatically.

| Entry | Cost | Default / boundary |
|---|---|---|
| FlightTime | 1/level, adjustable | +60 ticks/level to existing functional wing capacity. No airborne refill on purchase/increase; reduction clamps fuel. No wings, mounts or rocket boots granted. |
| FlightSpeed | 1/level, adjustable | Existing wing horizontal speed/acceleration and standard ascent parameters ×(1 + 0.05L). Special native hover modes can bypass standard ascent; no blanket mod-wing promise. |
| SwimSpeed | 1/level, adjustable | Native collision movement in ordinary water ×(1 + 0.1L). Does not grant flippers, breathing or a transformation. |
| PlacementSpeed | 1/level, adjustable | Nonweapon/nonmining placeable tiles/furniture/torches use speed ×(1 + 0.2L). |
| WallPlacementSpeed | 1/level, adjustable | Placeable walls use speed ×(1 + 0.2L), independent of tiles and demolition. |
| NightVision | 2 once | Native nightVision flag, no physical light. |
| SelfLight | 2 once | Native Lighting.AddLight at player center, RGB 0.8/0.95/1 (Shine light); no buff slot. Visible light depends on the client's renderer. |
| DangerSense | 2 once | Native dangerSense flag for recognized hazards/traps. No additional charge for duplicate trap detection. No wire editor. |

The first three numeric defaults come from the design catalog. The two placement defaults are implementation choices for this acceptance build. All eight entries are in Utility, grouped under movement/environment/tools. Disabling/refunding never clears real equipment or potion flags. Body light stops on disable/death; native potion light remains independent.

## Integration and bounds

`ExplorationPlayer.PostUpdateEquips` modifies wing capacity after equipment. Fuel is only clamped, never increased; native landing, grapples and real equipment retain their refill behavior. UnlimitedFlight remains an independent existing talent. Capacity conversion reserves 4096 ticks below int.MaxValue for native arithmetic.

`ExplorationItem` uses official GlobalItem horizontal/vertical wing hooks. No global WingStats mutation, no persistent velocity multiplication and no inherited ground run bonus. Growth only operates with an equipped functional wing and without a mount. Native special jetpack/hover logic outside those hooks is preserved. Extreme speed/ascent input parameters saturate at 256; the safe numeric representation is bounded, talent levels are not.

Swimming has no single native all-axis speed attribute. Generated On hooks wrap WaterCollision and DryCollision (the native merman/ignoreWater/trident path). The requested velocity is scaled before native collision and divided back after it, including in finally. Position is changed only by native collision. Native WaterCollision does not perform swept high-speed checks, so boosted water moves use at most 32 native steps of up to 8 px. Native floating-tube buoyancy is deferred to once after velocity restoration; DryCollision already has native high-speed subdivision. This preserves walls and avoids multiplicative velocity carry across ticks. Swimming changes water movement including vertical displacement; it does not change the gravity setting. Mounts, grappling, pulley, active dash, death and lava/honey/shimmer are excluded. Per-axis requested collision displacement is limited to 256 px/tick; existing faster motion is left untouched. Actual liquid detection and collision remain native. This custom adapter has compatibility grade C.

Placement uses `GlobalItem.UseSpeedMultiplier`, preserving relative use/animation timing and native tileSpeed/wallSpeed equipment multipliers without being limited to their vanilla 3x stat cap. Select by createWall/createTile metadata; reject damaging weapons and pick/axe/hammer items. Material costs, range, support requirements and tile protection remain native; never loops additional placements. Maximum input multiplier 1,000,000 gives the native one-tick minimum without division overflow. Mods overriding the entire placement system need separate compatibility testing.

API reference: [GlobalItem](https://docs.tmodloader.net/docs/stable/class_global_item.html), [ModPlayer](https://docs.tmodloader.net/docs/stable/class_mod_player.html); actual signatures and call order verified against pinned tModLoader v2026.07.3.0 assemblies.

## Validation and remaining work

[CI 34774979949](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34774979949) on `6a2c0b489dd406fa1f0aa97b6179be4e50eefe7b` passed 2956 core checks and 398 native checks, with zero warnings/errors in both builds. The first native run exposed water-collision tunneling; bounded substeps fixed it, and the unchanged wall assertion now stops exactly at the wall. The final PR #9 body/Actions run records the delivered SHA and includes an additional once-per-tick buoyancy regression. Tests cover paid costs/refunds, current intensity, native save/import, wing capacity with no refill, real WingMovement and ItemLoader hooks, linear native water/merman movement, collision with a wall, independent item/animation timing, actual wall placement and server confirmation. Previous P0/P1/P2-A/B/C checks remain enabled. Player download contains no CI harness; source ZIP contains exact committed blobs.

Headless CI cannot verify the visual appearance of lighting, dangersense or night vision, actual two-client interpolation or arbitrary third-party content. Use the Chinese guide for these tests. Previously deferred tiered crit, potion duration/fishing yield, multiplayer private drops and multi-stage bosses remain separate. Remaining P2 candidates/scanner and P3 are not included. AutoJump and world-chest enhancement stay cancelled.
