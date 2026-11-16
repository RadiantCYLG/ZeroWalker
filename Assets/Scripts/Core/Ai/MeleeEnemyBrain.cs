using ZeroWalker.Core.Model;

namespace ZeroWalker.Core.Ai
{
    /// <summary>近战敌人的状态。</summary>
    public enum MeleeEnemyState
    {
        /// <summary>在出生点附近来回巡逻。</summary>
        Patrol,

        /// <summary>发现玩家，加速靠近。</summary>
        Chase,

        /// <summary>扑击前摇：站住蓄力，给玩家反应时间。</summary>
        Windup,

        /// <summary>扑出去，途中最多命中一次。</summary>
        Lunge,

        /// <summary>扑完的硬直。</summary>
        Recover
    }

    /// <summary>
    /// 近战型地面敌人的大脑：纯 C# 有限状态机，不碰引擎。
    /// 每帧吃一份 <see cref="EnemySenses"/>、吐一个 <see cref="EnemyIntent"/>，
    /// 由 Game 层的敌人组件落实成刚体速度与攻击判定，所以状态机可以脱离 Unity 单测。
    /// 状态迁移：巡逻 →(看见玩家) 追击 →(进入攻击距离) 前摇 → 扑击 → 硬直 → 追击/巡逻。
    /// </summary>
    public sealed class MeleeEnemyBrain
    {
        private readonly MeleeEnemyTuning _tuning;
        private readonly float _originX;
        private float _timer;
        private int _facing = 1;
        private int _patrolDirection = 1;
        private bool _hitThisLunge;

        /// <summary>当前状态（只读，调试与测试用）。</summary>
        public MeleeEnemyState State { get; private set; } = MeleeEnemyState.Patrol;

        /// <summary>构造时记录出生点 X，作为巡逻范围的中点。</summary>
        public MeleeEnemyBrain(MeleeEnemyTuning tuning, float originX)
        {
            _tuning = tuning;
            _originX = originX;
        }

        public EnemyIntent Tick(float dt, EnemySenses senses)
        {
            var intent = new EnemyIntent { Facing = _facing };

            switch (State)
            {
                case MeleeEnemyState.Patrol:
                    Patrol(senses, ref intent);
                    break;
                case MeleeEnemyState.Chase:
                    Chase(senses, ref intent);
                    break;
                case MeleeEnemyState.Windup:
                    Windup(dt, senses, ref intent);
                    break;
                case MeleeEnemyState.Lunge:
                    Lunge(dt, senses, ref intent);
                    break;
                default:
                    Recover(dt, senses, ref intent);
                    break;
            }

            _facing = intent.Facing;
            return intent;
        }

        private bool CanSee(EnemySenses senses)
        {
            return senses.HasPlayer && senses.PlayerDistance <= _tuning.detectRadius;
        }

        private bool CanKeepChasing(EnemySenses senses)
        {
            return senses.HasPlayer && senses.PlayerDistance <= _tuning.loseRadius;
        }

        private void Patrol(EnemySenses senses, ref EnemyIntent intent)
        {
            if (CanSee(senses))
            {
                State = MeleeEnemyState.Chase;
                return;
            }

            // 走到巡逻范围边缘就掉头（先掉头再走，避免在边界反复抖）
            if (senses.SelfX >= _originX + _tuning.patrolRange)
            {
                _patrolDirection = -1;
            }
            else if (senses.SelfX <= _originX - _tuning.patrolRange)
            {
                _patrolDirection = 1;
            }

            intent.Facing = _patrolDirection;
            intent.MoveX = _tuning.patrolSpeed * _patrolDirection;
        }

        private void Chase(EnemySenses senses, ref EnemyIntent intent)
        {
            if (!CanKeepChasing(senses))
            {
                State = MeleeEnemyState.Patrol;
                return;
            }

            intent.Facing = senses.DeltaX >= 0f ? 1 : -1;

            if (senses.PlayerDistance <= _tuning.attackRange)
            {
                State = MeleeEnemyState.Windup;
                _timer = _tuning.windupTime;
                return;
            }

            intent.MoveX = _tuning.chaseSpeed * intent.Facing;
        }

        private void Windup(float dt, EnemySenses senses, ref EnemyIntent intent)
        {
            intent.Facing = senses.DeltaX >= 0f ? 1 : -1;
            _timer -= dt;

            if (_timer <= 0f)
            {
                State = MeleeEnemyState.Lunge;
                _timer = _tuning.lungeTime;
                _hitThisLunge = false;
            }
        }

        private void Lunge(float dt, EnemySenses senses, ref EnemyIntent intent)
        {
            intent.MoveX = _tuning.lungeSpeed * intent.Facing;
            _timer -= dt;

            if (!_hitThisLunge && senses.HasPlayer && senses.PlayerDistance <= _tuning.hitRadius)
            {
                intent.Attack = true;
                _hitThisLunge = true;
            }

            if (_timer <= 0f)
            {
                State = MeleeEnemyState.Recover;
                _timer = _tuning.cooldown;
                intent.MoveX = 0f;
            }
        }

        private void Recover(float dt, EnemySenses senses, ref EnemyIntent intent)
        {
            intent.Facing = senses.DeltaX >= 0f ? 1 : -1;
            _timer -= dt;
            if (_timer > 0f) return;

            State = CanSee(senses) ? MeleeEnemyState.Chase : MeleeEnemyState.Patrol;
        }
    }
}
