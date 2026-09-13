# P2-B 0.5.0 implementation

Base: merged PR #6, main bb36b53721db5f4a1758591b2406d6b0c88595d5. User approved this batch after accepting P2-A. P2-B itself is not yet accepted or authorized to merge.

## Implementation
Nine additions to FunctionalTalentRegistry, each MaxLevel=1, cost=2, individually enabled/refunded using the existing paid-cost ledger. 65 combined entries: 47 numeric registry + 18 functional registry (one is numeric MultiJump).

| ID | Native integration | Conflict / boundary |
| --- | --- | --- |
| Dash | Scoped dashType=1 at On_Player.DashMovement | Existing nonzero identifier wins, including custom negative markers; keep cooldown, no damage/dodge grant |
| WallClimb | Scoped spikedBoots tier 2 at WallslideMovement | Native cling and wall jumps; stronger of actual equipment and talent |
| WallSlide | Scoped spikedBoots tier 1 | Native slow slide and wall jumps; cling wins if also enabled |
| UnlimitedFlight | On_Player.WingMovement conserves pre-call fuel | Existing wings only; no speed, refill, mount or standalone rocket-boot power |
| IceTraction | iceSkate | Satisfy once |
| BuildingRuler | rulerGrid/rulerLine | Existing ruler remains free; native builder buttons retain user preferences |
| AutoPaint | autoPaint | Native placement/paint inventory consumption and networking; no existing-building repaint |
| FishingLine | accFishingLine | Native retrieval branch; does not remove bait requirements |
| LavaFishing | accLavaFishing | Native FishingAttempt evaluates pond, pole, bait and permission |

No AccessoryBridge or unknown third-party code is executed. Dash and wall values are scoped to native consumers after equipment updates and restored in finally blocks. Flight does not refill on enabling; already exhausted wings need a normal landing/grapple refresh. Native Soaring Insignia refill remains valid. Existing numerical wing duration/speed talents remain independent.

## Save and network
Save format remains v3, imports v1/v2, retains AutoJump one-time retirement refund. Protocol 7 prevents old peers from rejecting new IDs mid-session; all peers update to 0.5.0. Unlock/toggle requests still require server-confirmed session and revision. Movement, inventory fishing and paint placement retain native owner-client networking. This is not a new server-authoritative anti-cheat layer.

## Verification
Core tests cover each new unlock price, repeat purchase rejection, disabled save/import and actual-cost refund (2884 total core checks). Runtime harness calls actual native dash, wall-jump, wing movement, fishing retrieval/pond attempts and wall placement entry points, plus server toggle/import and equipment isolation. Automated pass results and exact commit are attached to the PR/CI and BUILD_INFO.txt in produced packages. The test-only harness is never bundled into the player package.

Real game acceptance remains necessary for controls, ice feel, display toggles, painting tiles/furniture, lava fishing waiting, 200% UI scale and real multiplayer position/paint observation. Third-party mods with independent dash/flight implementations need explicit testing. Previously deferred P0/P1 checks remain deferred.
