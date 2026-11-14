using UnityEngine;

namespace ZeroWalker.Player.States
{
    /// <summary>
    /// 跳跃/滞空状态。
    /// 目前没有专用的跳跃动画，按需求统一使用 Idle 动画代替。
    /// 负责起跳、空中的水平操控与落地切换；空中可以冲刺（与地面共用同一条冷却），但不响应射击输入。
    /// 空中冲刺结束时用 <see cref="ContinueAirborne"/> 回到这里，不会重新施加跳跃初速度。
    /// </summary>
    public class PlayerJumpState : PlayerState
    {
        private float _airTime;

        /// <summary>下一次进入是否属于「空中冲刺结束」，是则跳过跳跃初速度。</summary>
        private bool _resumeInAir;

        public PlayerJumpState(PlayerController player, StateMachine machine) : base(player, machine) { }

        /// <summary>
        /// 空中冲刺结束后回到滞空状态：不再给一次跳跃初速度，
        /// 并把滞空时间补到 minAirTime，落地判定不会因为重新进入而延迟。
        /// </summary>
        public void ContinueAirborne()
        {
            _resumeInAir = true;
            Machine.ChangeState(this);
        }

        public override void Enter()
        {
            // 用 Idle 动画代替跳跃动画
            Player.Animation.Play(PlayerAnimations.Idle, true);

            if (_resumeInAir)
            {
                _resumeInAir = false;
                _airTime = Player.Tuning.minAirTime;
                return;
            }

            Player.Jump();
            _airTime = 0f;
        }

        public override void Tick()
        {
            // 空中也允许冲刺：起跳后任何时刻按左 Shift 都能冲，冷却照常从冲刺开始算
            if (Player.Inputs.DashPressed && Player.CanDash)
            {
                Machine.ChangeState(Player.DashState);
                return;
            }

            // 空中允许转身；攻击/移动状态切换留到落地后
            Player.FaceTo(Player.Inputs.MoveAxis);
        }

        public override void FixedTick()
        {
            _airTime += Time.fixedDeltaTime;

            // 空中保留水平操控，垂直速度由起跳初速度和重力决定
            Player.MoveHorizontal(Player.Inputs.MoveAxis, 1f);

            if (_airTime < Player.Tuning.minAirTime) return;
            if (!Player.IsGrounded) return;
            if (Player.Body.velocity.y > 0.01f) return;

            var hasMoveInput = Player.HasMoveInput;
            Machine.ChangeState(hasMoveInput ? Player.MoveState : Player.IdleState);
        }
    }
}
