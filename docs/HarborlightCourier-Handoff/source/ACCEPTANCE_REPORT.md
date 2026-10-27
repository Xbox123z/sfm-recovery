# Harborlight Courier 本地构建与验收

## 玩法与来源边界

本项目是从零原创的轻量 2D 版本：据点准备、外出递送条件目标、管理 NPC 视线/噪声察觉、撤离结算；噪声触发调查，持续无遮挡视觉发现才触发失败；失败丢失当局未结算收益，长期钱包、升级和通关状态保留。两关为原创的 Pierline Green（简，3 件包裹/1 名巡逻者）与 Glasshouse Arcade（难，4 件/2 名巡逻者），采用白/浅灰方块 UI、键鼠及触屏控制。

只读参考源为 [`sfm-recovery`](https://github.com/KurosawaGeeker/sfm-recovery)，仅据其可证实的高层玩法流程建立简化规则；原仓库恢复代码、名称、角色、地图、对白、素材和关卡设计均未复制或改编。项目级源码/原创素材许可证尚未由所有者选择；未经许可不得据此推定可再分发，详见 `LICENSE_STATUS.md`。Godot、.NET、引擎字体及导出模板保留各自许可，见 `PROVENANCE.md` 与 `THIRD_PARTY_NOTICES.md`。在本地验收完成时、且在后来的明确公开上传授权之前，没有对原仓库或其他公开位置进行 push、PR、Release、上传或分发。

## 本次许可文档修正

已移除项目根目录的项目级 MIT `LICENSE`，以 `LICENSE_STATUS.md` 说明当前许可状态。第三方组件自身的许可与原有 NOTICE 未改动。未重建现有 EXE/APK；两者 SHA-256 与下表相同且未变化：Windows EXE `790cc5c89a0db105cfaf0e2dedf3137199b463bfd76a1fcd6c4c9f2586e3d6f9`，Android APK `33e4498e36058e22f38942b64aa6f44e59c582d09156ff7f701b8c17f0e8c735`。

## 平台产物

| 平台 | 产物 | 大小 | SHA-256 | 静态验收 |
|---|---|---:|---|---|
| Windows x64 | [`HarborlightCourier-windows-x64-complete.zip`](artifacts/windows/HarborlightCourier-windows-x64-complete.zip)（内含 EXE 与 runtime 目录） | ZIP 72,898,920 bytes；内含 EXE 109,292,408 bytes | ZIP `d23bacb4fcc5a069e64a48d0783d09e78d4bf50e6ce6e90fe3d25325ffa5b067`；EXE `790cc5c89a0db105cfaf0e2dedf3137199b463bfd76a1fcd6c4c9f2586e3d6f9` | PE32+ GUI，`pei-x86-64`；发行导出成功、ZIP 完整性校验通过 |
| Android ARM64 | [`HarborlightCourier-arm64.apk`](artifacts/android/HarborlightCourier-arm64.apk) | 96,655,966 bytes（92.18 MiB） | `33e4498e36058e22f38942b64aa6f44e59c582d09156ff7f701b8c17f0e8c735` | ZIP 完整；含 C# 程序集、主场景、`route_data.json` 和 arm64-v8a 原生库；APK v2 签名验证通过 |

Android 包元数据：包名 `org.harborlight.courier`，min SDK 24、compile/target SDK 36，ABI 为 `arm64-v8a`。签名使用 Godot 默认测试证书，不是供公开发行的项目专属发布密钥。

## 实测结果

C++ 路线编译器以 `g++` 编译通过，对两关路径可达性、碰撞/视线遮挡、包裹和巡逻参数共通过 21 项断言，并生成 C# 实际读取的 `route_data.json`。`.NET 8` solution 构建为 0 warnings、0 errors；Godot `--build-solutions` 退出码 0。最终 C# 玩法烟测 25/25 通过，覆盖双关、递送、结算、升级、失败损失、重试、存档恢复、墙体碰撞及视线遮挡。Godot headless 主场景运行 60 帧退出码 0。

APK 已检查 ZIP 完整性、`aapt` 包元数据、arm64-v8a 原生 ABI、包内游戏数据，并通过 `apksigner` v2 验签。Windows 文件确认是 x86-64 PE，但本机没有 Wine/Wine64，未能在 Windows 运行该 EXE。ADB 可用但设备列表为空，且没有 Android emulator 或 system image，因此没有在真机/模拟器安装运行 APK，也未完成触屏实机验收。以上目标平台运行项仍未验收；headless 玩法/场景测试和包级静态检查不能替代它们。

主要日志位于 `verification/`：`final-artifact-verification.log`、`cpp-route-compiler.log`、`final-dotnet-solution-build.log`、`final-godot-build-solutions.log`、`final-gameplay-smoke-v3.log`、`final-main-scene-startup-v3.log`、`windows-final-clean-export.log`、`android-gradle-final-attempt.log`。Godot Android build-template 安装规则的只读源码依据及 URL 见 `verification/upstream-godot-template-notes.md`。

## 本机 Android 工具链说明

本机只有 .NET SDK 8.0.427/runtime 8.0.33，没有 .NET 9；NDK 仅有 28.1.13356709，没有模板默认要求的 29.0.14206865。没有把项目目标框架改为 net9.0，也没有下载或安装新工具链：使用本机已安装的 Godot Gradle 模板、Gradle/AGP 缓存、Android API 36，并仅在 `/dev/shm/harborlight-stage-v3` 的模板副本中把 NDK 版本改为现有 28.1 后成功导出 APK。模板 compile/target SDK 仍为 36、min SDK 24。

为适配本沙盒的模板目录，`export_presets.cfg` 的 Android preset 使用本机绝对路径 `/home/ubuntu/.local/share/godot/export_templates/4.7.stable.mono/android_source.zip`。Android build template 与 NDK 覆盖只在临时 staging 中安装/修改，未写入全局设置或打包进项目源码；在其他机器重新导出 Android 时，需要按本机 Godot 模板路径安装项目 build template，并将模板 NDK 配置调整为该机器已安装版本，或使用匹配的 NDK 29。

## 公开交接补记（2026-10-27）

本报告中“没有公开上传”的描述是本地验收完成时的历史状态。之后用户明确授权本项目的源码、Windows 完整 ZIP、Android APK、交接 Markdown、验收材料与最终验证记录上传到 [`Xbox123z/sfm-recovery`](https://github.com/Xbox123z/sfm-recovery) 的独立分支 `handoff/harborlight-courier-2026-10-27`，目录为 `docs/HarborlightCourier-Handoff/`。旧20MB方案的 Windows 4 片网页 Commit 已明确失败，Android 5 片未尝试。8MB 方案共 23 片（Windows 10、Android 13）仅作为备用重组目标。Windows part001 单次提交后返回 HTTP 400，远端无变更；其余22片未尝试，已停止。不能据 HTTP 400 断言 8MB 本身超限，根因未证实。两个裸 ZIP/APK 仍在本地；完整包哈希与新片哈希见 `SHA256SUMS.txt`，本地重组事实不替代远端验证。
