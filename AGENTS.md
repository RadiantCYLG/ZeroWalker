# ZeroWalker — 项目说明书

> 任何 AI 或人都先读这份文件，再动代码。改动完必须让下面的「验证命令」全部通过。

## 一句话游戏

一款 2D 横版冒险闯关游戏：玩家操控折纸风格的枪手 Blitz，左右跑动、跳跃、冲刺，用连射与近战清掉关卡里的目标。

- 引擎/语言：Unity 2022.3.62f3c1 / C#（Spine 骨骼动画、LitJson、Input System Package）
- 目标平台：Windows PC + Android
- 设计起点：[`Documents/concept.md`](Documents/concept.md)

## 目录地图

仓库根就是 Unity 工程根。Unity 强制 `Assets/`、`Packages/`、`ProjectSettings/` 必须在工程根，所以不可能再套一层 `Game/`——`Assets/` 就是「Game」的落点。

| 路径 | 是什么 |
| --- | --- |
| `AGENTS.md` / `CLAUDE.md` | 本说明书。CLAUDE.md 只有一行 `@AGENTS.md`，勿改 |
| `Documents/` | 设计真相：概念、玩法专题、截图。代码怎么实现不算真相，这里写「是什么、为什么」 |
| `Assets/Scripts/Core/` | 纯 C# 逻辑层：数值 DTO（`Model/`）+ 规则（`Rules/`）。**不引用 UnityEngine** |
| `Assets/Scripts/Framework/` | 自控底座：内容加载、存档、LitJson。可引用引擎 |
| `Assets/Scripts/Game/` | 对象/控制层：MonoBehaviour、状态机、表现。`Player/`、`Combat/`、`Camera/`、`VFX/` |
| `Assets/Scripts/*/ZeroWalker.*.asmdef` | 程序集边界。跨层引用必须显式声明 |
| `Assets/Resources/Content/data/*.json` | **所有可调数值**。改手感只改这里 |
| `Assets/Resources/Content/i18n/ui.csv` | **所有玩家可见文案**。中英两列 |
| `Assets/Spine/` | Spine 运行时（第三方，含 `spine-unity` 程序集）。不改 |
| `Assets/ArtRes/` | 美术源文件（角色、环境、特效） |
| `Assets/Prefabs/` | 预制体（子弹、命中特效） |
| `Assets/Scenes/SampleScene.unity` | 当前唯一场景 |
| `tools/` | 校验脚本（见「验证命令」）。不参与打包 |
| `Packages/`、`ProjectSettings/` | Unity 工程配置。改包/输入设置只能在这里 |

## 分层与数据流

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
- **Framework**：引擎相关的公共底座。`JsonManager` 管存档（读 StreamingAssets → 回退 persistentDataPath），`ContentLoader` 管数值表。
- **Game**：只做「把数值接到引擎对象上」。组件里不写死数值，启动时从 `ContentLoader.Current` 取。

## 硬约定

1. **逻辑不碰画面**：`Core/` 下出现 `UnityEngine` 即违规（脚本会查）。动画名、颜色、缩放属于表现，留在 `Game/`。
2. **数值只进 JSON**：新增任何可调数值，写进 `Content/data/*.json` 并在对应 `*Tuning` 加字段；**不要在 MonoBehaviour 上新增 `[SerializeField] float/int`**。组件上只允许挂资源引用（预制体、Sprite、Transform）与 `bool` 开关。
3. **文案只进表格**：玩家能看到的字进 `Content/i18n/ui.csv`，中英两列。代码里不写字面量文案。
4. **新增 .cs 不用手工登记**：asmdef 自动收录当前目录及子目录。但新建目录要确认它归哪个程序集；跨层引用要改 asmdef 的 `references`。
5. **编辑器/调试代码不进发行版**：调试覆盖入口用 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 包住，发行版里是空操作。
6. **第三方目录不动**：`Assets/Spine/` 是导入的运行时，改了就再也无法升级。
7. **场景文件只放「引用与摆放」**：数值不放场景里，否则同一份数据有了两个来源。

## 验证命令

改完任何代码或数值，按顺序跑。全部 `PASS` 才算过。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify-content.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify-conventions.ps1
```

- `verify-content.ps1`：**跑什么**——解析 `Content/data/*.json`，检查字段齐全、类型正确、数值在合理区间。**什么算过**——每条都是 `PASS`，退出码 0。
- `verify-conventions.ps1`：**跑什么**——扫描 `Assets/Scripts`，检查 Core 层没有引用引擎、Game 层没有把数值写回组件字段。**什么算过**——没有 `FAIL` 行，退出码 0。

> 新增 `.ps1` 必须存成 **UTF-8 with BOM**：Windows PowerShell 5.1 读无 BOM 的 UTF-8 会按 GBK 解码，脚本里的中文直接语法报错。
> `tools/serialized-number-allowlist.txt` 是 `verify-conventions.ps1` 的例外名单（每行 `路径|理由`），只放「不是手感数值」的项（目前只有调试输入覆盖）。

Unity 侧（脚本跑不了的）：

- **什么算过**——Unity 编辑器重新编译后 Console 没有红色 error；进入 Play，A/D 移动、空格跳、左 Shift 冲刺（1 秒冷却）、F 近战、按住左键连射，靶子 3 发打掉并在 2 秒后复活。
- **怎么反着验一次**——把 `player.json` 的 `moveSpeed` 改成 50，Play 里角色明显变快，说明数值真的从 JSON 走；改回。

## 拍板记录

日期：2026-11-13（本次架构重构）

### 已确认

| 决策 | 理由 |
| --- | --- |
| 仓库根 = Unity 工程根，`Assets/` 即规范里 `Game/` 的落点 | Unity 强制 `Assets/` 只能在工程根，套一层 `Game/` 会让编辑器无法打开工程。用目录地图补上这层映射 |
| 拆 `ZeroWalker.Core` / `ZeroWalker.Framework` / `ZeroWalker.Game` 三个 asmdef | 用编译期边界代替口头约定：Core 设 `noEngineReferences`，一旦有人 `using UnityEngine` 直接编译失败 |
| 数值放 `Assets/Resources/Content/data/*.json`，用 `Resources.Load<TextAsset>` 同步读 | StreamingAssets 在 Android 上必须走 UnityWebRequest 异步读，配置表用它会把启动流程复杂化；Resources 在 PC/Android 都是同步可用 |
| 输入统一走 Input System Package（`Keyboard.current` / `Mouse.current` 轮询） | 用户要求；且键位改成表驱动，换键不用改代码。`Active Input Handling` 保持 `Both`，避免影响编辑器工具链 |
| 键位也进 JSON（`input.json`） | 键位同样是「可调数值」，跟手感参数放一起，改表即换键 |
| 纯表现参数（枪口火光 0.06/0.9/1.35、命中闪光 0.15/0.7/1.5）也进 JSON，用配置档区分 | 两处都用 `SpriteFlash`，用「配置档」而不是新建组件，避免改 prefab YAML |
| `JsonManager` 从全局命名空间挪进 `ZeroWalker.Framework.Persistence` 并转 UTF-8 | 原文件是 GBK，混着中文注释在 UTF-8 工程里必然乱码 |
| 场景/预制体里的数值不再回填 | 数值唯一来源是 JSON；场景里残留的字段会被 Unity 忽略 |

### 建议（待试玩确认）

- 冲刺：距离 4 米、时长 0.2 秒、冷却 1 秒 —— 目前偏「瞬间位移」，手感是试玩重点。
- 相机：偏移 (0, 1.15, -10)、平滑 0.15、不锁 Y —— 跑跳时会有轻微上下浮动，是否要锁 Y 待定。
- 特殊弹：青色 `(0.35, 0.95, 1)`、1.2 倍大小 —— 与普通弹区分度是否足够待定。
- 靶子：3 血、受击闪色 0.08 秒、2 秒复活 —— 只用于验证打击反馈，不是正式敌人。

### 待决定

- **Android 触屏操作**：当前只有键鼠绑定。上手机需要虚拟摇杆/开火按钮，属于新功能，本次不做，等明确要做时单开一阶段。
- **存档内容**：`JsonManager` 底座已就位（LitJson 已导入），但还没有要存的玩法数据（进度、解锁、设置），等玩法定下来再定存档字段。
- **关卡/敌人/受击死亡**：原型只有靶子与可跑的地形，没有正式敌人和玩家死亡流程。

## Git 节奏

- 每完成一个阶段提交一次，message 写清「做了什么 + 为什么」。
- 提交前跑完「验证命令」。
- 当前仓库没有 remote，`push` 等用户提供地址后再接。

## 已知限制

- 跳跃动画用的是 `Idle`，冲刺用的是 `Move`（Spine 资源里没有对应动作）。
- 近战只有动画、没有判定（`Skill_02` 播完就回 Idle）。
- 玩家没有受击/死亡状态。
- 色彩空间是 Gamma（2D 项目默认），改 Linear 需要重做贴图色彩校验。
- 移动端未做触屏输入（见「待决定」）。
