namespace ZeroWalker.Core.Rules
{
    /// <summary>
    /// 生命值规则：扣血、判死、复位。纯逻辑，不依赖 UnityEngine。
    /// </summary>
    public sealed class Health
    {
        private int _current;

        public Health(int max)
        {
            Max = max < 1 ? 1 : max;
            _current = Max;
        }

        public int Max { get; private set; }

        public int Current
        {
            get { return _current; }
        }

        public bool IsDead
        {
            get { return _current <= 0; }
        }

        /// <summary>扣血，返回实际扣除量（已死亡或伤害非正数时返回 0）。</summary>
        public int ApplyDamage(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return 0;
            }

            var before = _current;
            _current -= amount;
            if (_current < 0)
            {
                _current = 0;
            }

            return before - _current;
        }

        /// <summary>回满（复活时使用）。</summary>
        public void Reset()
        {
            _current = Max;
        }
    }
}
