# Harborlight Courier

原创轻量 2D 信使游戏：从 Dispatch 据点出发，在两张原创街区地图中递送包裹、管理 NPC 视线/噪声察觉，并回到 HQ 门结算。噪声会促使巡逻者转向调查；只有玩家持续暴露于其无遮挡视线中才会触发本作的“被拦下”失败。失败会返家并清空当局未结算报酬；钱包、通关记录和 Quiet Soles 升级跨关保留。

## 两关

- **Pierline Green / Easy**：3 件包裹、1 名巡逻者、宽通路、察觉较慢。
- **Glasshouse Arcade / Hard**：4 件包裹、2 名巡逻者、较密建筑、察觉较快。

包裹靠近后按互动递送；收集到至少一件后可从绿色 HQ 门返家，按已递送件数领取部分报酬；全部完成另有奖励。Notice 达到 100 时巡逻者会处于高警戒/调查态，但这本身不直接判失败。可以用已存货币买一次性长期升级 Quiet Soles，降低奔跑噪声。

## 操作

- **Windows**：鼠标选择关卡；WASD/方向键移动、Shift 奔跑、E/空格互动、Esc 暂停。
- **Android**：横屏；左侧触控摇杆移动，右侧 RUN 奔跑、ACT 互动，菜单可点按。
- 建筑会阻挡视线；奔跑会在巡逻者附近产生噪声。察觉会随时间衰减，NPC 可能面向上次听到声音的位置调查。

## 技术与本地运行

Godot 4.7 Mono + .NET 8。`CourierSession.cs` 负责状态/规则/存档，`CourierGame.cs` 负责场景绘制、输入、碰撞、NPC 感知与触屏映射。所有场景图形由代码绘制；不使用原作资源、外部游戏素材包或额外 NuGet 包。

自动规则烟测：
本次平台产物、校验日志以及 Windows/Android 真机运行的未验收项见 `ACCEPTANCE_REPORT.md`。

```sh
godot --headless --path . -- --smoke-test
```

来源、依赖与许可证状态见 `PROVENANCE.md`、`THIRD_PARTY_NOTICES.md` 和 `LICENSE_STATUS.md`。验证范围与未验收项见 `DESIGN_AND_ACCEPTANCE.md` 和 `verification/`。
