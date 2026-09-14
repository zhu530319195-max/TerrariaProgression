# DECISIONS.md

This file records accepted product/architecture decisions. New entries should be appended rather than silently rewriting history.

## D001 — Independent progression module
TerrariaProgression is developed independently from Xiaoyu visual replacement work. Visual sprites/hair/PlayerDrawLayer are out of scope for this repository.

## D002 — Character-bound infinite level
Progress is stored on the Terraria character, not the world. Initial level is 1 and there is no level cap.

## D003 — No death/world transition penalty
Death, changing worlds, and switching between singleplayer and multiplayer do not reduce level or experience.

## D004 — XP derives from NPC maximum life
Base kill XP uses `NPC.lifeMax`. Default conversion is 1.00 XP per max-HP point. The configurable range is 0.01–10.00.

## D005 — Upgrade requirement curve and cap
Base requirement for the next level is:

`250 + 50 * (level - 1) + 10 * (level - 1)^2`

The actual requirement is capped by a configurable per-level maximum. Default cap: 50,000 XP. Config value `0` means no cap.

## D006 — Statue XP is configurable
Statue-spawned NPC experience is controlled by a multiplier. A value of 0 disables XP; 1.0 means normal XP.

## D007 — Town NPCs do not give XP by default
Town NPC kills are excluded from normal experience to avoid trivial farming.

## D008 — Multiplayer XP is not last-hit-only
XP is allocated by meaningful participation/damage contribution. The server is authoritative for final settlement.

## D009 — Level-up grants talent points
Each level grants talent points. Default: 1 point per level. Large XP awards may produce multiple level-ups in one settlement.

## D010 — Talent disable and refund are different operations
Disabling a talent temporarily suppresses its effect without changing its level or invested points. Refunding lowers/reset levels and returns the points actually paid.

## D011 — Free reversible respec
Current design allows unlimited talent rollback/refund with no additional penalty. Exact paid costs must be stored so configuration changes cannot create refund exploits.

## D012 — Six talent categories
Approved categories: Base Stats; Recovery & Sustain; Combat; Economy & Resources; Utility/Accessory-like Abilities; Transcendent/World Interaction.

## D013 — High-risk world abilities require server authority
Area mining, vein mining, tree felling, terrain destruction, auto-replanting and similar world-changing effects must respect server-side control and protection rules.

## D014 — Mod compatibility should be data/API driven
Prefer standard tModLoader runtime properties and DamageClass/API behavior over hard-coded vanilla or third-party content lists, so modded NPCs/items work automatically when possible.

## D015 — P1 values were initially left open
The initial bootstrap catalog approved the talent categories conceptually while leaving exact P1 values for later design approval.

## D016 — Numeric talents are infinitely repeatable
Supersedes the finite-cap assumption in the initial catalog. Any talent with meaningful continuous numeric scaling defaults to unlimited levels (`MaxLevel = 0`). Pure binary functionality remains an unlock-type ability.

## D017 — Lv.10 is the default strong-state balance target
Default talent values are tuned so Lv.1–3 is immediately noticeable, Lv.5 is clearly stronger than vanilla, and focused investment to Lv.10 is already very strong. Levels above 10 continue to scale but are not required to preserve conventional vanilla balance.

## D018 — Numeric talent prices do not grow with level by default
Normal numeric talents use a fixed talent-point cost per level, normally 1 point. XP progression already supplies the long-term cost curve, so the default design does not add an additional escalating talent-price curve.

## D019 — Approved P1 strength examples
Current default numeric baseline includes: +25 max HP/level, +20 max MP/level, +4 defense/level, +5% movement speed/level, +1 HP/s fixed life regen/level, +2 MP/s fixed mana regen/level, +5% global damage/level, +3% attack speed/level, +2.5 percentage points crit/level, +5% crit multiplier/level, +3 armor penetration/level, +10% monster coins and resource quantity/level. Exact formulas for multiplicative reductions and loot probability are specified in `docs/PROGRESSION_DESIGN_v0.1.md`.

## D020 — Strength ceiling and current active strength are separate
For abilities that can become inconvenient or dangerous at high levels (multi-jump, area mining, vein mining, terrain destruction, etc.), talent level determines the maximum unlocked power while the player can separately choose a lower current active intensity or disable the effect entirely.

## D021 — World-editing growth remains unlimited but performance may be capped per action
Character talent levels are not capped for world-interaction abilities. Servers may independently enforce a per-action world-edit limit such as `MaxBlocksPerAction` for performance and safety. This is a runtime protection limit, not a progression cap.

## D022 — Binary utility unlocks cost 2 talent points by default
One-time functional unlock talents (`U` type), such as no fall damage, unlimited underwater breathing, water walking, lava immunity, accessory-like information functions, and status immunities, use a unified default unlock price of 2 talent points. This keeps utility abilities accessible and avoids unnecessary price tiers. Individual exceptions may only be introduced deliberately if a future ability is materially more powerful than the normal utility set.

## D023 — Functional talents use an explicit registry rather than automatic accessory import
All permanent utility/accessory-like powers are registered through `FunctionalTalentRegistry`. Each effect declares its implementation kind, stacking/conflict policy, network authority and compatibility status. Unknown accessories are never automatically exposed as purchasable talents.

## D024 — Native APIs are preferred over accessory emulation
Functional effects should use `NativeFlag` or `NativeSystem` whenever Terraria/tModLoader provides a stable state or subsystem. `AccessoryBridge` is reserved for explicitly whitelisted vanilla effects that cannot be represented cleanly otherwise. Complex cases use `Custom` implementations. Composite talents may combine already-registered child effects.

## D025 — Accessory scanning is a developer discovery tool only
`AccessoryTalentScanner` may enumerate registered accessories and report unmapped candidates, but it must not automatically generate behavior, spend talent points, run unknown third-party accessory logic, or infer final effects from tooltip text or reflection-based field differences.

## D026 — Third-party accessory auto-import is experimental and off by default
Any future third-party accessory bridge is opt-in/experimental (`ExperimentalImportedAccessories = false` by default). Formal support requires explicit mapping and test evidence. TerrariaProgression does not promise universal compatibility with arbitrary ModItem accessory logic.

## D027 — Information and common status immunity are bundled utility unlocks
To avoid menu bloat, vanilla-style information readouts are grouped under one 2-point `All Information` composite unlock with individually toggleable sub-effects. Common vanilla accessory-style debuff immunities are grouped under one 2-point status-immunity composite unlock, while knockback immunity remains a separate 2-point toggle because it materially changes combat feel.

## D028 — P1 completion scope and revised crit chance (2026-09-13)
User approved combining remaining P1 batches, adding melee swing size/reach (+10% per level, cost 1, unlimited, adjustable active strength), and advancing tool/build reach (+1 tile per level each) into P1. Crit chance is now +10 percentage points per level, superseding D019's +2.5. Lv.10 is a strength target, not a cap. P2 binary powers and P3 world destruction remain excluded. API limitations must be reported rather than changing the approved mechanics. PR #3 was explicitly authorized and merged.

## D029 — Tiered crit fix deferred after in-game feedback (2026-09-13)
The user confirmed critical chance, wooden-sword swing size, tool reach and mining yield. Their latest message supersedes the earlier copper-yield failure report. Tiered crit did not work in their game; they explicitly requested no fix for now because the separate critical-damage talent is available. Preserve the existing crit rules/code for now and mark tiered crit as unaccepted/deferred, not passed. Thrusting shortswords remain outside the implemented ordinary-swing reach adapter. This feedback does not authorize merging PR #4.

## D030 — Additional loot rolls and container contents (user approval)
Replace DropChance behavior with independent extra native rolls: floor(L/10) plus a fractional Bernoulli roll. Preserve the DropChance save ID, paid costs and enable state. Guaranteed three-option pools yield 2 weapons at L10 and 3 at L20; duplicates are allowed. Quantity bonuses stack separately. Add BagQuantity (+10%/level, cost 1, unlimited) for vanilla Boss bag/fishing crate contents, according to the opener. No world-chest enhancement. Independent quality/rarity/prefix talent values remain undefined.

## D031 — P1 refinement scope and implementation limits
P1 refinement precedes P2: extra rolls, container quantity, vanilla shortsword/spear reach, and native gun/bow/magic use-time verification. Preserve user-deferred tiered crit. PR #4 is stage-accepted but has not been merged; the new branch depends on #4. Extra rules use an explicit native-family allowlist. Original unknown rules still execute once. Runtime protection: server config MaxExtraLootRollsPerEvent defaults to 1000 (1–100000), with a 100000-rule-call cycle guard and native item-capacity guard; excess produces a notice/log and does not change levels/points. Container opening follows native owner-client item flow, not a new claim of server-verified item consumption. Multiplayer private-drop replay remains excluded.

## D032 — P1 merge and P2-A authorization (2026-09-13)
User explicitly accepted and authorized merging PR #4 and #5, now merged. User then requested P2-A and added tool destruction efficiency (mining, chopping, building demolition) and a server setting for talent points per level, default 1 and no upper gameplay limit. Existing +20%/level adjustable ToolSpeed implementation is the proposed initial default, awaiting in-game acceptance; it changes action interval, not tool strength, area or placement speed.

## D033 — Lifetime point ledger and functional state
Server reward settings affect future level gains only. Save v3 records lifetime points actually earned, imports v1/v2 as Level−1, and keeps actual paid-cost refunds. Text input uses BigInteger (0 allowed; invalid input becomes 1), retaining existing bounded transport safety; no silent reward clamping. Binary unlocks cost 2 once, numeric MultiJump costs 1 per extra jump. Information sub-toggles are free and persist in the parent talent. Protocol 5 requires same-version peers.

## D034 — Retire AutoJump after acceptance feedback
User confirmed all other P2-A functions tested successfully and requested cancellation of AutoJump. Remove the talent entirely and automatically refund actual paid points from existing validated saves, without changing real equipment behavior or other talents. Repeated loads must not duplicate refunds. This supersedes all earlier AutoJump catalog entries. No merge authorization in this feedback.

## D035 — P2-A accepted and merged; P2-B scope approved
User confirmed final 0.4.1 tests passed and authorized PR #6 merge, completed at bb36b53721db5f4a1758591b2406d6b0c88595d5. User then approved starting P2-B following the proposed movement/building/fishing scope, PR and automatic testing workflow; merge still follows explicit acceptance/authorization.

## D036 — P2-B native effect boundaries
Nine 2-point unlocks: Dash, WallClimb, WallSlide, UnlimitedFlight, IceTraction, BuildingRuler, AutoPaint, FishingLine, LavaFishing. Standard dash adds no hit damage or dodge and preserves equipment precedence and native cooldown. Cling and slide are independent toggles; native cling is the stronger mode and includes wall-jumping. Flight conserves existing wing fuel, never grants wings/speed, mount flight or an on-toggle refill; exhausted wings require native grounding/grapple refresh first. Grid/ruler and auto paint respect native builder buttons; paint consumes inventory supplies through the native placement flow. Lava fishing requires rod/bait and native pond conditions. No new world destruction or P2-C effects. Save v3 retained, protocol raised to 7 for the expanded catalog.

## D037 — P2-B accepted and merged; P2-C started
The user confirmed P2-B tests passed and authorized PR #7 merge, completed at a3ac45aeccb739b42bbcf3d317a7ced45075b539. The user then requested P2-C. Continue branch/PR/automatic tests and deliver an unmerged acceptance build.

## D038 — P2-C implementation defaults and boundaries
Implement six catalog entries: 2-point StatusImmunity with 13 free independent native-status switches; 2-point PanicSpeed using native eight-second Panic; four 1-point-per-level adjustable combat effects. Stars/bees scale native retaliation damage by active level and retain bounded native counts. Real registered accessories supply their existing projectile batch, scaled once; talent-only bees grant no Honey healing. AttackBurn/AttackPoison apply native OnFire/Poisoned for 2 seconds per active level, with max-duration refresh and normal NPC immunities; native damage over time is unchanged. These are implementation defaults for this test batch, not new user-specified numerical requirements. No dodge, unknown debuff immunity, PvP attack status hooks, scanner or P3 work. Save v3 retained; protocol 8 requires same-version peers.

## D039 — P2-C accepted; completion batch 1 authorized
User confirmed P2-C tests passed and authorized PR #8 merge (936f076ab16d827dc99805155ee0e34ab5be73bb). User then approved continuing P2 after the proposed wing duration/speed, swimming, night vision/light/dangersense and tile/wall placement batch. Deliver an unmerged PR with automatic tests and player/source packages.

## D040 — Completion defaults and native boundaries
Existing catalog values retained: FlightTime +1s/level, FlightSpeed +5%/level, SwimSpeed +10%/level, all adjustable and 1 point/level. PlacementSpeed and WallPlacementSpeed use initial test default +20%/level, independently adjustable. Three vision functions cost 2 once. Dangersense already includes known native traps; do not charge twice for a duplicate flag. Wing capacity only applies to real functional wings and never refills on toggle/increase; actual landing/equipment controls refill. Placement uses native item speed hooks and metadata, keeps animation/use timers relative, native material consumption and validity. Swimming scales native collision displacement and restores simulation velocity each call, including native merman/ignoreWater paths; no breathing/jump/equipment effects added. Exclude non-water liquids, mounts, hooks and dashes. Extreme movement/wing parameters are bounded for engine safety, timers have a one-tick floor, while stored levels have no gameplay cap. Save v3 / protocol 9.


## D041 — PR #9 accepted and merged
User confirmed all eight 0.7.0 additions passed and authorized PR #9 merge at 1301fea8a9805347150d204c36d5e47191859d24. Final tested head11c207ad162e92610c4f3d54c6f96592f5d523e0, CI34775077627 (2956 core/399 native). Prior pending statements are historical and superseded; previously deferred areas are not thereby accepted.

## D042 — Two-level menu and candidate pruning approved
User approved six purpose-based categories, nested subgroups, left navigation/middle talents/right details, category All, global search, owned/active filters, remember the last group on reopening, and clearly labeled talent/group/category refunds showing actually paid amounts. No empty/unimplemented groups or third nesting level. Preserve saved identifiers, levels, switches, current intensity and refunds. Exact mapping is TalentNavigation; group/category refunds are validated server-side. Legacy category enum values stay stable; new display scopes use separate identifiers.
Liquid/honey movement helper, wire view, actuator helper and independent jump height leave the development plan. Existing JumpSpeed/SwimSpeed remain. Hover and dodge are deferred. User requested a later shared abnormal-damage talent; +20%/level is a proposal for that later batch, not implemented here.

## D043 — 0.8.0 fishing defaults and passive scanner approved
BaitSaving:1 point/level, unlimited, adjustable; original consumption probability multiplied by0.93^activeLevel. CrateChance:1 point/level, unlimited, adjustable; original failed crate-gate probability multiplied by0.95^activeLevel, preserving downstream native rewards/conditions. Normal existing native catches execute once; no direct final-item replacement. Follow existing owner-client fishing and confirmed character state. Both return to native behavior when off/zero.
P2-D reuses FunctionalTalentRegistry. Scanner reads loaded metadata only, outputs explicit source mappings/partial mappings/unmapped candidates and compatibility grades without claiming test acceptance. Never execute unknown accessory hooks, infer behavior from tooltips, generate purchasable talents or change player state. Client-only active-culture text is reported honestly, other-language names may be unavailable. Branch/test/PR/package workflow is authorized; merge remains explicit.

## D044 — 0.8.1 accepted and navigation polish approved
User confirmed 0.8.1 testing passed, then approved a small UI-only refinement: entire second-level buttons inset20 UI pixels with right edges aligned, All renamed 本类全部, right/down expansion triangles, softer parent highlight with gold text and full gold selected-child border, and extra space after expanded children. Preserve the three-column layout and font sizes. Implement as0.8.2 in PR #10; no explicit merge authorization was given. Earlier deferred/unaccepted gameplay items retain their individual status.

## D045 — PR #10 accepted and merged
User confirmed0.8.2 testing passed and explicitly authorized PR #10 merge. Merged at `afe41c470faeb81e14eabbff2f35cf9183487ef1`; accepted head `0bf00456c72ddedaa119d1f8a0864de37f0f3649`; CI34805851271/34805853679 passed3060 core and440 native checks, zero build warnings/errors. This supersedes earlier pending merge/acceptance notes. Existing deferred/unaccepted/incomplete items retain their individual status.

## D046 — Post-scan task review and next-batch priority
User accepted prioritizing abnormal-attack completion and treating fishing-power improvement as an optional addition, then requested a repository-synced task checklist for review. NEXT-01 adds damage strengthening separately from duration; +20%/level remains a proposal, with affected statuses, source attribution and stacking unresolved. NEXT-02 needs a concrete additional-status list and rules; NEXT-03 is optional, not a committed batch requirement. Other accessory effects remain discussion candidates. Record the hard-coded scanner version bug as MAINT-01 and incomplete mappings as MAINT-02; this documentation task does not fix code. See docs/TASK_CHECKLIST_zh-CN.md for stable task IDs, stage exceptions and evidence. Do not auto-execute unknown accessory effects or treat unmapped entries as missing talents.
