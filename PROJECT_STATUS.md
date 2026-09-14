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

# PROJECT_STATUS.md

## 当前开发：0.11.0 P3农业＋Alt切换式采集

基于已验收并合并的0.10.0，main提交`f28559940bfef5217ecda36356c32268d0009e30`，PR #1～#13均已合并。PR #13最终分支／PR CI：34834854063／34834858202成功，3150核心／651原生；验收包`95e55b29156bad921e77d4d3bef1f1c701d9f359`与main功能代码一致。覆盖下文历史待验收、获准合并和旧main措辞。

用户批准农业规则与Alt改为按一下开、再按一下关，并授权开发。本分支`feat/p3-agriculture`新增范围收割和自动补种，两项整批交付；改造既有范围挖矿／矿脉／伐木的输入。实现后92项天赋，存档v3，联机协议13。详见[农业设计](docs/P3_AGRICULTURE_DESIGN_zh-CN.md)和[中文验收说明](docs/P3Agriculture_TEST_GUIDE_zh-CN.md)。本批自动验证已通过，待玩家实机验收；未获新PR合并授权。

功能代码`7db8aa1c8e4221ee704ea3888e99ad6e83094bb3`已通过[CI 34842171839](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34842171839)：3159项核心、927项原生检查通过；正式MOD与测试MOD编译零警告、零错误。后续验证记录提交仅改文档，功能代码一致。玩家实机验收仍待反馈，新PR保持未合并。

药水病缩短、原有钓鱼产量仍未实机验收；真实多人暂缓实测；特殊／多阶段Boss适配与实测未完成；多人独立掉落增产／按玩家额外抽取仍未完成；分层暴击暂缓修复；特殊斧头自动种树等行为本批不适配。取消事项及未知第三方兼容边界保持原决定。


## 当前已验收：0.10.0 P3采集基础＋攻击范围补全

PR #12已合并，main基线`308621510075c85188bdc546657c4d7a0433f354`。用户批准并授权开发本批：P3-01～04＋RANGE-01／02。独立分支`feat/p3-gathering`；实现90项天赋、存档v3、协议12。自动检查与本批实机验收均已通过，用户已授权合并PR #13。

功能代码提交 `d475f70e8a6defe9bd845884dbdb3290abd4bc0e` 已通过[分支CI 34825975807](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34825975807)和[PR CI 34825980551](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34825980551)：3150项核心、651项原生检查通过，生产与测试MOD编译均为零警告、零错误。

2026-09-14：用户确认0.10.0本批验收通过，并明确授权合并[PR #13](https://github.com/zhu530319195-max/TerrariaProgression/pull/13)。验收包提交 `95e55b29156bad921e77d4d3bef1f1c701d9f359`；本次只更新验收文档，功能代码与验收包一致。合并提交以PR记录为准。

验收提交已通过[分支CI 34826265788](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34826265788)和[PR CI 34826269107](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34826269107)：3150项核心、651项原生检查通过，生产与测试MOD编译均为零警告、零错误。

见[批准规则与实现边界](docs/P3_GATHERING_DESIGN_zh-CN.md)、[中文验收说明](docs/P3Gathering_TEST_GUIDE_zh-CN.md)。P3农业（范围收割、补种）尚未实现。以下0.9.0及旧记录属于历史基线；旧“P3全部未开发／规则待确认”由本节覆盖。所有单列遗留状态继续保留。

## 历史阶段：0.9.0（2026-09-14）

**0.9.0已通过用户实机验收，PR #12已获明确合并授权。** 当前87项天赋，6个一级分类、20个二级分组；存档v3、联机协议11。

2026-09-14：用户确认0.9.0本批测试均已通过，并明确授权合并[PR #12](https://github.com/zhu530319195-max/TerrariaProgression/pull/12)。验收包提交 `4f4a2135b0d48a367bbbbdfb307bf8aa9849df4f`；本次仅补充验收文档，功能代码与测试包一致。

- [分支CI 34813696181](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34813696181)、[PR CI 34813726394](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34813726394)：3112项核心、523项原生检查通过，生产与测试MOD编译零警告、零错误。
- 环境：tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8。
- 上一正式基线为PR #10合并提交 `afe41c470faeb81e14eabbff2f35cf9183487ef1`，0.8.2、81项；历史已验收能力继续保留，不重复开发。
- PR #12包含PR #11的规则文档，合并结果以GitHub PR记录为准。

## 最新任务审查入口

见[中文任务清单](docs/TASK_CHECKLIST_zh-CN.md)、[批准设计](docs/P2_AFFLICTIONS_DESIGN_zh-CN.md)、[实现记录](docs/P2Afflictions_IMPLEMENTATION.md)和[中文验收说明](docs/P2Afflictions_TEST_GUIDE_zh-CN.md)。六项新天赋及两项扫描维护已验收；87项注册由51项数值、36项功能组成。

本次验收覆盖NEXT-01／02／03与MAINT-01／02，即六项新天赋及两项扫描维护。药水病缩短、原有钓鱼产量、真实多人、特殊／多阶段Boss、多人独立掉落增产和分层暴击保留各自未验收、未完成或暂缓状态；可选补验没有单独反馈，不自动改为通过。

下一步进入P3采集基础的规则讨论：公共保护、范围挖矿、矿脉连锁、一键伐木。P3尚未开发；具体范围、数值和世界操作保护待确认。

## 必须单独保留的状态

- 药水病缩短、原有钓鱼产量：已有相关实现，仍未实机验收。
- 真实多人：已有同步／结算基础与自动检查，真实多人实机验证暂缓。
- 特殊／多节／多阶段Boss：已有去重基础及部分自动检查，特殊适配与实机覆盖未完成，不能统称通过。
- 多人独立掉落增产／按玩家额外抽取：未完成，不是仅缺一轮测试。
- 分层暴击：实机无效，按用户决定暂缓修复。
- 自动跳跃、世界探索宝箱强化继续取消；旧自动跳跃投入自动退款。液体／蜂蜜移动辅助、电线／执行器、独立跳跃高度移出计划，已有游泳／起跳速度保留。
- 悬停、闪避暂缓；未知第三方适配未验证。P3采集基础已验收，农业操作尚未开发。

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
第一、二步已完成，0.9.0已获实机验收及PR #12合并授权。下一步集中讨论P3采集基础规则，尚不开发P3。

## P1 merge authorization supersedes earlier notes
The earlier acceptance sections above record history at the time of feedback. PR #4 and #5 were subsequently explicitly authorized and merged. Their tested code is the P2-A base.

