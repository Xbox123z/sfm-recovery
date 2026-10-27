# 平台包分片重组说明

> **当前状态警告：GitHub 远端当前未含任何新8MB分片。本文仅是今后取得全部分片后的备用重组说明，不表示分片已上传或远端已验证。旧20MB Windows批量 Commit 已失败；新8MB方案的 Windows part001 单次提交返回 HTTP 400，远端无变更；其余22片未尝试。本文仅供未来取得全部23片后使用。**

从交接根目录 `docs/HarborlightCourier-Handoff/` 运行以下命令。分片是按字节连续、按编号顺序生成的上传片段，不是独立压缩包；必须取得同一平台的全部分片后再重组。不要对单片解压，也不要用文本编辑器或文本拼接方式处理二进制。

## 目标文件与完整哈希

| 平台 | 重组输出 | 大小 | SHA-256 |
|---|---|---:|---|
| Windows x64 | `source/artifacts/windows/HarborlightCourier-windows-x64-complete.zip` | 72,898,920 | `d23bacb4fcc5a069e64a48d0783d09e78d4bf50e6ce6e90fe3d25325ffa5b067` |
| Android ARM64 | `source/artifacts/android/HarborlightCourier-arm64.apk` | 96,655,966 | `33e4498e36058e22f38942b64aa6f44e59c582d09156ff7f701b8c17f0e8c735` |

## 当前8MB分片清单

Windows：part001–part009 各 8,000,000 bytes；part010 为 898,920 bytes。Android：part001–part012 各 8,000,000 bytes；part013 为 6,655,966 bytes。每片 SHA-256 见 `SHA256SUMS.txt`。

## Linux/macOS：显式顺序拼接

```sh
cd /path/to/docs/HarborlightCourier-Handoff
mkdir -p source/artifacts/windows source/artifacts/android
cat artifacts/chunks/windows/HarborlightCourier-windows-x64-complete.zip.part{001,002,003,004,005,006,007,008,009,010} > source/artifacts/windows/HarborlightCourier-windows-x64-complete.zip
cat artifacts/chunks/android/HarborlightCourier-arm64.apk.part{001,002,003,004,005,006,007,008,009,010,011,012,013} > source/artifacts/android/HarborlightCourier-arm64.apk
sha256sum source/artifacts/windows/HarborlightCourier-windows-x64-complete.zip source/artifacts/android/HarborlightCourier-arm64.apk
```

## Windows：Python流式二进制拼接

```python
from pathlib import Path
import shutil

def join(prefix, count, output):
    output = Path(output)
    with output.open('wb') as dst:
        for i in range(1, count + 1):
            name = Path(f'{prefix}.part{i:03d}')
            with name.open('rb') as src:
                shutil.copyfileobj(src, dst, length=1024*1024)

join('artifacts/chunks/windows/HarborlightCourier-windows-x64-complete.zip', 10, 'source/artifacts/windows/HarborlightCourier-windows-x64-complete.zip')
join('artifacts/chunks/android/HarborlightCourier-arm64.apk', 13, 'source/artifacts/android/HarborlightCourier-arm64.apk')
```

本地历史验证曾用旧9片方案拼接并得到 Windows 72,898,920 bytes、SHA-256 `d23bacb4fcc5a069e64a48d0783d09e78d4bf50e6ce6e90fe3d25325ffa5b067`，Android 96,655,966 bytes、SHA-256 `33e4498e36058e22f38942b64aa6f44e59c582d09156ff7f701b8c17f0e8c735`，均与本地原始包一致。新8MB的23片仍全在本地；part001 HTTP 400 未产生远端变更，其余未尝试。这些本地事实不表示远端已验证。
