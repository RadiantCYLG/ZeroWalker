using UnityEngine;
using ZeroWalker.Core.Model;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.VFX
{
    /// <summary>
    /// 通用的一次性精灵闪光（枪口火光、命中火花等）。
    /// 播放时会放大并淡出，结束后自动隐藏。
    /// 时长与缩放区间来自 Content/data/effects.json 中的配置档（默认使用 muzzleFlash）。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;

        private float _duration = 0.06f;
        private float _startScale = 0.9f;
        private float _endScale = 1.35f;
        private float _elapsed;
        private bool _hasExplicitProfile;
        private Color _baseColor;
        private Color _playColor;

        /// <summary>是否正在播放。</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>一次闪光的总时长（秒）。</summary>
        public float Duration
        {
            get { return _duration; }
        }

        private void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            _baseColor = target.color;
            _playColor = _baseColor;

            // 默认用枪口火光档；若挂在同一物件上的组件（如 HitEffect）已经显式换档，就不再覆盖。
            // SpriteFlash.Awake 与 HitEffect.Awake 的先后顺序不确定，靠这个标记保证两边都对。
            if (!_hasExplicitProfile)
            {
                UseProfile(ContentLoader.Current.effects.muzzleFlash);
            }

            Hide();
        }

        /// <summary>切换到另一档闪光参数（例如命中特效使用 hitFlash 档）。</summary>
        public void UseProfile(FlashTuning profile)
        {
            if (profile == null) return;

            _hasExplicitProfile = true;
            _duration = profile.duration;
            _startScale = profile.startScale;
            _endScale = profile.endScale;
        }

        /// <summary>从头播放一次闪光（保持原色）。</summary>
        public void Play()
        {
            Play(Color.white);
        }

        /// <summary>从头播放一次闪光，tint 会与自身颜色相乘（白色表示不改变原色）。</summary>
        public void Play(Color tint)
        {
            _playColor = _baseColor * tint;
            _playColor.a = _baseColor.a;

            _elapsed = 0f;
            IsPlaying = true;
            gameObject.SetActive(true);

            target.enabled = true;
            target.color = _playColor;
            transform.localScale = Vector3.one * _startScale;
        }

        private void Update()
        {
            if (!IsPlaying) return;

            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / Mathf.Max(_duration, 0.0001f));

            var color = _playColor;
            color.a = _playColor.a * (1f - t);
            target.color = color;
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, t);

            if (t >= 1f) Hide();
        }

        private void Hide()
        {
            IsPlaying = false;
            if (target != null) target.enabled = false;
        }
    }
}
