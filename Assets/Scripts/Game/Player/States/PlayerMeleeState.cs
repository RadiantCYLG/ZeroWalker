using UnityEngine;

namespace ZeroWalker.Player.States
{
    /// <summary>
    /// 近战攻击状态。
    /// 播放 Spine 的 Skill_02 动画（单次），动画结束后回到 Idle / Move。
    /// 近战期间角色原地不动、朝向锁定，不响应移动、冲刺和射击输入。
    ///
    /// 伤害不在按下那一刻结算，而是等 meleeHitDelay 秒（刀落下）在身前判定一次盒子，
    /// 这样「按了没马上掉血」是设计，不是漏判；一次挥击只判定一次。
    /// </summary>
    public class PlayerMeleeState : PlayerState
    {
        private float _elapsed;
        private float _duration;
        private float _strikeTimer;
        private bool _struck;

        public PlayerMeleeState(PlayerController player, StateMachine machine) : base(player, machine) { }

        public override void Enter()
        {
            Player.StopMovement();
            Player.Animation.Play(PlayerAnimations.Skill02, false);
            _duration = Mathf.Max(Player.Animation.GetDuration(PlayerAnimations.Skill02), 0.1f);
            _elapsed = 0f;
            _struck = false;

            // 判定时刻不能晚于动画本身，否则短动画会「挥完没伤害」；夹到 60% 处保证一定挥得出来
            _strikeTimer = Mathf.Min(Player.Tuning.meleeHitDelay, _duration * 0.6f);
        }

        public override void Tick()
        {
            _elapsed += Time.deltaTime;

            if (!_struck)
            {
                _strikeTimer -= Time.deltaTime;
                if (_strikeTimer <= 0f)
                {
                    _struck = true;
                    Player.MeleeStrike();
                }
            }

            if (_elapsed < _duration) return;

            var hasMoveInput = Player.HasMoveInput;
            Machine.ChangeState(hasMoveInput ? Player.MoveState : Player.IdleState);
        }

        public override void FixedTick()
        {
            // 近战攻击期间保持原地
            Player.StopMovement();
        }
    }
}
