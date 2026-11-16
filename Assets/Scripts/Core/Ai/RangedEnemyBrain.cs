using ZeroWalker.Core.Model;

namespace ZeroWalker.Core.Ai
{
    /// <summary>远程敌人的状态。</summary>
    public enum RangedEnemyState
    {
        /// <summary>待机，玩家进入视野才动。</summary>
        Scan,

        /// <summary>瞄准：站住不动，等前摇结束。</summary>
        Aim,

        /// <summary>开火（只持续一帧）。</summary>
        Fire,

        /// <summary>射击间隔。</summary>
        Cooldown
    }

    /// <summary>
    /// 远程型敌人的大脑：原地不动，玩家进入视野后「瞄准 → 开火 → 冷却」循环。
    /// 同样是纯 C# 状态机，开火只是把 <see cref="EnemyIntent.Shoot"/> 置真，
    /// 真正生成子弹的是 Game 层，所以「打什么、打多快」都能单独换。
    /// </summary>
    public sealed class RangedEnemyBrain
    {
        private readonly RangedEnemyTuning _tuning;
        private float _timer;
        private int _facing = 1;

        /// <summary>当前状态（只读，调试与测试用）。</summary>
        public RangedEnemyState State { get; private set; } = RangedEnemyState.Scan;

        public RangedEnemyBrain(RangedEnemyTuning tuning)
        {
            _tuning = tuning;
        }

        public EnemyIntent Tick(float dt, EnemySenses senses)
        {
            var intent = new EnemyIntent { Facing = _facing };

            switch (State)
            {
                case RangedEnemyState.Scan:
                    Scan(senses, ref intent);
                    break;
                case RangedEnemyState.Aim:
                    Aim(dt, senses, ref intent);
                    break;
                case RangedEnemyState.Fire:
                    Fire(senses, ref intent);
                    break;
                default:
                    Cooldown(dt, senses, ref intent);
                    break;
            }

            _facing = intent.Facing;
            return intent;
        }

        private bool CanSee(EnemySenses senses)
        {
            return senses.HasPlayer && senses.PlayerDistance <= _tuning.detectRadius;
        }

        private void Scan(EnemySenses senses, ref EnemyIntent intent)
        {
            if (!CanSee(senses)) return;

            intent.Facing = senses.DeltaX >= 0f ? 1 : -1;
            State = RangedEnemyState.Aim;
            _timer = _tuning.aimTime;
        }

        private void Aim(float dt, EnemySenses senses, ref EnemyIntent intent)
        {
            if (!CanSee(senses))
            {
                State = RangedEnemyState.Scan;
                return;
            }

            intent.Facing = senses.DeltaX >= 0f ? 1 : -1;
            _timer -= dt;
            if (_timer <= 0f) State = RangedEnemyState.Fire;
        }

        private void Fire(EnemySenses senses, ref EnemyIntent intent)
        {
            intent.Facing = senses.DeltaX >= 0f ? 1 : -1;
            intent.Shoot = true;
            State = RangedEnemyState.Cooldown;
            _timer = _tuning.fireCooldown;
        }

        private void Cooldown(float dt, EnemySenses senses, ref EnemyIntent intent)
        {
            intent.Facing = senses.DeltaX >= 0f ? 1 : -1;
            _timer -= dt;
            if (_timer > 0f) return;

            State = CanSee(senses) ? RangedEnemyState.Aim : RangedEnemyState.Scan;
            if (State == RangedEnemyState.Aim) _timer = _tuning.aimTime;
        }
    }
}
