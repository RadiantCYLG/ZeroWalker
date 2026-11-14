using UnityEngine;

namespace ZeroWalker.Player.States
{
    /// <summary>
    /// 冲刺状态：朝当前朝向前进固定距离。
    /// 没有专用的冲刺动画，使用 Move 动画表现快速前冲。
    /// 地面与空中都能触发；冲刺期间锁定方向与输入，由 PlayerController 负责 1 秒冷却。
    /// 空中冲刺结束时回到滞空状态（见 PlayerJumpState.ContinueAirborne），不额外给跳跃初速度。
    /// </summary>
    public class PlayerDashState : PlayerState
    {
        private float _elapsed;

        public PlayerDashState(PlayerController player, StateMachine machine) : base(player, machine) { }

        public override void Enter()
        {
            Player.Animation.Play(PlayerAnimations.Move, true);
            Player.BeginDash();
            _elapsed = 0f;
        }

        public override void Tick()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed < Player.DashDuration) return;

            // 空中冲刺结束：回滞空状态继续下落，而不是切到地面的待机/移动
            if (!Player.IsGrounded)
            {
                Player.JumpState.ContinueAirborne();
                return;
            }

            var hasMoveInput = Player.HasMoveInput;
            Machine.ChangeState(hasMoveInput ? Player.MoveState : Player.IdleState);
        }

        public override void FixedTick()
        {
            // 每物理帧保持冲刺速度，避免被上一状态的速度/阻力影响
            Player.ApplyDashVelocity();
        }

        public override void Exit()
        {
            Player.StopHorizontalMovement();
        }
    }
}
