## 0.12.0 开发与验收批次：工具强度、地形爆破、默认快捷键

2026-09-14：已核对PR #14合并，main为`a196415d158ca1a772fdf67ef994cf08e59a41ca`，0.11.1／92项已实机验收。本批用户另行确认规则并授权开发，独立分支`feat/p3-tool-power-blast`，新PR未经授权不得合并。下文旧版“获准合并／未合并／不包含下一批”是历史记录，不覆盖本条。

0.12.0新增镐力、斧力（每级1点、+10个百分点）和爆破范围（每级1点、地形半径+1格），共95项，存档v3、协议15。全部无限升级、可调强度、实际投入退款；爆破限六种原版炸弹／雷管，只扩展地形外圈，共用保护和分帧预算。补齐P／左Alt／G默认键一次，保留已有改键和迁移后的主动清空。

功能提交`305bec0700bdfba4b0c61f05c01ed3e1dcf52c8d`已通过[CI 34855726423](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34855726423)：3237项核心、1169项原生检查，正式及测试MOD编译零警告、零错误。后续验证文档提交不改功能代码，复用此证据；详见docs/P3_TOOL_POWER_BLAST_VERIFICATION_zh-CN.md。玩家实机待验收。药水病、旧钓鱼产量仍未实机验收；真实多人暂缓；特殊Boss覆盖、多人独立掉落增产未完成；分层暴击暂缓；特殊斧头批量种树未适配。

设计见[工具与爆破规则](docs/P3_TOOL_POWER_BLAST_DESIGN_zh-CN.md)，验收见[中文操作步骤](docs/P3ToolPowerBlast_TEST_GUIDE_zh-CN.md)。

## 0.11.1已实机验收，PR #14获准合并

2026-09-14：用户确认“测试通过。开始合并”，本批实机验收通过并明确授权合并[PR #14](https://github.com/zhu530319195-max/TerrariaProgression/pull/14)。实际合并提交以PR记录为准。此记录覆盖下文历史“待验收／待复验／不执行合并”措辞。

验收包及功能代码提交：`5695d484083bf97486eb4fbf9c31eec8bdf72038`。[分支CI 34848427906](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34848427906)和[PR CI 34848430955](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34848430955)均成功：3197项核心、966项原生检查，正式及测试MOD编译零警告、零错误。本次验收记录仅修改Markdown文档，功能代码与验收包一致，复用以上有效证据。

当前基线0.11.1，共92项天赋，存档v3，联机协议14。本批涵盖范围收割、自动补种、Alt统一切换式采集、普通补挖／单株收割修复，以及一次+10／+100／+300／+1000级操作。保留实际投入退款、当前强度与批量公共保护规则。

药水病缩短、原有钓鱼产量仍未实机验收；真实多人暂缓实测；特殊／多阶段Boss适配和实测未完成；多人独立掉落增产／按玩家额外抽取尚未实现；分层暴击暂缓修复；特殊斧头自动种树、生长法杖／再生之斧专用入口仍未适配。未知方块类型不因此新增兼容承诺，攻击破坏地形／拆墙建筑扩展仍为候选。本次授权不包含下一批开发。

## 0.11.1 玩家反馈修复与批量升级（待复验）

用户报告Alt开启后部分留块无法手动补挖、单株草药也被“批量未启动”提示拦住，并要求一次升10／100／300／1000级。本版在未合并的PR #14、feat/p3-agriculture继续修复；main仍为已验收0.10.0，不执行合并。

- 批量模式不支持的直接目标改走原生工具入口；不能范围收割的成熟草药，有可用自动补种时走单株事务，否则正常单株采集。只放行鼠标明确指向的一格，其他草药不借此绕过范围幼苗保护。
- 原有家具及支撑、电线、下落方块、特殊地形保护仍只限制批量操作；普通手动操作仍服从原生镐力、破坏许可等规则。范围结束有留块时提供原因类别提示。截图没有方块种类，不能据此宣称所有漏挖情形均已定位。
- 天赋面板增加+10／+100／+300／+1000按钮及总价；点数不足整次不购买，一次性功能不可批量升级。保留已有开关、当前强度和实际付款退款，单请求直接记账，不循环发送千次点击。
- 92项天赋，存档v3；升级数量改用16位字段，协议14，客户端与服务器须同版。1000是单次购买上限，不是天赋等级上限。
- 旧0.11.0的3159核心／927原生证据属于旧包；修复版验证以本次CI及包内BUILD_INFO为准。玩家实机待复验，旧未验收／未完成／暂缓事项保持各自状态。

# ROADMAP.md

## 当前开发：0.11.0 P3农业＋Alt切换式采集

基于已验收并合并的0.10.0，main提交`f28559940bfef5217ecda36356c32268d0009e30`，PR #1～#13均已合并。PR #13最终分支／PR CI：34834854063／34834858202成功，3150核心／651原生；验收包`95e55b29156bad921e77d4d3bef1f1c701d9f359`与main功能代码一致。覆盖下文历史待验收、获准合并和旧main措辞。

用户批准农业规则与Alt改为按一下开、再按一下关，并授权开发。本分支`feat/p3-agriculture`新增范围收割和自动补种，两项整批交付；改造既有范围挖矿／矿脉／伐木的输入。实现后92项天赋，存档v3，联机协议13。详见[农业设计](docs/P3_AGRICULTURE_DESIGN_zh-CN.md)和[中文验收说明](docs/P3Agriculture_TEST_GUIDE_zh-CN.md)。本批自动验证已通过，待玩家实机验收；未获新PR合并授权。

功能代码`7db8aa1c8e4221ee704ea3888e99ad6e83094bb3`已通过[CI 34842171839](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34842171839)：3159项核心、927项原生检查通过；正式MOD与测试MOD编译零警告、零错误。后续验证记录提交仅改文档，功能代码一致。玩家实机验收仍待反馈，新PR保持未合并。

药水病缩短、原有钓鱼产量仍未实机验收；真实多人暂缓实测；特殊／多阶段Boss适配与实测未完成；多人独立掉落增产／按玩家额外抽取仍未完成；分层暴击暂缓修复；特殊斧头自动种树等行为本批不适配。取消事项及未知第三方兼容边界保持原决定。


## 最新已验收批次：0.10.0

用户已批准P3两批安排，并授权第一批：公共保护、范围挖矿、矿脉连锁、一键伐木＋原版鞭子／链锤攻击范围补全。第一批已通过3150项核心、651项原生检查及用户实机验收，PR #13获准合并，第二批范围收割／自动补种未实现。详见[批准设计](docs/P3_GATHERING_DESIGN_zh-CN.md)。旧待定规则记录由本决定覆盖，旧未验收／未完成项目保持原状态。

## 历史审查入口：0.9.0（2026-09-14）

0.9.0已实机验收，PR #12已获合并授权，当前87项天赋。逐项状态、待定规则、完成证据与取消事项统一查阅[中文任务清单](docs/TASK_CHECKLIST_zh-CN.md)。下面阶段描述保留版本演进；不能用历史“planned／待验收”覆盖新验收事实，也不能将单列遗留项目统称通过。

第一步规则已确认，见[下一批P2设计](docs/P2_AFFLICTIONS_DESIGN_zh-CN.md)：NEXT-01异常伤害+20%/级，NEXT-02四种攻击异常2秒/级，NEXT-03渔力+5/级已纳入必做，合计六项新天赋；MAINT-01／02同步维护。第二步P2整批0.9.0已交付并实机验收，PR #12获准合并；第三步合并P3公共保护、范围挖矿、矿脉连锁、一键伐木；第四步收割补种。后两步P3均未开发，P3具体规则在各批次开工前确认。自动魔力药水、减伤、额外攻击等继续留在候选池。

## P0 — Progression Core
Goal: stable character-bound infinite leveling foundation.

Implementation status: P0 0.1.0 is delivered through PR #2 (bootstrap dependency #1); official compilation, 1,537 core assertions and 28 tModLoader runtime assertions passed. On 2026-09-13 the user confirmed singleplayer XP and persistence across death, restart and world changes, and authorized merging. Real multiplayer contribution and segmented/multi-stage Boss tests are explicitly deferred, not passed. See `docs/P0_VALIDATION.md`. P0 is merged; see P1 below. Delivered P2 batches through0.8.2 are described below; P3 remains unimplemented.

- tModLoader project skeleton
- `ModPlayer` progression data
- Level starts at 1; no level cap
- Current XP + total lifetime XP
- XP formula based on NPC `lifeMax`
- Configurable XP per max-HP point: 0.01–10.00, default 1.00
- Quadratic XP requirement formula
- Configurable per-level requirement cap: default 50,000; 0 = uncapped
- Multiple levels from one XP award
- Talent points granted per level
- Character save/load
- Death/world-change persistence
- Multiplayer synchronization foundation
- Server-authoritative XP settlement
- Configurable statue-spawn XP multiplier
- Town-NPC XP exclusion
- Store actual paid talent-point costs for safe refunds
- Generic talent enabled/disabled state
- Basic debug/test commands or diagnostics as needed

## P1 — Numeric Talents
Goal: implement stable numeric talents using the approved Lv.10 strong-state baseline.

P1-A 0.2.0 implements 21 entries and the talent panel; official compilation, 2,058 core checks and 73 runtime checks passed; the user has confirmed the core singleplayer talent interactions and persistence. PR #3 includes the 0.2.1 UI layout refinement for 200% scale, merged after explicit user authorization. Implemented: maximum HP/MP, defense, run speed/acceleration, fixed/natural HP/MP recovery, item HP/MP restoration, generic damage/attack speed/crit damage/armor penetration/knockback, ammo conservation, mana cost, minion/sentry capacity, pickup range. The catalog below is the historical P1 scope; later versions implemented most entries. Current exceptions, including tiered crit, are listed in the numbered checklist. See `docs/P1_IMPLEMENTATION.md`.

0.3.0 adds 24 entries (45 total) and adjustable melee/tool/build strength. Historical exceptions at0.3.0 included exact jump-height scaling, drop-rule adapters and per-recipient private loot. Independent jump height was subsequently cancelled; extra-roll adapters arrived in0.3.1; multiplayer private loot remains incomplete. See `docs/P1_COMPLETION_IMPLEMENTATION.md`; this is not a claim of full third-party compatibility.

0.3.1 P1 refinement: independent extra loot rolls replace DropChance, BagQuantity is added, vanilla shortsword/spear reach is adapted, and native firing intervals are checked. Delivered in PR #5 on the PR #4 base; both are now accepted and merged. No world-chest enhancement or P2/P3 expansion. See `docs/P1_REFINEMENT_0.3.1.md`.

### Base Stats
- Max Life
- Max Mana
- Defense
- Movement Speed
- Movement acceleration
- Jump speed implemented; independent jump height removed from scope
- Breath capacity
- Knockback resistance

### Recovery & Sustain
- Fixed Life regeneration
- Natural Life regeneration multiplier
- Fixed Mana regeneration
- Natural Mana regeneration multiplier
- Healing/mana restoration modifiers
- Potion sickness / Debuff duration reduction where reliable

### Combat
- Global damage
- Attack speed
- Critical strike chance
- Critical damage
- Armor penetration
- Knockback
- Projectile speed where feasible
- Ammo conservation
- Mana cost reduction
- Minion / sentry capacity
- Invulnerability-frame enhancement if compatibility is acceptable
- Layered critical behavior above 100% after compatibility validation

### Economy & Resources
- Coin drop multiplier
- Loot quantity multiplier
- Additional native loot rolls (replaces the retired drop-chance scheme)
- Mining yield
- Wood yield
- Herb / gem / fishing yield
- Pickup range
- Shop / sell / reforge modifiers where reliable

## P2 — Utility / Accessory-like Abilities
Goal: grant permanent utility and accessory-style powers without occupying real accessory slots.

Architecture first:
- `FunctionalTalentRegistry`
- Implementation kinds: `NativeFlag`, `NativeSystem`, `AccessoryBridge`, `Custom`, `Composite`
- Explicit stack/conflict/network policies
- One-time `U` unlocks default to 2 talent points
- Real equipped accessory + talent must not duplicate the same boolean/special effect by default

### P2-A — Highest-confidence effects
0.4.0 implementation: the ten entries below, plus numeric tool destruction efficiency (+20%/level, adjustable) and server points-per-level (default 1, direct integer entry, no gameplay cap). Runtime CI and real-game acceptance are recorded separately in `docs/P2A_IMPLEMENTATION.md`.
- No fall damage
- Unlimited underwater breathing
- Water walking
- Lava-surface walking
- Lava immunity
- Hot-tile/fire-block immunity
- Auto jump — cancelled by user after P2-A testing; refund existing purchases
- Multi-jump integration
- Knockback immunity
- All Information composite

### P2-B — Movement / building / fishing
0.5.0 implementation covers nine new 2-point unlocks. Wall cling and wall slide are independently switchable; existing equipment takes priority. Unlimited flight conserves existing wing fuel and does not grant wings/speed or mount flight. Building helpers are native grid/ruler and auto paint with normal paint consumption. See `docs/P2B_IMPLEMENTATION.md`; accepted by user and merged in PR #7.

- Dash
- Wall climb/slide
- Unlimited flight toggle
- Ice traction
- Selected building helpers
- Fishing-line protection / lava-fishing eligibility where reliable

### P2-C — Composite and combat-trigger effects
0.6.0 implements the common 13-status immunity composite, stars, bees, panic speed and attack burn/poison. Two binary unlocks cost 2 points; four adjustable numeric effects cost 1 point per level. Retaliation grows damage at bounded native counts; attack debuffs last 2 seconds per level. See `docs/P2C_IMPLEMENTATION.md`; accepted and merged in PR #8.
- Common status-immunity composite
- Selected on-hit/on-hurt accessory effects
- Selected attack-inflicted vanilla Debuffs
- Additional vanilla functions explicitly approved after testing

### P2 completion batch 1 — 0.7.0
Accepted and merged in PR #9: wing duration (+1s/level), wing speed (+5%/level), swimming speed (+10%/level), tile/wall placement speed (+20%/level each), night vision, personal light and dangersense. Adjustable numeric entries cost 1/level; binary entries cost 2 once. Native dangersense covers recognized traps without a second paid trap toggle. See `docs/P2Completion_IMPLEMENTATION.md`.

0.8.2 is user-accepted and merged in PR #10, with bait saving/crate eligibility, two-level navigation, client loading correction and hierarchy polish; see docs/P2MenuFishing_IMPLEMENTATION.md. Independent jump height, liquid/honey utility, actuator/wire helpers were removed from future scope by the user. Hover and dodge remain deferred. This batch does not claim the entire P2 catalog is done. Tool destruction efficiency already shipped in P2-A and is not duplicate P3 work.

### P2-D — Developer discovery tooling (0.8.0 implementation)
- `AccessoryTalentScanner`: passive loaded metadata, JSON/CSV and explicit mapped/partial/unmapped report
- Report unmapped vanilla accessory candidates
- Scanner never auto-creates behavior or executes unknown third-party accessory logic
- Third-party auto-import remains experimental/off by default

Authoritative functional catalog:
- `docs/FUNCTIONAL_TALENTS_v0.1.md`

## P3 — Transcendent / World Interaction
Goal: high-power, high-risk abilities with strong multiplayer/world protections.

- Tool destruction speed already shipped in P2-A; no duplicate speed talent
- Area mining with selectable active radius
- Vein mining with scalable chain limit
- One-action tree felling
- Area harvesting
- Auto-replanting
- Attack-driven terrain destruction
- Wall/building expansion helpers
- Protected-object filters
- Server master switches for world-altering abilities
- `MaxBlocksPerAction` performance protection (default 1000; 0 = unlimited)

## Later / Optional
- Presets / build profiles
- Import/export of talent configurations
- Additional mod compatibility adapters only when standard APIs are insufficient
- Explicit support modules for popular content mods
- Experimental third-party accessory bridging only after the vanilla functional registry is stable
- Character statistics and achievements based on lifetime XP
- Prestige/rebirth only if separately approved; it is not part of the current baseline

## Development policy
Each major roadmap item should be implemented in focused branches/PRs. Do not merge until the user explicitly accepts the in-game test result.

