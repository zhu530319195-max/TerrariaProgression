# PROJECT_STATUS.md

## 当前阶段（2026-09-14）

**0.8.2已通过用户实机验收并合并PR #10，PR #1～#10均已合并。** 当前81项天赋，6个一级分类、20个二级分组。0.8.1客户端加载修复和0.8.2目录缩进／高亮优化均已验收；此事实覆盖旧阶段文档中的“PR #10未合并／待验收”。

- main合并提交：`afe41c470faeb81e14eabbff2f35cf9183487ef1`。
- 验收分支提交：`0bf00456c72ddedaa119d1f8a0864de37f0f3649`。
- [分支CI 34805851271](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34805851271)、[PR CI 34805853679](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34805853679)：3060项核心、440项原生检查；生产与测试MOD编译零警告、零错误。
- 环境：tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8；存档v3、协议10。现有ID、等级、实际成本、开关与强度保留。
- PR #9八项补全能力已验收合并，0.8.2继续保留，不重复开发。

## 最新任务审查入口

本分支实现0.9.0 P2异常／渔力批次，共87项天赋（51项数值注册、36项功能注册），存档v3、协议11；当前进入自动验证，尚未实机验收或合并。正式已验收main仍是上面的0.8.2。

见[中文任务清单](docs/TASK_CHECKLIST_zh-CN.md)、[批准设计](docs/P2_AFFLICTIONS_DESIGN_zh-CN.md)、[实现记录](docs/P2Afflictions_IMPLEMENTATION.md)和[中文验收说明](docs/P2Afflictions_TEST_GUIDE_zh-CN.md)。本批NEXT-01／02／03已实装：异常伤害+20%/级，霜冻／诅咒焰／毒液／灵液2秒/级，渔力+5/级；均1点/级、无限、可调。保留已有燃烧／中毒ID、存档及投入。

MAINT-01读取实际加载版本；MAINT-02补充31项明确映射（信息读数、免疫子项、钓鱼和建筑工具）。原报告450项、映射11／部分16／未映射423仅作为0.8.2历史样本，不冒充新报告结果；扫描仍不执行未知第三方饰品逻辑。

## 必须单独保留的状态

- 药水病缩短、原有钓鱼产量：已有相关实现，仍未实机验收。
- 真实多人：已有同步／结算基础与自动检查，真实多人实机验证暂缓。
- 特殊／多节／多阶段Boss：已有去重基础及部分自动检查，特殊适配与实机覆盖未完成，不能统称通过。
- 多人独立掉落增产／按玩家额外抽取：未完成，不是仅缺一轮测试。
- 分层暴击：实机无效，按用户决定暂缓修复。
- 自动跳跃、世界探索宝箱强化继续取消；旧自动跳跃投入自动退款。液体／蜂蜜移动辅助、电线／执行器、独立跳跃高度移出计划，已有游泳／起跳速度保留。
- 悬停、闪避暂缓；未知第三方适配未验证。P3世界操作全部未开发。

## Confirmed core rules
- Initial level: 1.
- Level cap: none.
- Every level grants talent points; default is 1 point per level.
- Death does not reduce level or experience.
- Progress persists when changing worlds and when moving between singleplayer and multiplayer.
- Base kill XP uses NPC maximum life (`lifeMax`) rather than remaining life.
- XP per NPC max-HP point is configurable from 0.01 to 10.00; default 1.00.
- Per-level XP requirement follows a quadratic curve with a configurable cap; default cap is 50,000 XP and `0` means uncapped.
- Statue-spawned NPC experience is configurable by multiplier.
- Town NPCs give no XP by default.
- Multiplayer XP is based on participation/damage contribution rather than last-hit ownership; server is authoritative.
- Talents can be upgraded, disabled/enabled, and refunded. Disable does not refund points.
- Refunds return the points actually paid.

## Talent model approved
- Current display categories: 生存与恢复、战斗与召唤、移动与探索、采集与钓鱼、建筑与工具、掉落与交易. Legacy saved enum values remain unchanged.
- Numeric talents default to unlimited levels (`MaxLevel = 0`).
- Pure binary functionality uses one-time unlocks.
- One-time utility/function unlocks use a unified default price of 2 talent points.
- Toggle/mode talents may combine unlimited progression with a separately adjustable current strength.
- Numeric talent cost is normally fixed rather than increasing with level.
- Default balance target: Lv.1–3 immediately noticeable, Lv.5 clearly strong, Lv.10 very strong; beyond Lv.10 balance is intentionally not guaranteed.

## Approved P1 default examples
- Max HP: +25 / level.
- Max MP: +20 / level.
- Defense: +4 / level.
- Movement speed: +5% / level.
- Fixed life regeneration: +1 HP/s / level.
- Fixed mana regeneration: +2 MP/s / level.
- Global damage: +5% / level.
- Global attack speed: +3% / level.
- Critical chance: +10 percentage points / level (user revision 2026-09-13).
- Critical damage multiplier: +5% / level.
- Armor penetration: +3 / level.
- Monster coin gain: +10% / level.
- Loot/resource quantity talents: generally +10% / level.
- Full formulas and the rest of the current defaults are authoritative in `docs/PROGRESSION_DESIGN_v0.1.md`.

## Functional talent architecture approved
- Use `FunctionalTalentRegistry` as the single explicit registration source for utility/accessory-like powers.
- Prefer `NativeFlag` and `NativeSystem` implementations.
- Use `AccessoryBridge` only for explicit, tested vanilla whitelist entries.
- Use `Custom` for mechanics that cannot be safely represented by stable native hooks.
- Use `Composite` for grouped abilities such as All Information and common status immunity.
- Unknown accessories are never automatically turned into purchasable talents.
- `AccessoryTalentScanner` is a development-time candidate finder only; it does not infer or execute unknown effects.
- Third-party accessory auto-import is experimental and disabled by default.
- Boolean/special effects default to non-duplicating behavior when a real equipped item already provides the same effect.

## Approved first functional groups
- Movement: no fall damage, dash, wall climb/slide, ice traction, water/lava surface movement, optional unlimited flight.
- Environment: underwater breathing, lava immunity, hot-tile immunity, danger/trap sensing, optional night vision/light functions.
- Information: one 2-point All Information composite unlock with individually toggleable readouts.
- Immunity: one 2-point common status-immunity composite; knockback immunity remains a separate 2-point toggle.
- Building/tool assistance: numeric reach/speed talents plus selected binary helpers after compatibility testing.
- Fishing/collection convenience: line protection, lava fishing eligibility and other approved utility effects.
- Combat-trigger accessory effects remain in the Combat page rather than bloating the Utility page.

## Still not finalized
- P3具体范围、费用和保护规则在对应批次开工前集中确认；本批P2规则已见D047和专项设计，当前UI与P热键已验收。
- Detailed segmented/multi-entity boss settlement handling.
- Additional-loot-roll compatibility beyond the implemented native families, and per-player private reward expansion; do not restore the retired drop-chance scheme.
- Exact compatibility behavior for over-100% critical chance with third-party crit systems.
- Exact implementation mappings (`NativeFlag` vs `NativeSystem` vs `AccessoryBridge` vs `Custom`) for every P2 functional entry; these are technical tasks, not unresolved product rules.

## Design status
P0/P1 and the delivered P2 batches are stage-accepted through0.8.2, subject to the explicit exceptions above. New development follows the numbered task checklist; proposed mechanics need specific decisions before implementation.

## User acceptance update (2026-09-13)
- Confirmed in 0.3.0: critical chance, ordinary wooden-sword swing size, tool reach, mining/wood yield, purchase and selling prices. Potion sickness and fishing lack user test conditions. The user subsequently accepted 0.3.0 as a stage. The latest message corrects the earlier report of copper yield failure.
- Tiered critical damage did not work in the user test; the user explicitly deferred fixing it because the separate critical-damage talent provides adjustment. Do not report tiered crit as accepted.
- In 0.3.0, thrusting shortswords were outside the ordinary-swing adapter. The 0.3.1 adapter now passes user testing for vanilla thrusting shortswords and spears.
- No new merge authorization for PR #4. Unpublished mining diagnostics were discarded after the correction.

## 0.3.1 user acceptance
- User confirmed successful tests for extra loot rolls (10/20 levels and original reward pools), bag quantity and stacking, vanilla thrusting shortsword/spear reach, and ordinary gun/bow/magic firing speed.
- Tested code: fcd77c0c09d06e7636e3c3b5ee9fa33a51535140. CI run 34761264199 passed 2826 core checks and 198 native runtime checks.
- This confirmation covers the four listed feature groups; it does not claim completion of deferred multiplayer, potion sickness, fishing capture yield, tiered critical damage or arbitrary mod compatibility tests.
- PR #4 and dependent PR #5 remain open. No explicit merge authorization in this feedback.

## Next implementation task
第一步规则定稿已完成。下一步按 docs/P2_AFFLICTIONS_DESIGN_zh-CN.md 整批实现NEXT-01／02／03和MAINT-01／02，统一交付。当前文档PR #11尚未合并，本次规则确认不等于合并授权。

## P1 merge authorization supersedes earlier notes
The earlier acceptance sections above record history at the time of feedback. PR #4 and #5 were subsequently explicitly authorized and merged. Their tested code is the P2-A base.

