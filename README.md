# TerrariaProgression

Terraria/tModLoader 独立角色成长系统。

核心目标：

- 无限角色等级；
- 击杀经验按 NPC 最大生命动态计算；
- 升级获得天赋点；
- 数值天赋默认允许无限重复升级；
- Lv.10 作为默认“非常强力”的第一阶段完成态，之后继续成长但不强制保持原版平衡；
- 天赋可自由回退、返还实际投入点数，也可在不退款的情况下临时关闭；
- 角色成长跨世界保留，并考虑单人/多人同步；
- 高风险世界修改能力由服务器控制。

默认天赋成本：

- 普通数值成长：通常 1 天赋点 / Lv；
- 一次性功能/饰品式能力：统一 2 天赋点解锁；
- 洗点免费，返还实际支付点数。

## Functional talents

功能型/饰品式能力采用显式 `FunctionalTalentRegistry`，不把未知饰品自动转换成永久天赋。

实现优先级：

1. `NativeFlag` — 直接使用 Terraria/tModLoader 稳定状态；
2. `NativeSystem` — 使用正式子系统（如额外跳跃等）；
3. `AccessoryBridge` — 仅对白名单原版饰品复用效果；
4. `Custom` — 无可靠原生入口时自行实现；
5. `Composite` — 组合多个已注册子效果。

`AccessoryTalentScanner` 只作为开发期候选发现工具：扫描未映射饰品并输出报告，不自动推断效果、不自动执行第三方饰品、不自动向玩家菜单添加未知能力。

第三方饰品自动导入若未来实现，默认关闭并视为实验功能。

## Documentation

- `AGENTS.md` — 工程与协作规则
- `PROJECT_STATUS.md` — 当前状态、已确认规则和下一步
- `ROADMAP.md` — P0–P3 开发路线
- `DECISIONS.md` — 已接受的产品/架构决策
- `docs/PROGRESSION_DESIGN_v0.1.md` — 等级、经验、数值天赋与世界能力总设计
- `docs/FUNCTIONAL_TALENTS_v0.1.md` — 功能天赋正式目录、Registry、Scanner 与兼容策略

当前阶段：**P2-A 0.4.0 功能天赋测试版**。P1 的 PR #4、#5 已通过用户验收并合并。新增九项一次解锁功能、多段跳、方块破坏效率及每级天赋点配置。

- 按 P 或 `/tptalents` 打开页面，“功能能力”中查看分组与免费信息子开关。
- 二元功能2点解锁；多段跳、方块破坏效率每级1点，当前强度可调。
- 存档v3读取v1/v2；升级前备份角色，退回旧MOD需恢复旧备份。多人两端统一更新。
- [P2-A 中文安装与测试](docs/P2A_TEST_GUIDE_zh-CN.md)
- [P2-A 实现、验证与限制](docs/P2A_IMPLEMENTATION.md)
- 目标：tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8。

## P1 补全测试版 0.3.0

新增 24 项，共 45 项数值天赋；暴击率每级 +10 个百分点，新增多重暴击与可调攻击／工具／建造范围。

- [安装与分组测试](docs/P1_COMPLETION_TEST_GUIDE_zh-CN.md)
- [实现与明确未覆盖的项目](docs/P1_COMPLETION_IMPLEMENTATION.md)
- [验证记录](docs/P1_COMPLETION_VALIDATION.md)

### 0.3.1 P1 refinement
Extra loot rolls, container quantities and vanilla thrust reach: [implementation and limits](docs/P1_REFINEMENT_0.3.1.md), [中文安装测试](docs/P1_REFINEMENT_TEST_GUIDE_zh-CN.md). Depends on PR #4; no automatic merge.
