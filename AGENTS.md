# ZeroWalker — 项目说明书

> 任何 AI 或人都先读这份文件，再动代码。改动完必须让下面的「验证命令」全部通过。
> 这里只放**规则和入口**；玩法细节见 `Documents/`，决策理由见 `Documents/拍板记录.md`。

## 一句话游戏

一款 2D 横版冒险闯关游戏：玩家操控折纸风格的枪手 Blitz，左右跑动、跳跃、冲刺，用连射与近战清掉关卡里的目标。

- 引擎/语言：Unity 2022.3.62f3c1 / C#（Spine 骨骼动画、LitJson、Input System Package）
- 目标平台：Windows PC + Android
- 设计起点：[`Documents/concept.md`](Documents/concept.md)

## 目录地图

仓库根就是 Unity 工程根。Unity 强制 `Assets/`、`Packages/`、`ProjectSettings/` 必须在工程根，所以技术内容直接落在 `Assets/`。

| 路径 | 是什么 |
| --- | --- |
| `AGENTS.md` / `CLAUDE.md` | 本说明书。CLAUDE.md 只有一行 `@AGENTS.md`，勿改 |
| `Documents/` | 设计真相：概念、玩法专题、拍板记录、截图。写「是什么、为什么」，不写实现细节 |
| `Documents/AI开发游戏规范.md` | 团队开发规范原文（要求），会随进展更新。冲突时以它为准，差异记进拍板记录 |
| `Documents/concept.md` | 设计起点：已确认 / 建议待试玩 / 待决定 三栏 |
| `Documents/拍板记录.md` | 决策定稿处：带日期的三栏记录，本文件只留入口 |
| `Documents/玩家控制.md` | 玩法专题：操作、状态机、时序、参数表 |
| `Documents/Screenshots/` | 运行截图，文档引用 |
| `Assets/Scripts/Core/` | 纯 C# 逻辑层：数值 DTO（`Model/`）+ 规则（`Rules/`）。**不引用 UnityEngine** |
| `Assets/Scripts/Framework/` | 自控底座：内容加载、存档、LitJson。可引用引擎 |
| `Assets/Scripts/Game/` | 对象/控制层：MonoBehaviour、状态机、表现。`Player/`、`Combat/`、`Camera/`、`VFX/` |
| `Assets/Scripts/*/ZeroWalker.*.asmdef` | 程序集边界。跨层引用必须显式声明 |
| `Assets/Resources/Content/data/*.json` | **所有可调数值**。改手感只改这里 |
| `Assets/Resources/Content/i18n/ui.csv` | **所有玩家可见文案**。中英两列 |
| `Assets/Spine/` | Spine 运行时（第三方，含 `spine-unity` 程序集）。不改 |
| `Assets/ArtRes/` | 美术源文件（角色、环境、特效），由 Unity 导入，不手工导出 |
| `Assets/Prefabs/` | 预制体（子弹、命中特效） |
| `Assets/Scenes/SampleScene.unity` | 当前唯一场景 |
| `tools/` | 校验脚本（见「验证命令」）。不参与打包 |
| `Packages/`、`ProjectSettings/` | Unity 工程配置。改包/输入设置只能在这里 |

## 硬约定

分层与数据流：

```
Content/data/*.json
      │  Framework/Content/ContentLoader（Resources.Load + LitJson）
      ▼
Core/Model/*Tuning ──────► Core/Model/GameContent（一局游戏的全部数值）
      │                          │
      │  Core/Rules/*（DashCooldown、Health 等纯逻辑）
      ▼                          ▼
Game/*（MonoBehaviour：读数值 → 跑规则 → 驱动动画/刚体/特效）
```

- **Core**：不 `using UnityEngine`，可脱离引擎单测。数据类叫 `*Tuning`，规则类放 `Core/Rules`。
- **Framework**：引擎相关的公共底座。`JsonManager` 管存档，`ContentLoader` 管数值表。
- **Game**：只做「把数值接到引擎对象上」。组件里不写死数值，启动时从 `ContentLoader.Current` 取。

1. **逻辑不碰画面**：`Core/` 下出现 `UnityEngine` 即违规（脚本会查）。动画名、颜色、缩放属于表现，留在 `Game/`。
2. **数值只进 JSON**：新增可调数值，写进 `Content/data/*.json` 并在对应 `*Tuning` 加字段；**不要在 MonoBehaviour 上新增 `[SerializeField] float/int`**。组件上只允许挂资源引用（预制体、Sprite、Transform）与 `bool` 开关。纯数学保护量（除零下限、归一化阈值、物理 epsilon）留在代码里并写注释，不算可调数值。
3. **文案只进表格**：玩家能看到的字进 `Content/i18n/ui.csv`，中英两列。代码里不写字面量文案。
4. **新增 .cs 不用手工登记**：asmdef 自动收录当前目录及子目录。但新建目录要确认它归哪个程序集；跨层引用要改 asmdef 的 `references`。
5. **一个文件一件事**：类只承担一个职责，混了两件事就拆（例：`PlayerInputReader` 轮询、`InputBindings` 解析绑定）。
6. **编辑器/调试代码不进发行版**：调试覆盖入口用 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 包住，发行版里是空操作。
7. **第三方目录不动**：`Assets/Spine/` 是导入的运行时，改了就再也无法升级。
8. **场景文件只放「引用与摆放」**：数值不放场景里，否则同一份数据有了两个来源。

## 验证命令

改完任何代码或数值，按顺序跑。全部 `PASS` 才算过。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify-content.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify-conventions.ps1
```

- `verify-content.ps1`：**跑什么**——解析 `Content/data/*.json`，检查字段齐全、无多余字段、数值在合理区间，并核对 `ui.csv` 表头。**什么算过**——每条都是 `PASS`，退出码 0。
- `verify-conventions.ps1`：**跑什么**——扫描 `Assets/Scripts`，检查 Core 层没有引用引擎、Game 层没有把数值写回组件字段、全部 `.cs` 是合法 UTF-8。**什么算过**——没有 `FAIL` 行，退出码 0。

> 新增 `.ps1` 必须存成 **UTF-8 with BOM**：Windows PowerShell 5.1 读无 BOM 的 UTF-8 会按 GBK 解码，脚本里的中文直接语法报错。
> `tools/serialized-number-allowlist.txt` 是 `verify-conventions.ps1` 的例外名单（每行 `路径|理由`），只放「不是手感数值」的项（目前只有调试输入覆盖）。
> 校验脚本本身也要反着验：故意把某个数值改到区间外、或往组件里塞一个 `[SerializeField] float`，脚本必须报 `FAIL`。

Unity 侧（脚本跑不了的）：

- **什么算过**——Unity 编辑器重新编译后 Console 没有红色 error；进入 Play，A/D 移动、空格跳、左 Shift 冲刺（1 秒冷却）、F 近战、按住左键连射，靶子 3 发打掉并在 2 秒后复活。
- **怎么反着验一次**——把 `player.json` 的 `moveSpeed` 改成 50，Play 里角色明显变快，说明数值真的从 JSON 走；改回。
- **最近一次通过（2026-11-13）**——用户实测：`moveSpeed` 改 50 后角色明显变快，确证 `Resources.Load` 在运行时可用（不是只在编辑器里对）；已改回 5，`verify-content.ps1` 仍 PASS。

## 拍板记录

日期：2026-11-13（本次架构重构）。**三栏明细在 [`Documents/拍板记录.md`](Documents/拍板记录.md)**，本文件不再重复。

- 「已确认」是已经进代码的事实，带日期；被推翻的用删除线划掉并写作废日期，不直接删。
- 「建议（待试玩确认）」是猜测，不能当已实现的功能写在文档里。
- 「待决定」是明确没做或还没想好的事。

## Git 节奏

- 每完成一个阶段提交一次，message 写清「做了什么 + 为什么」，用中文。
- 提交前跑完「验证命令」。
- 当前仓库没有 remote（2026-11-13 用户说明：地址后续再给）。**每次需要 push 时先提醒用户提供地址，不要静默跳过、也不要自己猜一个地址。**

## 已知限制

- 跳跃动画用的是 `Idle`，冲刺用的是 `Move`（Spine 资源里没有对应动作）。
- 近战只有动画、没有判定（`Skill_02` 播完就回 Idle）。
- 玩家没有受击/死亡状态。
- 色彩空间是 Gamma（2D 项目默认），改 Linear 需要重做贴图色彩校验。
- 移动端未做触屏输入（见拍板记录「待决定」）。
