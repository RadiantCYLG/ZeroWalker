using System;
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

            var direction = Facing >= 0 ? Vector3.right : Vector3.left;
            var bullet = Instantiate(bulletPrefab, muzzle.WorldPosition, Quaternion.identity);
            bullet.Launch(direction, special);
            ShotCount++;

            // 枪口火光跟随子弹颜色，让特殊射击在画面上更统一。
            // 这里只广播事实，具体怎么闪由订阅者决定（MuzzlePoint 负责火光）。
            Fired?.Invoke(Facing, special ? bullet.ShotColor : Color.white);
        }

        private void OnDrawGizmosSelected()
        {
            Bounds bounds;
            if (arena != null && arena.TryGetBounds(out bounds))
            {
                Gizmos.color = new Color(1f, 0.92f, 0.3f, 0.6f);
                Gizmos.DrawWireCube(bounds.center, new Vector3(bounds.size.x, bounds.size.y, 0f));
            }
        }
    }
}
