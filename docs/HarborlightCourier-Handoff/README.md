# Harborlight Courier — AI 接手包

这是供后续 AI/开发者接管的本地完整快照及网页部分上传说明。请先读 [`PROJECT_HANDOFF.md`](PROJECT_HANDOFF.md)、[`WEB_UPLOAD_STATUS.md`](WEB_UPLOAD_STATUS.md) 和 [`REASSEMBLE_ARTIFACTS.md`](REASSEMBLE_ARTIFACTS.md)，再看 [`source/README.md`](source/README.md) 与 [`source/ACCEPTANCE_REPORT.md`](source/ACCEPTANCE_REPORT.md)。网页实际文件清单见 [`UPLOAD_MANIFEST.md`](UPLOAD_MANIFEST.md)，平台包及分片校验值见 [`SHA256SUMS.txt`](SHA256SUMS.txt)。

## 内容

- `source/`：Godot 4.7 Mono + .NET 8 项目源码、设计/来源/许可证说明、验收报告、测试及最终验证记录。
- 8MB 方案分片目前在 交接目录 `artifacts/chunks/` 本地保留：Windows 10 片、Android 13 片，每片不超过 8,000,000 bytes；尚未据此宣称任何远端成功。旧 20MB 方案的 Windows 4 片批量提交失败，Android 5 片未尝试。
- `source/artifacts/windows/HarborlightCourier-windows-x64-complete.zip` 与 `source/artifacts/android/HarborlightCourier-arm64.apk`：本地仍保留的原始完整包；分片方案已停止测试，按 `REASSEMBLE_ARTIFACTS.md` 重组后校验。
- `PROJECT_HANDOFF.md`：项目缘由、误做 Quiet Grid 后按用户要求从零重启的经过、玩法和架构、测试、未验收项、许可证边界及接手建议。

## 一眼要知道的事

这是独立原创的 Harborlight Courier，不是对参考项目的复刻。自动规则烟测 25/25 通过，.NET solution 构建 0 warnings/0 errors，Godot headless 场景启动成功；但 Windows 实机运行、Android 安装和触屏真机验收尚未完成。项目级许可证仍未选择，公开上传不等于授予任何开源再分发许可。

本地已验证旧方案 9 个分片按序拼接后分别与本地原始包完全一致；新 8MB 方案原拟为 Windows 10 片、Android 13 片；Windows part001 单片提交后页面返回 HTTP 400，远端无变更，因此按要求已停止，其余 22 片均未尝试。23 片全仍在本地，不能写成已上传、已交付或远端已验证。
