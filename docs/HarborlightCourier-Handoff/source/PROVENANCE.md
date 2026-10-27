# 来源与许可边界

## 参考范围

唯一玩法参考是原仓库 `https://github.com/KurosawaGeeker/sfm-recovery` 的只读本地副本。只吸收可证实的高层循环：据点准备、外出条件任务、NPC 视线/噪声察觉、撤离结算或被发现后返家、当局收益与长期存档分离。只读证据包括 `docs/REVERSE_ENGINEERING.zh-CN.md` 与恢复快照内的 `PlayerSearcher`、`NpcStrangenessManager`、`MissionBase`、`StageTransController`、`GameOverController`、`GameStateChecker`、`SaveManager` 等文件。核心区别是：察觉累积、噪声导致 NPC 转身调查、直接发现、玩家生命/精神门槛触发失败属于不同阶段；具体场景阈值和完整任务配置未能从现有快照证实。

本项目在玩法层只实现“噪声提高 NPC 警觉并使其调查；持续无遮挡视觉发现触发本作的捕获失败”这一抽象，使用全新数值，不声称和原作内部阈值/规则相同。源快照缺少完整场景、Prefab 与任务数据，因此本作不是原作地图或内容复刻。

## 全新实现

本项目没有从参考仓库或 Quiet Grid 复制/改编任何代码、第三方游戏组件、图像、音频、字体文件、对白、名称、角色、模型、地图、存档结构或关卡设计。Pierline Green、Glasshouse Arcade、Harborlight Dispatch、巡逻者、包裹、数值、界面与两关布局均为新创。地图/人物/建筑/包裹/视线锥和 UI 方块由 `CourierGame.cs` 的绘图代码实时生成；不随包带入原作素材。
应用图标 `harborlight_icon.svg` 亦为本项目从零绘制的原创图形；随包视觉内容均非参考仓库提取。

## 项目级许可证状态

项目级源码/原创素材许可证尚未由所有者选择；未经许可不得据此推定可再分发。详见 `LICENSE_STATUS.md`。本次公开上传是所有者针对 `Xbox123z/sfm-recovery` 指定新目录和分支的明确交接授权，不构成项目许可证的选择或对其他再分发方式的授权。

## 依赖

运行/构建使用 Godot Engine 4.7 Mono、Godot .NET SDK 4.7.0、.NET 8 和本机 Android workload；没有额外游戏插件或 NuGet 依赖。Godot 与随引擎提供的运行模板依其 MIT 许可，.NET 组件依其各自许可；这些第三方许可不决定本项目原创内容的许可状态。界面使用 `ThemeDB.FallbackFont` 中随 Godot 引擎提供的字体，遵循 Godot 发行版第三方声明；本项目没有打包外部字体文件。构建用 JDK/Android SDK 是本机工具，不作为本项目原创内容。

本项目在本地构建与验收完成时未 push、PR、Release、公开上传或分发。2026-10-27 用户明确授权后，网页端仅把 33 个可接受的交接文件上传到 `Xbox123z/sfm-recovery` 的 `handoff/harborlight-courier-2026-10-27` 分支；Windows ZIP 与 Android APK 因网页单文件限制尚未上传，本地包仍保留且未外托管或拆包。没有更改默认分支、创建 PR 或 Release。详见同目录 `PROJECT_HANDOFF.md` 与 `WEB_UPLOAD_STATUS.md`；最终远端状态须另行验证。
