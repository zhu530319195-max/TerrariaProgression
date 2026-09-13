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

当前阶段：**P1-A 0.2.0 第一批数值天赋与天赋页面**，等待实机验收。包含 21 项数值天赋；P1 剩余目录、P2/P3 尚未完成。已合并的 P0 保留原有经验规则与角色成长。

- **按 P 打开天赋页**（可在控制设置中改键），也可输入 `/tptalents`。
- 支持升级、回退一级、整项／分类／全部退款、启停。关闭不退款。
- 旧 P0 角色自动读取，保存为新版 v2；升级前备份角色，新存档不可交给旧 P0 读取。
- [P1 中文安装与测试步骤](docs/P1_TEST_GUIDE_zh-CN.md)
- [P1 工程、网络、已实现项目与后续范围](docs/P1_IMPLEMENTATION.md)
- [P1 验证记录](docs/P1_VALIDATION.md)
- [P0 验证记录及用户暂缓项目](docs/P0_VALIDATION.md)
- 工程位于 `TerrariaProgression/`。目标：tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8。
- `/tpstatus` 查看成长。新角色开启开发命令后 `/tpgivexp 1000`，应为 Lv.4、50/490 XP、3 个天赋点。
