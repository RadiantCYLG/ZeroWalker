namespace ZeroWalker.Core.Rules
{
    /// <summary>
    /// 冲刺冷却计时器。纯逻辑，不依赖 UnityEngine，可单独测试。
    /// </summary>
    public sealed class DashCooldown
    {
        private float _duration = 1f;
        private float _remaining;

        /// <summary>冷却时间（秒）。</summary>
        public float Duration
        {
            get { return _duration; }
        }

        /// <summary>剩余冷却时间（秒），0 表示可用。</summary>
        public float Remaining
        {
            get { return _remaining; }
        }

        public bool IsReady
        {
            get { return _remaining <= 0f; }
        }

        public void Configure(float duration)
        {
            _duration = duration < 0f ? 0f : duration;
        }

        public void Start()
        {
            _remaining = _duration;
        }

        public void Reset()
        {
            _remaining = 0f;
        }

        public void Tick(float deltaSeconds)
        {
            if (_remaining <= 0f)
            {
                return;
            }

            _remaining -= deltaSeconds < 0f ? 0f : deltaSeconds;
            if (_remaining < 0f)
            {
                _remaining = 0f;
            }
        }
    }
}
