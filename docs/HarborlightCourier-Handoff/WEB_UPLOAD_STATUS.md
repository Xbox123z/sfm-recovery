# 网页上传状态（2026-10-27）

本次网页交接目标为 `Xbox123z/sfm-recovery` 的 `handoff/harborlight-courier-2026-10-27` 分支下 `docs/HarborlightCourier-Handoff/` 目录，范围包括源码、平台包交接材料、项目历史/交接 Markdown、验收材料和验证记录。禁止使用 Release、PR、`main`、Git LFS 或外托管。

当前远端仍为提交 `861a7ca2d96e39ac8ab0c5b705e7a958e9b3c0aa`（28 个文件）；`main` 仍为 `b240b61b438300ba4f3adcaae94b7140756949ea`。根目录 6 个文档尚未提交。原计划的 20MB Windows 4 片批量 Commit 明确失败；Android 5 片未尝试。

## 当前8MB逐片测试方案

交接目录当前保留 23 片：Windows 10 片、Android 13 片，每片不超过 8,000,000 bytes。Windows part001 单片加入新草稿后，提交前页面仍显示 `Uploading 1 of 1 files`；点击 Commit 一次后返回 GitHub upload 页面，Chrome 显示 `This page isn’t working / HTTP ERROR 400`。随后回读远端仍为 `861a7ca2d96e39ac8ab0c5b705e7a958e9b3c0aa`，`artifacts/chunks` 路径为空。HTTP 400 的根因未证实，不能据此断言 8MB 本身超限；按用户要求已停止，不重试、不继续 Windows 余片或 Android。23 片全未上传。最终只补 34 个基底文件（source 28 + 根目录 6），不把分片计入本次目标。

| 方案 | Windows | Android | 状态 |
|---|---:|---:|---|
| 旧20MB方案 | 4片 | 5片 | Windows 批量 Commit 失败；Android 未尝试，均未提交 |
| 新8MB方案 | 10片 | 13片 | part001 单次提交后 HTTP 400；远端无变更；其余22片未尝试，全部待上传 |

## 失败证据与包哈希

旧方案网页失败的精确页面信息为：`Commit failed`；`Committing the files to the repository failed. The file is too large and cannot be uploaded. Consider creating the file in a local clone and pushing it to GitHub.` 这次失败发生在 Windows 4 片批量提交，当前没有任何旧方案分片提交成功。新方案 part001 的 HTTP 400 也未产生远端变更，但其根因未证实。

| 原始本地包 | 大小 | SHA-256 |
|---|---:|---|
| `HarborlightCourier-windows-x64-complete.zip` | 72,898,920 | `d23bacb4fcc5a069e64a48d0783d09e78d4bf50e6ce6e90fe3d25325ffa5b067` |
| `HarborlightCourier-arm64.apk` | 96,655,966 | `33e4498e36058e22f38942b64aa6f44e59c582d09156ff7f701b8c17f0e8c735` |

两个原始包和 23 个新分片仍在本地；新分片的大小与 SHA-256 见 `SHA256SUMS.txt`，基底文件的路径、大小与 SHA-256 见 `UPLOAD_MANIFEST.md`。本地重组事实不替代远端逐片回读核验。
