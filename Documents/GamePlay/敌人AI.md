# 敌人 AI（状态机实现）

> 说明「现在有两种敌人、各自会做什么、怎么实现的、为什么这么设计」。
> 数值全部在 `Assets/Resources/Content/data/enemies.json`；改手感只改表，不改代码。

## 1. 现在有什么

| 敌人 | 外观 | 行为 | 血量 |
| --- | --- | --- | --- |
| 近战型 `MeleeEnemy` | 暖红胶囊占位 | 在出生点左右 `patrolRange` 内来回巡逻；玩家进入 `detectRadius` 后加速追击；进到 `attackRange` 站住前摇，然后扑出去；扑中一次造成一次命中 | 3 |
| 远程型 `RangedEnemy` | 紫色胶囊占位 | 原地不动；玩家进入 `detectRadius` 后转身瞄准，前摇结束朝玩家打一发子弹，然后冷却，重复 | 2 |

两者都能被玩家子弹打死，死后先闪一下再消失。玩家目前**没有血量**：敌人打中玩家只播命中特效（见「已知限制」）。

生成方式：Unity 菜单 **`ZeroWalker/生成敌人（胶囊占位）`**（`Assets/Editor/EnemySetup.cs`）。它会生成两张胶囊 PNG、两个预制体（`Assets/Prefabs/Enemies/`），在场景里摆一只近战（右侧）和一只远程（左侧），并给玩家打上 `Player` 标签（敌人子弹靠这个标签命中玩家）。

## 2. 分两层：大脑只管想，组件只管做

```
EnemySenses（每帧采集：距离、方位）
        │
        ▼
Core/Ai/*Brain（纯 C# 有限状态机）
        │
        ▼
EnemyIntent（MoveX、Facing、Attack、Shoot）
        │
        ▼
Game/Enemies/*（落地：写刚体速度、翻朝向、生成子弹、播特效）
```

- **大脑**（`Assets/Scripts/Core/Ai/MeleeEnemyBrain.cs`、`RangedEnemyBrain.cs`）：不 `using UnityEngine`，不知道刚体、动画、预制体存在，只回答「这一帧想干什么」。
- **落地**（`Assets/Scripts/Game/Enemies/`）：每物理帧采一份 `EnemySenses` 喂给大脑，拿到 `EnemyIntent` 后落实成引擎动作。
- 这样拆的好处：行为可以脱离 Unity 推演（给一串 senses 就能测状态迁移）；换表现（换成 Spine 动画、换贴图）不用动状态机；调参只动 JSON。

## 3. 感知与意图的契约

`Assets/Scripts/Core/Ai/EnemySenses.cs`（Game 层填，大脑只读）：

| 字段 | 含义 |
| --- | --- |
| `HasPlayer` | 场景里有没有玩家（`EnemyAgent.Start` 里 `FindObjectOfType<PlayerController>()`，找不到就是 false） |
| `SelfX` / `SelfY` | 自己的位置 |
| `PlayerX` / `PlayerY` | 玩家位置 |
| `PlayerDistance` | 两者距离（`Vector2.Distance`） |
| `DeltaX` | `PlayerX - SelfX`，判断朝左还是朝右 |

`Assets/Scripts/Core/Ai/EnemyIntent.cs`（大脑吐，Game 层执行）：

| 字段 | 含义 |
| --- | --- |
| `MoveX` | 这一帧想要的水平速度（正右负左，0 就是站住） |
| `Facing` | 想朝哪边（1 右 / -1 左），`EnemyAgent` 用它翻 `visual.localScale.x` |
| `Attack` | 这一帧发生一次近战命中（近战扑击用） |
| `Shoot` | 这一帧开一枪（远程用） |

`Attack` / `Shoot` 都是**一次性脉冲**：只在满足条件的那一帧为真，执行方不要缓存它。

## 4. 近战敌人：巡逻 → 追击 → 前摇 → 扑击 → 硬直

```
        ┌──────── 玩家离开 loseRadius ────────┐
        ▼                                     │
   ┌─────────┐  看见玩家   ┌─────────┐  进入攻击距离  ┌────────┐
   │ Patrol  │ ─────────► │  Chase  │ ────────────► │ Windup │
   └─────────┘            └─────────┘               └────────┘
        ▲                      ▲                         │ 计时到
        │  硬直结束且看不见玩家  │  硬直结束且还看得见        ▼
   ┌────────┐                  │                    ┌────────┐
   │Recover │ ─────────────────┘◄───────────────────│ Lunge  │
   └────────┘       （只回 Chase）                    └────────┘
        ▲                                                   │ 计时到
        └───────────────────────────────────────────────────┘
```

| 状态 | 做什么 | 关键参数 |
| --- | --- | --- |
| `Patrol` | 朝当前方向以 `patrolSpeed` 走；走到 `出生点 ± patrolRange` 先掉头再走（避免在边界反复抖） | `patrolSpeed`、`patrolRange` |
| `Chase` | 朝玩家方向以 `chaseSpeed` 追；`PlayerDistance <= attackRange` 进前摇；超过 `loseRadius` 放弃回巡逻 | `chaseSpeed`、`attackRange`、`loseRadius` |
| `Windup` | 站住不动，`windupTime` 秒后扑出去（给玩家反应时间） | `windupTime` |
| `Lunge` | 以 `lungeSpeed` 朝前摇时的朝向冲 `lungeTime` 秒；途中第一次进到 `hitRadius` 时置一次 `Attack` | `lungeSpeed`、`lungeTime`、`hitRadius` |
| `Recover` | 站住 `cooldown` 秒（硬直），结束后还看得见就回 `Chase`，否则回 `Patrol` | `cooldown` |

设计取舍：

- **发现半径与追丢半径分开**（`detectRadius` 5 < `loseRadius` 8）。只用半径会在这个圈边上来回切换状态（追一步、丢一步），加迟滞后就稳定了。
- **追击速度比玩家慢**（3.4 < 玩家 5），靠「前摇 + 扑击」补最后一段距离，避免敌人粘着玩家跑。
- **扑击只命中一次**（`_hitThisLunge`），否则一次扑击会连续扣血。
- 扑击结束一律先进 `Recover`，不会「扑空立刻再扑」。

## 5. 远程敌人：扫描 → 瞄准 → 开火 → 冷却

```
   ┌────────┐  看见玩家   ┌──────┐  计时到   ┌──────┐  立即   ┌──────────┐
   │  Scan  │ ─────────► │ Aim  │ ────────► │ Fire │ ──────► │ Cooldown │
   └────────┘            └──────┘           └──────┘         └──────────┘
        ▲                    │                                   │
        │                    │ 看不见玩家                          │ 冷却结束且看不见
        └────────────────────┴───────────────────────────────────┘
                        （冷却结束且还看得见 → 回 Aim）
```

| 状态 | 做什么 | 关键参数 |
| --- | --- | --- |
| `Scan` | 站住待机；玩家进 `detectRadius` 才转身进瞄准 | `detectRadius` |
| `Aim` | 站住，持续朝玩家转身；`aimTime` 秒后开火；中途看不见玩家就回扫描 | `aimTime` |
| `Fire` | 只持续一帧：置 `Shoot`，转冷却 | — |
| `Cooldown` | 站住等 `fireCooldown`；结束后还看得见就回瞄准，否则回扫描 | `fireCooldown` |

- 瞄准期间**一直跟着玩家转**，所以玩家横向跑动不会让子弹打偏；纵向（W/S）跑动会让它打偏，因为子弹是朝玩家当时的位置直线飞的。
- 子弹速度 `bulletSpeed` 12，比玩家的 22 慢，留出反应时间。

## 6. 数值表（`enemies.json`）

改这个文件就是改手感；改完退出 Play 再进（`ContentLoader.Current` 有静态缓存）。

| 字段 | 默认 | 说明 |
| --- | --- | --- |
| `hitColor` | `(1, 0.55, 0.55)` | 受击时精灵被染成的颜色（要够亮才看得出闪） |
| `hitFlashDuration` | 0.1 | 受击闪色持续秒数 |
| `melee.maxHealth` | 3 | 挨几发玩家子弹死 |
| `melee.patrolSpeed` | 1.2 | 巡逻速度 |
| `melee.chaseSpeed` | 3.4 | 追击速度 |
| `melee.patrolRange` | 3.0 | 巡逻半径（以出生点为中点） |
| `melee.detectRadius` | 5.0 | 发现玩家的距离 |
| `melee.loseRadius` | 8.0 | 追丢的距离（必须 ≥ 发现距离） |
| `melee.attackRange` | 1.3 | 进这个距离就起手 |
| `melee.hitRadius` | 1.6 | 扑击途中进这个距离算命中（必须 ≥ 攻击距离） |
| `melee.windupTime` | 0.25 | 前摇秒数 |
| `melee.lungeSpeed` | 7.0 | 扑击速度 |
| `melee.lungeTime` | 0.18 | 扑击持续秒数 |
| `melee.cooldown` | 0.8 | 扑完的硬直秒数 |
| `ranged.maxHealth` | 2 | 挨几发死 |
| `ranged.detectRadius` | 8.0 | 开火视野 |
| `ranged.aimTime` | 0.35 | 瞄准前摇 |
| `ranged.fireCooldown` | 1.2 | 两发之间 |
| `ranged.bulletSpeed` | 12.0 | 敌方子弹速度 |
| `deathDelay` | 0.15 | 死掉到消失的延迟（播闪色的时间） |

## 7. 谁打谁：标签 + 接口

- **阵营**：`BulletSide.Player` 的子弹只对 `Enemy` 标签生效；`BulletSide.Enemy` 的只对 `Player` 标签生效。两边共用同一个子弹预制体（`Assets/Prefabs/Bullet.prefab`），阵营只改「打哪个标签」和目标速度。
- **伤害结算**：子弹命中后先播命中特效，再 `GetComponentInParent<IDamageable>()?.TakeDamage(damage)`。`ShootableTarget`（靶子）和 `EnemyHealth`（敌人）都实现这个接口，所以子弹不需要知道打的是谁。
- **标签名**：`GameTags.Enemy` / `GameTags.Player`，不散在代码里。
- **死亡**：`EnemyHealth` 扣到 0 → 关掉所有子碰撞体（不再挡子弹/被打）→ 广播 `Died`；`EnemyAgent` 收到后停住并关掉自己（不再跑 AI）；`EnemyView` 收到后延迟 `deathDelay` 销毁对象。三者职责分开，互不知道对方做什么。
- **受击闪色为什么不用 `SpriteFlash`**：那个组件是「一次性特效」，`Awake` 里就调用 `Hide()` 把渲染器关掉（枪口火光、命中火花本来就该从不可见开始）。挂到敌人身上，敌人一进 Play 就整块看不见。敌人的闪色是「闪一下再恢复」，所以 `EnemyView` 自己记一个计时器，直接改 `SpriteRenderer.color`，到点换回原色。

## 8. 为什么这么设计

| 决定 | 理由 |
| --- | --- |
| 用有限状态机，不用行为树 | 每种敌人只有四五个状态、迁移条件都是「距离/计时」，行为树要多写一层组合与黑板，收益为零 |
| 状态机放 `Core`，不碰引擎 | 与玩家状态机同一条约定：逻辑层可离线推演、可单测；也符合规范里「逻辑层不碰画面表现层代码」 |
| 用 `EnemySenses`/`EnemyIntent` 传值，而不是让大脑直接拿 `Transform` | 一旦大脑能碰 `Transform`，测试就得先搭场景；传值让「喂一串数据、断言输出」成为可能 |
| 攻击/开火用「一次性脉冲」 | 状态机不知道执行方是每帧跑还是每物理帧跑，用脉冲能保证一次攻击只结算一次 |
| 敌人组件继承 `EnemyAgent` | 刚体设置、朝向翻转、感知采集、死亡停机这些是两种敌人共用的，抽出来只留「挑哪个大脑」的差异 |
| 占位敌人用菜单生成 | 贴图、导入设置、预制体连线、场景摆放一次成型且可复现；手摆漏一个引用就要靠猜 |

## 9. 怎么加第三种敌人

1. 在 `Assets/Scripts/Core/Ai/` 写一个 `XxxEnemyBrain`：构造拿 `XxxEnemyTuning`，`Tick(float dt, EnemySenses senses)` 返回 `EnemyIntent`。只算「想干什么」。
2. 在 `Core/Model/EnemyTuning.cs` 加 `XxxEnemyTuning` 字段与 `Validate` 检查，`enemies.json` 加同名段，`tools/verify-content.ps1` 的 schema 与区间检查同步加。
3. 在 `Assets/Scripts/Game/Enemies/` 写 `XxxEnemy : EnemyAgent`：`Start` 里 `new XxxEnemyBrain(...)`，`FixedUpdate` 里 `TryGetSenses` → `Tick` → `ApplyIntent`，需要的话落实 `Attack`/`Shoot`。预制体由编辑器菜单或手摆，记得挂 `EnemyHealth`（`kind` 要加枚举值）与 `EnemyView`。

## 10. 已知限制

- **直线追**：没有寻路、不会绕障、不会跳平台；活动范围只有纵向带子，敌人和玩家一样被夹在里面（敌人没有单独的区域约束）。
- **一套参数**：每种敌人只有一份数值，不能在同一关放「弱一点/强一点」的同种敌人。
- **玩家不会死**：玩家没有血量，敌人命中只播特效；近战扑击没有击退、没有受击硬直。
- **敌人只有两种死法**：玩家子弹（`BulletSide.Player`，打 `Enemy` 标签）和玩家近战（`PlayerController.MeleeStrike` 的身前盒子）。两者都走 `IDamageable`，扣的是 `EnemyHealth` 同一份血。
- **没有动画**：占位是胶囊 Sprite，只有受击闪色与死亡消失；正式 Spine 动画、攻击动作、朝向翻转的表现细节都还没接。
- **远程不会走位**：站着不动，玩家绕到背后也只是转身继续打。
