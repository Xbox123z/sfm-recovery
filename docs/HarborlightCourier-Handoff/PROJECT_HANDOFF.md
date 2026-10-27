# Harborlight Courier：项目历史与接手指南

## 项目现状

Harborlight Courier 是一款 Godot 4.7 Mono / .NET 8 制作的原创轻量 2D 信使游戏。玩家从 Harborlight Dispatch 据点出发，在街区递送包裹、避开巡逻者，再回到 HQ 门结算。本地完整快照含 Windows x64 ZIP、Android ARM64 APK、源码、验收报告与主要最终验证日志；两个原始包仍在本地。当前改测每片不超过 8,000,000 bytes 的 Windows 10 片、Android 13 片方案，完整包可按 `REASSEMBLE_ARTIFACTS.md` 重组，详见 `WEB_UPLOAD_STATUS.md`。

**本地原工程没有 `.git` 目录或提交历史。** 因此本分支提交是把现有项目整理进仓库的一次快照，而非原项目连续的开发提交史。以下项目经过根据现有任务上下文、源码、README、来源说明和验收记录整理；原始聊天中的最初请求全文没有作为文件保存，所以这里不是逐字聊天记录，不会伪造原文。

## 来龙去脉

1. 项目早期曾误做成 **Quiet Grid** 原型，方向不符合用户想要的结果。用户明确纠正并要求从头重做；后续工作按“从零开始”处理。Quiet Grid 原型的代码、素材、名字和地图没有被带入本包。
2. 重启后建立 **Harborlight Courier**：只参考 `KurosawaGeeker/sfm-recovery` 仓库中能被证实的高层玩法因果——据点准备、外出条件任务、NPC 察觉/调查、撤离结算和持久化状态。它不是原游戏的恢复版，也不声称复原原作数值、任务或地图。
3. 所有游戏代码、原创地图/角色/包裹/UI、关卡名及图标均重新创作。地图和 UI 由 C# 几何绘制，没有从原仓库或 Quiet Grid 复制代码、游戏素材、对白或关卡设计。来源边界详见 `source/PROVENANCE.md`。
4. 游戏实现完成后，用 C++ 路线编译器生成并校验两关数据；用 .NET / Godot 构建，执行自动玩法烟测、headless 场景启动以及 Windows/Android 包级校验。严格的通过项与缺项见 `source/ACCEPTANCE_REPORT.md` 和 `source/verification/`。
5. 原本的本地验收说明要求不要公开上传。项目级 MIT `LICENSE` 后来被移除，改为保留 `LICENSE_STATUS.md`，因为所有者尚未选择项目原创代码/素材的许可证。
6. **之后用户明确授权这次公开交接**，批准范围是将源码、Windows 完整 ZIP、Android APK、项目历史/交接 Markdown、验收材料和相关验证记录放进 `Xbox123z/sfm-recovery` 的新目录和独立分支。旧 20MB 方案的 Windows 4 片批量 Commit 明确失败，Android 5 片未尝试。现将不超过 8,000,000 bytes 的 23 片方案（Windows 10、Android 13）保留为备用重组目标。Windows part001 单片提交后返回 HTTP 400，远端无变更；按要求已停止，其余 22 片未尝试，不能宣称任何分片成功。当前网页交接仅针对 `handoff/harborlight-courier-2026-10-27` 分支的 `docs/HarborlightCourier-Handoff/` 目录；没有改 `main`，没有创建 PR 或 Release。

## 游戏玩法与规则

- **Pierline Green / 简单**：3 件包裹、1 名巡逻者、路更宽、察觉较慢。
- **Glasshouse Arcade / 困难**：4 件包裹、2 名巡逻者、建筑遮挡较多、察觉较快。
- 靠近包裹后按互动键递送；收集后可返回绿色 HQ 门结算。可以部分返家；完整递送另有奖励。
- 奔跑会制造噪声，促使巡逻者转向声音来源调查。察觉值（Notice）达到 100 本身不会直接失败；持续处于巡逻者无遮挡视线中才会报告本作的 Caught 失败。
- 失败会清空当局未结算收入；钱包、Quiet Soles 升级和通关记录会跨关保存。Quiet Soles 售价 35，可降低奔跑噪声。简单关完整报酬为 36（3×8+12），困难关为 68（4×12+20）。
- Windows：鼠标选关，WASD/方向键移动，Shift 奔跑，E/空格互动，Esc 暂停。Android：横屏、左侧触控摇杆，右侧 RUN/ACT。

## 代码结构

| 文件 | 职责 |
|---|---|
| `source/CourierGame.cs` | Godot 主玩法：绘制场景/UI、键鼠和触控输入、玩家碰撞、巡逻/视线/噪声、保存 IO。 |
| `source/CourierSession.cs` | 与引擎分离的状态规则：阶段、递送、结算、失败损失、钱包、升级、通关状态及 JSON 存档。察觉值和失败事件刻意分开。 |
| `source/RouteCatalog.cs` | 读取并校验生成的 JSON 关卡数据。 |
| `source/native/route_compiler.cpp` | 生成 `route_data.json` 并对路线可达、碰撞/视线遮挡和关卡参数执行断言。它不是游戏主实现；运行时玩法由 Godot/C# 实作。 |
| `source/route_data.json` | 两关地图、包裹、建筑、巡逻路径和感知参数。 |
| `source/Main.tscn`, `source/project.godot` | Godot 项目入口和配置。 |
| `source/tests/SMOKE_TESTS.md` | 自动烟测覆盖范围。 |
| `source/PROVENANCE.md`, `source/LICENSE_STATUS.md`, `source/THIRD_PARTY_NOTICES.md` | 来源、原创边界、当前许可证状态及第三方依赖说明。 |

## 构建和验证事实

在原构建环境中使用 Godot 4.7 stable Mono、.NET SDK 8.0.427 / runtime 8.0.33；没有额外 NuGet 游戏依赖。最终记录显示：

- C++ 路线编译器通过 21 项断言；
- .NET solution 构建 0 warnings、0 errors；Godot `--build-solutions` 退出码 0；
- C# 自动玩法烟测 25/25 通过，涵盖两关配置、递送/结算、升级、满察觉不直接失败、Caught 失败、收入损失、重试、存档恢复、墙体碰撞与视线遮挡；
- Godot headless 主场景启动 60 帧，退出码 0；
- Windows 导出文件静态识别为 PE32+ x86-64；Android 包结构、ARM64 ABI、SDK 元数据和 APK v2 签名静态检查通过。

可从 `source/` 项目目录尝试：

```sh
godot --headless --path . -- --smoke-test
dotnet build HarborlightCourier.sln
```

以上命令只说明项目中的验收入口，不代表在新机器上已重跑成功；新环境应先安装匹配的 Godot 4.7 Mono 与 .NET 8。

## 本地完整包、分片与校验值

| 文件 | 大小 | SHA-256 | 说明 |
|---|---:|---|---|
| `source/artifacts/windows/HarborlightCourier-windows-x64-complete.zip`（由 4 片重组） | 72,898,920 bytes | `d23bacb4fcc5a069e64a48d0783d09e78d4bf50e6ce6e90fe3d25325ffa5b067` | 完整可解压 Windows 文件夹；ZIP CRC/内容清单通过。内含 EXE 与运行时目录。 |
| `source/artifacts/android/HarborlightCourier-arm64.apk`（由 5 片重组） | 96,655,966 bytes | `33e4498e36058e22f38942b64aa6f44e59c582d09156ff7f701b8c17f0e8c735` | 包名 `org.harborlight.courier`，min SDK 24，target/compile SDK 36，仅 `arm64-v8a`。 |
| Windows ZIP 内的 `HarborlightCourier.exe` | 109,292,408 bytes | `790cc5c89a0db105cfaf0e2dedf3137199b463bfd76a1fcd6c4c9f2586e3d6f9` | PE32+ GUI x86-64。原始 EXE 单文件超过 GitHub 限制，因此随完整 ZIP 交付，不单独上传。 |

Android APK 使用 Godot 默认测试证书签名，不是项目专属生产发布密钥；在本地完整快照中，APK 单独位于 `source/artifacts/android/`，并不包含在 Windows ZIP 中。原始两个包仍在本地；23 个 8MB 分片也仍在本地。远端当前无分片，未来若另获全部分片才可按备用说明重组并核验。

## 仍未完成 / 接手时先注意

1. **Windows 真机运行未验收。** 原环境没有 Windows 或 Wine/Wine64；目前只确认 EXE 的 PE 架构、导出及包文件，不应声称已在 Windows 启动。
2. **Android 真机/模拟器安装和触控未验收。** 原环境 ADB 设备列表为空，也没有 Android emulator/system image；APK 只完成静态包、ABI、SDK 和签名检查。请实际安装并测试横屏、摇杆、RUN/ACT、菜单触点。
3. **许可证未选。** `source/LICENSE_STATUS.md` 必须保留；这次公开上传只授权了指定交接范围，不自动授予任何人源码或原创素材的开源再分发权。不要擅自加 MIT/其他许可证、改授权或发布到其他地方；需要时先让所有者决定。
4. `source/export_presets.cfg` 中 Android 模板路径为原沙盒绝对路径 `/home/ubuntu/.local/share/godot/export_templates/4.7.stable.mono/android_source.zip`，在接手机器通常不可用。原 APK 使用本机 Godot Gradle 模板副本，将模板 NDK 从默认要求的 29.0.14206865 改到本机已有的 28.1.13356709；只改了 `/dev/shm` 临时 staging，不在本包源码中。接手者需要用该机器安装的 Godot build template 和匹配 NDK 调整导出配置，再重新导出并记录环境。
5. 关键最终验证日志已保存在 `source/verification/`；这里选择的是验收报告所引用的最终记录，不包含缓存目录、临时 build tree、早期试错日志或重复/旧产物。

## 上传清单和排除项

`UPLOAD_MANIFEST.md` 说明最终仅补交 28 个 source 文件和 6 个根目录文档，共 34 个文件；23 个 8MB 分片列为本地待上传记录，不纳入本次远端目标。两个原包仅本地保留。没有上传凭据、token、用户私有数据、缓存或临时 build tree。原远端交接分支仍为 `861a7ca2d96e39ac8ab0c5b705e7a958e9b3c0aa`（28 文件），`main` 仍为 `b240b61b438300ba4f3adcaae94b7140756949ea`；根目录 6 文件尚未提交。

没有单独上传 109 MB 的原始 Windows EXE（已包含在完整 ZIP 中）、解压后的 Windows runtime 目录（ZIP 内已有）、旧版 `HarborlightCourier-windows-x64.zip`（缺少后来补入的 README、许可证状态和第三方声明文件），也没有重复上传项目中其他过时产物。平台包使用公共分支普通路径分片，不使用 Git LFS、PR、Release 或默认分支提交。
