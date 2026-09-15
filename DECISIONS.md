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

## D047 — Next P2 rules approved; consolidate delivery
User explicitly approved the complete proposal after step-one discussion. NEXT-01 is +20% damage per active level (1+0.2L, Lv.10=3x), exclusively for talent-applied OnFire/Poisoned/Frostburn/CursedInferno/Venom damage over time. NEXT-02 adds Frostburn, CursedInferno, Venom and Ichor: each2 seconds per active level; Ichor's defense reduction does not grow. NEXT-03 fishing power is now mandatory, +5 per active level, additive with equipment. All six cost1 point/level, unlimited and adjustable with existing enable/refund semantics; expected count87 only after implementation.

Valid weapon and owned-projectile hits including summons always attempt application, respecting native enemy immunity and coexistence/replacement. Same-status hits refresh rather than accumulate duration or damage instances. Track each valid talent source and its duration; same-status multiplayer extra damage uses the strongest valid source rather than summing/multiplying, weak effects cannot overwrite strong ones, and current enabled intensity controls ongoing extra damage. Disabling/refunding removes that source's extra bonus without clearing the native status. No amplification of weapon-native/environment/unknown-mod statuses, no crit or recursive hit-effect triggers from DoT, and existing kill XP/loot settlement is retained. See docs/P2_AFFLICTIONS_DESIGN_zh-CN.md for the full accepted specification.

Include MAINT-01 actual report version and MAINT-02 known mappings in the same implementation batch. Automatic mana potions, damage reduction and other attack effects remain outside this batch. Consolidated plan: step1 rules (complete), step2 one P2 implementation PR/package, step3 shared world protections plus area mining/vein chaining/tree felling, step4 harvesting/replanting. P3 details require later per-batch decisions. This turn finalizes documentation only; no new gameplay has shipped and PR #11 has not been authorized for merge. D047 supersedes earlier tentative D042/D046 defaults and optional-fishing status, without changing historical acceptance exceptions.


## D048 — 0.9.0原生适配与开发记录（2026-09-14）

- 按用户开发授权落实D047，六项天赋整批交付；功能分支基于PR #11已批准规则文档，不执行任何PR合并。
- 固定原生NPC基础DoT：燃烧4、中毒6、霜冻8、诅咒焰24、毒液30 HP/s；灵液0 DoT。额外伤害按各自原生量的20%×当前强度相加到lifeRegen，不乘整条lifeRegen；涂油等其他效果不倍增。
- 单人使用有效命中回调；多人复用已存在的服务器原生StrikeNPC确认链，来源由服务器记录，不新增客户端自报异常来源消息。NPC额外AI同步只传服务器选出的伤害率，协议升11，存档v3不变。
- GetFishingLevel在固定版本实际收到的是环境倍率；渔力改加Player.fishingSkill，保留原版后续倍率。
- 极端等级只对引擎输出饱和：基础渔力贡献结果最高1,000,000；额外lifeRegen扣减最高120,000,000，并提高原生批量飘字单位以限制内部循环。等级／实际投入仍无限且完整保存，不把饱和上限当等级上限。
- 自动运行及玩家实机验收分开记录；不得自动通过此前药水病、原有钓鱼产量、真实多人和特殊Boss事项。


## D049 — 0.9.0验收与PR #12合并授权

2026-09-14：用户确认0.9.0本批测试均已通过，并明确授权合并[PR #12](https://github.com/zhu530319195-max/TerrariaProgression/pull/12)。验收包提交 `4f4a2135b0d48a367bbbbdfb307bf8aa9849df4f`；本次仅补充验收文档，功能代码与测试包一致。

[分支CI 34813696181](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34813696181)、[PR CI 34813726394](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34813726394)：3112项核心、523项原生检查通过，生产与测试MOD编译零警告、零错误。

本次验收覆盖NEXT-01／02／03与MAINT-01／02，即六项新天赋及两项扫描维护。药水病缩短、原有钓鱼产量、真实多人、特殊／多阶段Boss、多人独立掉落增产和分层暴击保留各自未验收、未完成或暂缓状态；可选补验没有单独反馈，不自动改为通过。

本授权覆盖PR #12及必要验收记录更新；PR #11规则文档已包含在该分支中，不另行执行其合并。下一步P3仅进入规则讨论，不因本次合并授权自动开发。

## D050 — P3批次及攻击范围补全获准开发

用户确认五项P3天赋分两批，确认鞭子、链锤沿用现有MeleeRange补全，随后明确开始开发P3采集基础＋攻击范围补全。规则见`docs/P3_GATHERING_DESIGN_zh-CN.md`，取代旧P3待定记录。第一批P3-01～04及RANGE-01／02合一交付；第二批收割／补种未实现。不自动合并新PR。

## D051 — 0.10.0自动验证与交付边界（历史；验收结果见D052）

功能代码提交 `d475f70e8a6defe9bd845884dbdb3290abd4bc0e` 已通过[分支CI 34825975807](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34825975807)和[PR CI 34825980551](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34825980551)：3150项核心、651项原生检查通过，生产与测试MOD编译均为零警告、零错误。

[PR #13](https://github.com/zhu530319195-max/TerrariaProgression/pull/13)等待玩家实机验收，未合并。本次后续提交只补充文档，功能代码与上述检查一致。最终交付包绑定提交和校验值以包内BUILD_INFO及PR记录为准。

公共保护、三项P3采集与鞭子／链锤修复的自动检查通过，不自动视为玩家实机验收；不合并PR #13。旧未验收／未完成／暂缓事项保持独立状态。

## D052 — 0.10.0验收与PR #13合并授权

2026-09-14：用户确认0.10.0本批验收通过，并明确授权合并[PR #13](https://github.com/zhu530319195-max/TerrariaProgression/pull/13)。验收包提交 `95e55b29156bad921e77d4d3bef1f1c701d9f359`；本次只更新验收文档，功能代码与验收包一致。合并提交以PR记录为准。

验收提交已通过[分支CI 34826265788](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34826265788)和[PR CI 34826269107](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34826269107)：3150项核心、651项原生检查通过，生产与测试MOD编译均为零警告、零错误。

本次验收覆盖P3-01～04公共保护与控制、范围挖矿、矿脉连锁、一键伐木，以及RANGE-01／02原版鞭子和链锤攻击范围补全。90项天赋，存档v3、协议12。此前PR #13待验收／不得合并的记录由本次明确授权覆盖。

范围收割和自动补种留待下一批，尚未开发。药水病缩短、原有钓鱼产量、真实多人、特殊／多阶段Boss、多人独立掉落增产和分层暴击继续保留各自未验收、未完成或暂缓状态，不随本批验收自动通过。本授权不包含下一批开发或其他新PR合并。

## D053 — PR #13合并核对与农业规则／开发授权

PR #13已合并，main为`f28559940bfef5217ecda36356c32268d0009e30`，最终分支／PR CI 34834854063／34834858202均成功；3150核心、651原生，正式与测试MOD编译零警告／错误。验收分支头`d138d4ed5a30a929c29b42b4106a2dd8877c39e4`只比验收包追加七份Markdown文档。旧记录的历史状态不覆盖合并事实。

用户确认：范围收割1点/级、半径+1、无限升级／强度可调；自动补种一次2点；首批原版七种草药。范围只收原生当前能产种子的阶段，幼苗和未到产种阶段跳过；合法地面、黏土盆、种植盆原位补回同种幼苗，每株1枚对应种子。本次收获优先、再用背包，无种子照常收割，补种失败不扣材料。自动补种也适用于普通镐单株收割，不依赖范围收割解锁。沿用产量、实际操作者、服务器许可与预算。

用户将默认左Alt改为切换式开关，统一影响范围挖矿／矿脉／伐木／范围收割；G保留挖矿模式切换。进入世界默认关，关闭立即取消剩余队列，切换显示中文提示；菜单／聊天／界面不触发。自动补种保持独立天赋开关，不受Alt关闭影响。按键可改。

用户随后明确“开始写代码”，授权同批实现、测试、推送和创建PR、交付测试与源码包；不授权合并。存档v3保留，协议升13要求同版客户端／服务器。农业入口首批为普通镐点击，特殊生长工具的附带行为不宣称适配。详情见docs/P3_AGRICULTURE_DESIGN_zh-CN.md。

## 0.11.0自动验证与交付记录

功能代码`7db8aa1c8e4221ee704ea3888e99ad6e83094bb3`已通过[CI 34842171839](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34842171839)：3159项核心、927项原生检查通过；正式MOD与测试MOD编译零警告、零错误。后续验证记录提交仅改文档，功能代码一致。玩家实机验收仍待反馈，新PR保持未合并。

原生覆盖：原有采集／鞭子／链锤回归；3×3／21×21与可调强度；原生草药产种条件；七种草药各自地面及真实黏土盆／种植盆；草药种子增产一次、本次收获优先、背包扣种、无种子与失败不扣；保护区、线路、权限、预算；Alt关停、进世界复位、会话／重放／客户端拒绝、真实普通镐入口及临近玩家归属。真实多人实机仍暂缓，不能以这些检查替代。

## D054 — 0.11.1玩家反馈修复与批量加点

用户报告Alt开启后部分留块无法手动补挖、单株草药也被“批量未启动”提示拦住，并要求一次升10／100／300／1000级。本版在未合并的PR #14、feat/p3-agriculture继续修复；main仍为已验收0.10.0，不执行合并。

- 批量模式不支持的直接目标改走原生工具入口；不能范围收割的成熟草药，有可用自动补种时走单株事务，否则正常单株采集。只放行鼠标明确指向的一格，其他草药不借此绕过范围幼苗保护。
- 原有家具及支撑、电线、下落方块、特殊地形保护仍只限制批量操作；普通手动操作仍服从原生镐力、破坏许可等规则。范围结束有留块时提供原因类别提示。截图没有方块种类，不能据此宣称所有漏挖情形均已定位。
- 天赋面板增加+10／+100／+300／+1000按钮及总价；点数不足整次不购买，一次性功能不可批量升级。保留已有开关、当前强度和实际付款退款，单请求直接记账，不循环发送千次点击。
- 92项天赋，存档v3；升级数量改用16位字段，协议14，客户端与服务器须同版。1000是单次购买上限，不是天赋等级上限。
- 旧0.11.0的3159核心／927原生证据属于旧包；修复版验证以本次CI及包内BUILD_INFO为准。玩家实机待复验，旧未验收／未完成／暂缓事项保持各自状态。


## D055 — 0.11.1实机验收与PR #14合并授权

2026-09-14：用户确认“测试通过。开始合并”，本批实机验收通过并明确授权合并[PR #14](https://github.com/zhu530319195-max/TerrariaProgression/pull/14)。实际合并提交以PR记录为准。此记录覆盖下文历史“待验收／待复验／不执行合并”措辞。

验收包及功能代码提交：`5695d484083bf97486eb4fbf9c31eec8bdf72038`。[分支CI 34848427906](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34848427906)和[PR CI 34848430955](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34848430955)均成功：3197项核心、966项原生检查，正式及测试MOD编译零警告、零错误。本次验收记录仅修改Markdown文档，功能代码与验收包一致，复用以上有效证据。

当前基线0.11.1，共92项天赋，存档v3，联机协议14。本批涵盖范围收割、自动补种、Alt统一切换式采集、普通补挖／单株收割修复，以及一次+10／+100／+300／+1000级操作。保留实际投入退款、当前强度与批量公共保护规则。

药水病缩短、原有钓鱼产量仍未实机验收；真实多人暂缓实测；特殊／多阶段Boss适配和实测未完成；多人独立掉落增产／按玩家额外抽取尚未实现；分层暴击暂缓修复；特殊斧头自动种树、生长法杖／再生之斧专用入口仍未适配。未知方块类型不因此新增兼容承诺，攻击破坏地形／拆墙建筑扩展仍为候选。本次授权不包含下一批开发。


## D056 — 0.12.0工具强度、爆破范围及默认快捷键获准开发
用户确认镐力／斧力每级1点、每级加10个百分点；爆破地形半径每级1点、加1格；无限升级、可调强度、批量加点及实际退款。支持原版炸弹／雷管及各自黏性／弹力版本，只扩地形、不扩伤害和背景墙，扩展服从采集保护及服务器预算，独立于Alt。默认P／左Alt／G自动配置，已有自定义保留，旧空绑定仅补齐一次，之后主动清空保留。用户随后明确“开始开发”，授权独立分支／测试／PR／包交付，不授权合并。详细规则见docs/P3_TOOL_POWER_BLAST_DESIGN_zh-CN.md。


## D057 — 用户确认0.12.1原版地形行为修订
爆破一次性执行扩大后的原生整圆，允许沙子等按原版炸毁，背景墙使用扩大半径的原生判定；取消额外采集保护、分帧和数量截断，改用服务器实际半径上限。挖矿额外保护默认关闭，K及面板免费切换，保留原生镐力与破坏限制、服务器许可、距离及分帧预算。农业／伐木规则保留。详见docs/P3_NATIVE_TERRAIN_REVISION_zh-CN.md。用户授权修改代码，不授权合并。


## D058 — 0.12.1 实机验收与 PR #15 合并授权

2026-09-14：用户确认“测试通过，可以合并”，本批实机验收通过并明确授权合并 [PR #15](https://github.com/zhu530319195-max/TerrariaProgression/pull/15)。实际合并结果和 main 提交以 GitHub PR 记录为准。本记录覆盖下文历史“待复验／未获合并授权”措辞。

验收包提交 `5b2da240778ce47e68cd7bc64a34e11dabaed7a2`；功能提交 `03e66a42a5f4512e7ff2a5ef9f0c9c7fbaac08bc`。[最终分支 CI 34861970946](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34861970946) 和 [PR CI 34861975091](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34861975091) 均成功：3240 项核心、1103 项原生检查，正式及测试 MOD 编译零警告、零错误。本次仅追加 Markdown 验收记录，功能代码与验收包一致，复用以上有效证据。

本批包括镐力／斧力增强、即时原版整圆地形爆破（背景墙沿用原版条件）、默认快捷键修复，以及默认关闭、可用 K／面板切换的额外挖矿保护。版本 0.12.1，共 95 项天赋，存档 v3、联机协议 16。MOD SHA256：`4b6fa3adf99ac7783bd550961adc7865520a53462f4c5ace4146841247f0a822`。

药水病缩短、原有钓鱼产量仍未实机验收；真实多人暂缓实测；特殊／多阶段 Boss 适配与实机覆盖、多人独立掉落增产／按玩家额外抽取尚未完成；分层暴击暂缓；特殊斧头自动种树等附带行为仍未适配。取消事项和第三方兼容边界保持原决定。

下一步仅讨论资源获取天赋的现有覆盖与缺口；本次不授权开发新天赋。


## D059 — 资源补全与服务器等级限制交接及本批授权

用户本轮完整交接确认：基础方块产量+10%/级，不区分天然和玩家放置；七种草药加速+20%/级、固定附近范围、多人取最高；锤力+10个百分点/级；范围拆墙半径+1/级、Alt切换、原生逐墙敲击；独立2点树苗补种。四项新数值均1点/级、默认无限、可调强度、批量与实际退款。树苗补种适用普通与一键伐木，完成残桩清理后再补种，本次合法材料优先、失败不扣、每树最多一次，不执行未知特殊斧头逻辑。

全局等级限制默认-1无限，0禁购禁用，正整数限级；单项覆盖优先，支持相同取值，一次性上限1。限级不删已购／实际投入，不自动退款，不修改玩家保存的强度选择；提高限制恢复原有可用性。批量超限按剩余可升数购买并显示实际价，点数不足整次失败。恢复按钮只重置全局-1与清空覆盖，其他设置及角色数据不变。保留现有服务器配置权限，不新增远程改服权限。

PR #15／main和既有CI已通过GitHub插件核对；验收包至main仅Markdown追加。新分支feat/p3-resources-talent-limits先记录设计及任务，再集中确认剩余规则、实现和交付；未授权合并。具体27种基础方块清单、草药半径50格圆形和宝石树消耗对应宝石橡实是本轮建议，尚未批准。完整规则见docs/P3_RESOURCES_LIMITS_DESIGN_zh-CN.md；当前未修改功能代码。所有取消、暂缓和未验收／未完成边界保留。

## D060 — 本批剩余规则全部确认

用户明确加入泥沙、雪泥、沙漠化石，保留雪块和四种冰块；首批共30种。其余建议全部同意：人物周围半径50格圆形、不要求视线的草药加速；普通树／棕榈用普通橡实，宝石树用同种宝石橡实，每棵1枚。按既有授权继续实现、测试、PR和交付，不合并。

## D061 — 正式命名、0.13.0实机验收与合并授权

2026-09-15：用户确认本批测试成功，确定“无尽潜能 / Boundless Potential”，批准游戏内显示“无尽潜能 · Boundless Potential”，要求现在改动、然后合并、准备正式发布。0.13.1作为更名发布准备版本，玩法保持已验收0.13.0；保留内部TerrariaProgression标识与数据键、存档v3和协议17。授权覆盖PR #16改名后检查通过的合并；“准备发布”不记为已经上传Steam。中英文游戏内／创意工坊介绍与安装发布步骤随源码交付。

## D062 — 可选全服经验共享，每人全额

用户确认每人全额获得经验。新增默认关闭服务器开关，关闭保留贡献分配；开启后合法击杀结算向全服当前在线且角色数据就绪者各发完整预算，不要求参战、距离或同队，死亡等待复活也领取。离线无补发，不统一历史等级，贡献者不重复领。雕像、城镇NPC、遭遇去重与实际归属继续沿用。详见docs/SERVER_SHARED_XP_zh-CN.md；本批独立PR，未授权合并。
