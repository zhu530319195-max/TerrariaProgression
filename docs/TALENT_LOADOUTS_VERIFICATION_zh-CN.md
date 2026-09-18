# 0.15.1 名称输入与快捷键修订验证

2026-09-18。设计提交466b64e7ad395a8a78fbc7a7d7a4e29af75eeb3d先记录D064，再实现；构建提交`444442add6a3faa32af0c3e66b6aa65b531713cc`，PR #18未合并。存档v4／协议18不变。用户报告的“可以删除不能输入”使0.15.0不能视为已实机通过，需使用本版复验。

检查了固定版本原生Main、PlayerInput及UIFocusInputTextField：Mod UI.Update早于PlayerInput.UpdateInput，后者重置WritingText，随后聊天绘制会根据该标记开关IME。修复为绘制阶段读文本，在PostUpdateInput维持焦点并占用Main.CurrentInputTextTakerOverride，防止回车选字打开聊天、清掉字符缓冲。搜索使用同一规则。

[CI35334035900](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/35334035900)成功：4013核心、1849原生（本次增加61项），正式及测试MOD编译均零警告、零错误。原生GetInputText检查使用实际字符队列，覆盖提交后的中文、字母数字、32字符长度、代理对安全截断和失焦保留；原生OpenPlayerChat验证输入占用时不抢走字符。快捷键验证涵盖格式解析、左右Shift/Ctrl、额外修饰键隔离、按住与先按主键不触发、新绑定持久化、清空不补回、重复列表优先、数字快捷栏拦截、当前／不存在方案不执行、删除序号前移、死亡／就绪／等待网络限制，以及11项输入与界面状态门禁。复用原方案全部保存、退款、切换不回复与采集停止的回归。

这不等于Windows输入法候选窗、实际面板点击与真人多人已实测。请用户按新说明先复验改名，再试默认Shift+1／Shift+2和自定义Ctrl+F1。其他未验收／未完成／暂缓边界保持。

[交付产物10542258727](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/35334035900/artifacts/10542258727)通过摘要与ZIP完整性校验。完整源码157个Git文件逐一与构建提交比对；玩家包不含测试Harness。BUILD_INFO中的`513bd5e07122c42e47d9b5404a54e961709d88ca`是GitHub临时PR测试提交，不是main合并。后续仅更改Markdown证据，代码和玩家包一致。

| 文件 | SHA256 |
|---|---|
| BoundlessPotential_0.15.1_Test.zip | `2bd4a7a173efba21133e7add5f36ffef382fbbbbf973d442305c8c5ad2565572` |
| BoundlessPotential_0.15.1_Source.zip | `20870b5578288103745695e186c96a2d1db7f8da982d300c4d08fdb9f44e2ff0` |
| TerrariaProgression.tmod | `0642318095c8e65356510022ba96252711702ba4c95b01a0d3fb06863e905e4a` |

以下保留0.15.0历史证据，不能替代本次修订成绩。

# 0.15.0 天赋方案验证记录

## 提交与授权

- 基线main：`6be301e6d44ef405bc81bdea50dd9ed48c03c5fb`，PR #17已按用户授权合并。
- 设计先行：`ed0f3f61c10f52bb860fda6d7546419bc622d04c`，D063与方案任务。
- 功能提交：`c0886c2b622acc96a070a4cd1521001ac7f34d06`。
- 本次通过检查的构建提交：`55976c060f54b01db5158e571decc4a7b8f10397`，随后仅追加Markdown验收证据；功能代码与交付包一致。
- [PR #18](https://github.com/zhu530319195-max/TerrariaProgression/pull/18)未合并，未获本批合并授权。版本0.15.0、100项天赋、存档v4、联机协议18。

## 自动证据

[CI34980331304](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34980331304)成功：4013项核心检查（较基线增加559项）、1788项原生检查（增加66项），正式及测试MOD编译均0 Warning / 0 Error。

核心检查覆盖两套各花100点、仅当前效果、复制历史价与独立开关／子效果／强度、当前退款、删除不加款、每次升级增加全页额度、重名稳定ID、名称与请求边界、服务器上限、v3及既有v1/v2迁移、v4全页账本与注册项校验、容量失败原子性、250次混合操作的逐页守恒。

原生检查覆盖真实SaveData/LoadData与旧v3外层Tag、原生生命魔力上限、反复切换不回复资源或重置药水／回蓝计时及已用跳数、Alt/K保留、实际服务器请求与快照、过期修订号／错误会话／重复复制、死亡限制、客户端不能创建权威页、共享击杀升级增加各页额度。实际背景墙作业启动后切换，立即停止，下一帧不继续拆墙；实际树干中段伐木产生待补种后切换，取消上下文且不扣背包材料。

早先两次CI分别暴露测试夹具问题：服务器共享XP用成单机OnHit入口，以及木锤首次敲击尚未拆掉首墙就检查作业队列。后续测试改用原生服务器Strike入口，并给夹具合法购买足够锤力。未为通过测试更改上述游戏规则，最终重新执行完整流程成功。

本地核心检查和双MOD编译成功；本地原生运行环境限制沿用此前记录，因此原生执行证据来自CI。界面布局、中文输入和实际手感待用户实机验收；没有宣称图形界面自动实测或真人联机已通过。

## 交付包

直接下载[CI产物10401375921](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34980331304/artifacts/10401375921)，核对官方产物摘要与ZIP完整性。玩家包包含正式`TerrariaProgression.tmod`、本批安装与测试说明、共享经验和已有功能操作说明、更新介绍及BUILD_INFO，不包含ProgressionHarness。

BUILD_INFO的`01318aca1dab016e02a986d8ee94580f4c7bdb6f`是GitHub临时PR测试合并提交，并非main合并。完整源码包152个Git文件逐一与构建提交55976c0比对一致。后续Markdown验证记录不冒充已编入测试包。

| 文件 | SHA256 |
|---|---|
| BoundlessPotential_0.15.0_Test.zip | `5d544e7e4277e5369b6eaa55725898f13113b4a813e98d160477351f22e0d117` |
| BoundlessPotential_0.15.0_Source.zip | `397cdcc5f004084d756c2053c7c7eb6551fc0915bae5b2b3fb30b974d01b94ce` |
| TerrariaProgression.tmod | `3a3d9c7d39472c50b7d655f0191aea26f99a9cad577b1419a0ebb0761664a70f` |

安装前备份角色；新存档不可直接降级给旧MOD，客户端和服务器需同版。用户按[中文步骤](TALENT_LOADOUTS_TEST_GUIDE_zh-CN.md)验收，重点看每套可独立花全部点数、切换后效果和退款范围、重进保存、中文管理界面。

## 保留边界

自动网络检查不是实际多人验收；真人联机仍暂缓。药水病缩短、原有钓鱼产量未专项实机验收；特殊／多阶段Boss、多人独立掉落增产和未知第三方兼容没有完成或通过承诺。分层暴击暂缓，所有已取消／移出事项保持。未上传Steam创意工坊。
