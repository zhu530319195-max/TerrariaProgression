> 0.9.0已通过用户实机验收，共87项天赋；[PR #12](https://github.com/zhu530319195-max/TerrariaProgression/pull/12)已获合并授权。见[实现记录](docs/P2Afflictions_IMPLEMENTATION.md)与[任务清单](docs/TASK_CHECKLIST_zh-CN.md)。旧的未验收／未完成事项继续单列。

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
- [最新任务清单（中文）](docs/TASK_CHECKLIST_zh-CN.md) — 已完成、下一批、候选、遗留验证与取消事项
- `ROADMAP.md` — P0–P3 开发路线
- `DECISIONS.md` — 已接受的产品/架构决策
- `docs/PROGRESSION_DESIGN_v0.1.md` — 等级、经验、数值天赋与世界能力总设计
- `docs/FUNCTIONAL_TALENTS_v0.1.md` — 功能天赋正式目录、Registry、Scanner 与兼容策略
- [下一批P2已确认规则](docs/P2_AFFLICTIONS_DESIGN_zh-CN.md) — 异常伤害、四种新异常、渔力与扫描维护（待开发）

当前阶段：**0.8.2 二级天赋菜单、钓鱼与饰品扫描**。PR #1～#10已验收合并。本批新增鱼饵节约与宝匣概率，共81项；此前单列的未验收／未完成项目仍按各自状态保留。

- 六类二级目录、全局搜索、已购买/已启用筛选，菜单重开保留当前会话选择。
- 分组/分类退款明确显示范围与实际投入；旧角色数据保留。
- 开发指令 `/tpscanaccessories` 导出被动候选报告；`/tpfishsample` 提供单人钓鱼测试物资。
- 存档v3、协议10；所有端更新至0.8.2。
- [中文安装与验收](docs/P2MenuFishing_TEST_GUIDE_zh-CN.md)
- [实现、验证与限制](docs/P2MenuFishing_IMPLEMENTATION.md)
- 本版已获用户实机验收并合并PR #10；下一批六项新天赋和两项扫描维护的规则已确认，整批开发尚未开始。
