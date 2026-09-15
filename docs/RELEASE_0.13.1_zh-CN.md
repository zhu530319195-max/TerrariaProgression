# 无尽潜能 · Boundless Potential 0.13.1

## 安装或更新

1. 退出游戏，备份角色和世界。解压正式发布包，找到`TerrariaProgression.tmod`。
2. tModLoader的模组管理页面点“打开模组文件夹”，把上述文件放进去，覆盖旧版同名文件。常见路径为`文档/My Games/Terraria/tModLoader/Mods`。
3. 重新启动游戏，启用“无尽潜能 · Boundless Potential”，确认版本0.13.1。
4. 进入原角色，P打开面板。中文标题为“无尽潜能”，英文标题为“Boundless Potential”；原等级、投入、开关和所选强度应保留。服务器设置标题也改为新名。
5. 默认操作继续为P面板、左Alt批量开关、G范围／矿脉、K额外挖矿保护默认关闭。客户端和服务器请使用同版模组。

版本0.13.1只更新名称与介绍、发布材料；游戏玩法与用户已验收0.13.0一致。共100项天赋，角色存档v3，协议17。内部名称和文件名继续是TerrariaProgression，看到这个名字不代表装错版本。

100项覆盖战斗、属性、资源、采集与农业。功能详细操作仍见源码中的`docs/P3ResourcesLimits_TEST_GUIDE_zh-CN.md`；那份说明的0.13.0旧显示名属于验收历史。

## 准备发布到Steam创意工坊

材料已经包含：正式tmod、完整源码、中英文创意工坊介绍`description_workshop.txt`、游戏内介绍`description.txt`、更新说明`changelog.txt`。当前尚未上传Steam，未创建公开工坊条目。

1. 使用你的Steam账号启动tModLoader。进入“创意工坊 → 开发模组（Workshop → Develop Mods）”，打开源码目录。
2. 从完整源码包中复制`TerrariaProgression`这一层文件夹到源码目录。最终应为`ModSources/TerrariaProgression/build.txt`，不能多套一层同名目录；不要复制`tests/ProgressionHarness`。
3. 正式tmod应已按上文安装。返回开发模组页面，刷新列表，找到内部名`TerrariaProgression`。开发源码列表显示内部名，模组管理列表显示“无尽潜能 · Boundless Potential”。若无“发布（Publish）”按钮，先按该页面提示设置开发环境并“构建并重载（Build + Reload）”。在不同tModLoader版本本地重建后，须重新核对目标版本和运行效果，不能直接沿用原CI包校验值。
4. 点击这项的“发布”，核对名称与0.13.1版本。介绍使用源码中的`description_workshop.txt`，更新说明来自`changelog.txt`。保留已安装的源码目录，后续更新继续维护同一个条目。
5. 在发布页面设置标签、可见性和预览图。自定义封面尚未制作，可以在此选择自己的预览图片。确认无误后再点击最后的“发布”。
6. 发布成功后，把创意工坊链接记录到仓库发布文档。日后更新沿用同一内部标识与工坊条目，不另建一个同名MOD。

步骤依据[tModLoader官方Workshop说明](https://github.com/tModLoader/tModLoader/wiki/Workshop)及固定版本开发菜单。当前任务完成改名、合并与发布准备，最终Steam上传仍需在你的客户端完成。

## 兼容与验证范围

目标tModLoader v2026.07.3.0／Terraria 1.4.4.9／.NET8。精确构建提交、自动检查数量和tmod校验值见包内BUILD_INFO。

0.13.0本批功能由用户确认实机成功。0.13.1更名后仍可按上面第3、4步快速确认显示。真实多人暂未实测；特殊／多阶段Boss适配和多人独立掉落增产未完成；药水病缩短、原有钓鱼产量仍未专项实机验收；不保证未知第三方MOD兼容。分层暴击暂缓，已取消项目不恢复。
