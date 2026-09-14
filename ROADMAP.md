# ROADMAP.md

## 当前开发：0.11.0 P3农业＋Alt切换式采集

基于已验收并合并的0.10.0，main提交`f28559940bfef5217ecda36356c32268d0009e30`，PR #1～#13均已合并。PR #13最终分支／PR CI：34834854063／34834858202成功，3150核心／651原生；验收包`95e55b29156bad921e77d4d3bef1f1c701d9f359`与main功能代码一致。覆盖下文历史待验收、获准合并和旧main措辞。

用户批准农业规则与Alt改为按一下开、再按一下关，并授权开发。本分支`feat/p3-agriculture`新增范围收割和自动补种，两项整批交付；改造既有范围挖矿／矿脉／伐木的输入。实现后92项天赋，存档v3，联机协议13。详见[农业设计](docs/P3_AGRICULTURE_DESIGN_zh-CN.md)和[中文验收说明](docs/P3Agriculture_TEST_GUIDE_zh-CN.md)。新批次待自动验证和玩家实机验收，未获新PR合并授权。

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

