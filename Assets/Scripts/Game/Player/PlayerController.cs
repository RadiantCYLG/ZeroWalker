using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroWalker.Combat;
using ZeroWalker.Core.Model;
using ZeroWalker.Core.Rules;
using ZeroWalker.Framework.Content;
using ZeroWalker.Player.States;
using ZeroWalker.World;

namespace ZeroWalker.Player
{
    /// <summary>
    /// 玩家角色控制器（Blitz）。
    /// 负责：读取输入 -> 驱动有限状态机 -> 播放 Spine 动画 -> 八向移动/转向 -> 开枪。
    /// 具体状态行为在 States 目录下，每个状态自治地决定何时切换。
    /// 角色在桥面这条带子上移动：A/D 左右，W/S 纵深；没有重力和跳跃，
    /// 每物理帧结束时把位置夹进活动范围（ArenaBounds）。
    /// 可调数值不在这里写死，统一来自 Content/data/player.json。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("射击")]
        [Tooltip("子弹预制体（按住左键时，Attack_Loop 每次 OnAttack 事件出一发）")]
        [SerializeField] private Bullet bulletPrefab;

        [Header("引用（留空会自动查找）")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpineAnimationPlayer animationPlayer;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private MuzzlePoint muzzle;

        [Header("场地（留空会自动查找场景里的 ArenaBounds）")]
        [SerializeField] private ArenaBounds arena;

        // ---- 状态机 ----
        private readonly StateMachine _machine = new StateMachine();

        /// <summary>冲刺冷却（纯逻辑计时器，见 Core/Rules/DashCooldown）。</summary>
        private readonly DashCooldown _dashCooldown = new DashCooldown();

        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerAttackState AttackState { get; private set; }
        public PlayerDashState DashState { get; private set; }
        public PlayerMeleeState MeleeState { get; private set; }

        private PlayerTuning _tuning;

        /// <summary>瞄准用的主相机（懒取，见 TryGetAimDirection）。</summary>
        private Camera _camera;

        // ---- 对外只读数据 ----
        public PlayerInputReader Inputs => inputReader;
        public SpineAnimationPlayer Animation => animationPlayer;
        public Rigidbody2D Body => body;
        public Transform VisualRoot => visualRoot;
        public MuzzlePoint Muzzle => muzzle;
        public StateMachine Machine => _machine;
        public ArenaBounds Arena => arena;

        /// <summary>玩家可调数值（来自 Content/data/player.json）。</summary>
        public PlayerTuning Tuning
        {
            get
            {
                if (_tuning == null)
                {
                    _tuning = ContentLoader.Current.player;
                }

                return _tuning;
            }
        }

        /// <summary>朝向：1 右，-1 左。</summary>
        public int Facing { get; private set; } = 1;

        /// <summary>本次运行累计射出的子弹数（便于调试、UI 或音效统计）。</summary>
        public int ShotCount { get; private set; }

        /// <summary>
        /// 开火后广播：朝向 + 这一发的颜色。
        /// 枪口火光、音效、震动等表现订阅它；控制器本身不直接改任何表现状态，
        /// 这样加音效或换特效都不用动开火逻辑（见 AGENTS.md 硬约定「事件解耦」）。
        /// </summary>
        public event Action<int, Color> Fired;

        /// <summary>移动输入是否超过死区（含纵深，死区值来自 player.json 的 moveDeadzone）。</summary>
        public bool HasMoveInput => Inputs.Axis2D.sqrMagnitude > Tuning.moveDeadzone * Tuning.moveDeadzone;

        /// <summary>冲刺冷却是否已结束。</summary>
        public bool CanDash => _dashCooldown.IsReady;

        /// <summary>单次冲刺的持续时间（秒）。</summary>
        public float DashDuration => Tuning.dashDuration;

        /// <summary>冲刺时的水平速度（米/秒），由距离/时长换算。</summary>
        public float DashSpeed => Tuning.DashSpeed;

        /// <summary>剩余冲刺冷却时间（秒）。</summary>
        public float DashCooldownRemaining => _dashCooldown.Remaining;

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (animationPlayer == null) animationPlayer = GetComponentInChildren<SpineAnimationPlayer>();
            if (muzzle == null) muzzle = GetComponent<MuzzlePoint>();
            if (arena == null) arena = FindObjectOfType<ArenaBounds>();

            if (visualRoot == null && animationPlayer != null)
                visualRoot = animationPlayer.transform;

            _dashCooldown.Configure(Tuning.dashCooldown);

            IdleState = new PlayerIdleState(this, _machine);
            MoveState = new PlayerMoveState(this, _machine);
            AttackState = new PlayerAttackState(this, _machine);
            DashState = new PlayerDashState(this, _machine);
            MeleeState = new PlayerMeleeState(this, _machine);
        }

        private void Start()
        {
            _machine.ChangeState(IdleState, true);
        }

        private void Update()
        {
            // 冲刺冷却独立于状态机计时，保证任何状态下都按真实时间恢复
            _dashCooldown.Tick(Time.deltaTime);

            inputReader.Poll();
            _machine.Tick();
        }

        private void FixedUpdate()
        {
            _machine.FixedTick();

            // 状态机算完速度后统一夹一次：不写进各个状态，保证任何时候都不会走出活动范围
            ClampToArena();
        }

        /// <summary>沿八向输入移动。speedScale 用于不同状态下的速度倍率。</summary>
        public void Move(Vector2 axis, float speedScale)
        {
            body.velocity = new Vector2(
                axis.x * Tuning.moveSpeed * speedScale,
                axis.y * Tuning.moveSpeedY * speedScale);
        }

        /// <summary>立刻停下（射击、近战时角色保持原地不动）。</summary>
        public void StopMovement()
        {
            body.velocity = Vector2.zero;
        }

        /// <summary>开始冲刺：启动冷却，并按当前朝向施加冲刺速度。</summary>
        public void BeginDash()
        {
            _dashCooldown.Start();
            ApplyDashVelocity();
        }

        /// <summary>每物理帧保持冲刺水平速度（冲刺只走水平方向，纵深速度清零）。</summary>
        public void ApplyDashVelocity()
        {
            body.velocity = new Vector2(Facing * DashSpeed, 0f);
        }

        /// <summary>
        /// 把角色限制在活动范围内。
        ///
        /// 不在事后把位置拉回来——那样每物理帧都会先冲出边界一点、再被瞬移回边界，
        /// 刚体开着插值，渲染位置就在这两点之间来回摆，贴边时看着就是抖。
        ///
        /// 这里改成事先削速度：这一步允许的位移最多是「到边界的剩余距离」，
        /// 位置因此从不会被写回。真出现在外侧（冲刺、传送、场景初始摆放）才纠正一次。
        /// </summary>
        private void ClampToArena()
        {
            if (arena == null) return;

            Bounds area;
            if (!arena.TryGetBounds(out area)) return;

            var position = body.position;
            var velocity = body.velocity;
            var dt = Time.fixedDeltaTime;

            if (dt > 0f)
            {
                if (velocity.x > 0f) velocity.x = Mathf.Min(velocity.x, (area.max.x - position.x) / dt);
                else if (velocity.x < 0f) velocity.x = Mathf.Max(velocity.x, (area.min.x - position.x) / dt);

                if (velocity.y > 0f) velocity.y = Mathf.Min(velocity.y, (area.max.y - position.y) / dt);
                else if (velocity.y < 0f) velocity.y = Mathf.Max(velocity.y, (area.min.y - position.y) / dt);
            }

            body.velocity = velocity;

            // 保底：万一已经在范围外，只纠正这一次，之后靠上面的削速度保持贴边静止
            var clamped = new Vector2(
                Mathf.Clamp(position.x, area.min.x, area.max.x),
                Mathf.Clamp(position.y, area.min.y, area.max.y));

            if ((clamped - position).sqrMagnitude > 0.000001f)
            {
                body.position = clamped;
            }
        }

        /// <summary>根据输入水平分量转身（翻转折纸的视觉根节点，不影响碰撞体）。</summary>
        public void FaceTo(float axis)
        {
            if (Mathf.Abs(axis) < Tuning.moveDeadzone) return;

            var direction = axis > 0f ? 1 : -1;
            if (direction == Facing) return;

            Facing = direction;

            if (visualRoot == null) return;
            var scale = visualRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            visualRoot.localScale = scale;
        }

        /// <summary>
        /// 挥一次近战：在角色正面判定一个盒子，命中标签为 Enemy 的物体各结算一次伤害。
        ///
        /// 只在 PlayerMeleeState 的挥击瞬间调用一次，所以一次按键最多打一下；
        /// 盒子尺寸由 meleeRange 推导（高度取 0.9 倍），不额外开参数——
        /// 近战是「身前一条」，不是脚下一圈，所以从脚下往上抬 0.35 倍量。
        ///
        /// 伤害走 IDamageable，和子弹命中共用同一套结算：靶子和敌人都会被近战打掉。
        /// </summary>
        public void MeleeStrike()
        {
            var tuning = Tuning;
            var reach = tuning.meleeRange;
            var center = body.position + new Vector2(Facing * reach * 0.5f, reach * 0.35f);
            var size = new Vector2(reach, reach * 0.9f);

            var hits = Physics2D.OverlapBoxAll(center, size, 0f);
            if (hits == null || hits.Length == 0) return;

            // 同一个目标身上可能挂多个碰撞体，先按「可受伤对象」去重，避免一刀打两下
            var damaged = new HashSet<IDamageable>();
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null || hit.tag != GameTags.Enemy) continue;

                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damaged.Add(damageable)) continue;

                damageable.TakeDamage(tuning.meleeDamage);
            }
        }

        /// <summary>立刻在枪口生成一发普通子弹。</summary>
        public void Fire()
        {
            Fire(false);
        }

        /// <summary>
        /// 立刻在枪口生成一发特殊子弹（颜色/特效区别于普通子弹）。
        /// 由 Attack_Loop 的 OnAttack 动画事件调用；按住扳机时每次事件都会调用一次。
        /// </summary>
        public void FireSpecial()
        {
            Fire(true);
        }

        private void Fire(bool special)
        {
            if (bulletPrefab == null || muzzle == null) return;

            // 默认朝面对的方向平射；按住瞄准键时改为朝准星指的那个世界点。
            var direction = Facing >= 0 ? Vector3.right : Vector3.left;
            if (Inputs.AimHeld && TryGetAimDirection(out var aim))
            {
                direction = new Vector3(aim.x, aim.y, 0f);
                FaceTo(aim.x >= 0f ? 1f : -1f);
            }

            var bullet = Instantiate(bulletPrefab, muzzle.WorldPosition, Quaternion.identity);
            bullet.Launch(direction, BulletSide.Player, special);
            ShotCount++;

            // 枪口火光跟随子弹颜色，让特殊射击在画面上更统一。
            // 这里只广播事实，具体怎么闪由订阅者决定（MuzzlePoint 负责火光）。
            Fired?.Invoke(Facing, special ? bullet.ShotColor : Color.white);
        }

        /// <summary>
        /// 把「准星所在的屏幕点」换算成枪口射出的方向。
        ///
        /// 只在瞄准时用：屏幕点先按「相机到枪口所在平面」的距离反投影成世界点，
        /// 再取「枪口 → 该点」的方向。方向会被收进玩家面向一侧的扇形里：
        /// 仰角/俯角绝对值不超过 player.json 的 aimMaxAngle，所以准星移到头顶也不会朝天开枪；
        /// 超出时取边界角度（水平方向朝准星那一侧，抬高或压低到上限）。
        /// 没有相机/枪口或两者重合时返回 false，调用方退回平射。
        /// </summary>
        private bool TryGetAimDirection(out Vector2 direction)
        {
            direction = Vector2.zero;

            var camera = _camera != null ? _camera : (_camera = Camera.main);
            if (camera == null || muzzle == null || inputReader == null) return false;

            var from = muzzle.WorldPosition;
            var depth = Mathf.Abs(camera.transform.position.z - from.z);
            var screen = new Vector3(Inputs.AimScreenPosition.x, Inputs.AimScreenPosition.y, depth);
            var world = camera.ScreenToWorldPoint(screen);

            var delta = new Vector2(world.x - from.x, world.y - from.y);
            if (delta.sqrMagnitude < 0.000001f) return false;

            // 准星在枪口的哪一侧，就朝哪一侧打（同时也决定开枪后的朝向）。
            // 准星几乎正在头顶时横向分量趋近 0，这时沿用当前朝向，避免朝向在一帧内反复跳。
            var side = delta.x >= 0f ? 1f : -1f;
            if (Mathf.Abs(delta.x) < 0.0001f)
            {
                side = Facing >= 0 ? 1f : -1f;
            }

            // 仰角 = atan2(竖直分量, 横向分量绝对值)，范围 (-90°, 90°)；
            // 夹到 ±aimMaxAngle 就是「能打到的扇形」，超出的部分贴着边界打。
            var maxAngle = Mathf.Clamp(Tuning.aimMaxAngle, 1f, 89f);
            var elevation = Mathf.Atan2(delta.y, Mathf.Abs(delta.x)) * Mathf.Rad2Deg;
            elevation = Mathf.Clamp(elevation, -maxAngle, maxAngle);

            var radians = elevation * Mathf.Deg2Rad;
            direction = new Vector2(Mathf.Cos(radians) * side, Mathf.Sin(radians));
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Bounds bounds;
            if (arena != null && arena.TryGetBounds(out bounds))
            {
                Gizmos.color = new Color(1f, 0.92f, 0.3f, 0.6f);
                Gizmos.DrawWireCube(bounds.center, new Vector3(bounds.size.x, bounds.size.y, 0f));
            }

            // 近战判定盒（运行时按当前朝向画，便于对位调 meleeRange）
            if (body == null) return;

            var reach = Tuning.meleeRange;
            var center = body.position + new Vector2(Facing * reach * 0.5f, reach * 0.35f);
            Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.6f);
            Gizmos.DrawWireCube(center, new Vector3(reach, reach * 0.9f, 0f));
        }
    }
}
