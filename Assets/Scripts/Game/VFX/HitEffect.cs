using UnityEngine;
using ZeroWalker.Core.Model;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.VFX
{
    /// <summary>
    /// 击中特效：播放一次精灵闪光，视觉效果结束后销毁自身实例。
    /// 由 Bullet 在命中 ShootableTarget 时实例化到命中点。
    /// 闪光参数取 effects.json 的 hitFlash 档，颜色与销毁延迟也来自该表。
    /// </summary>
    [RequireComponent(typeof(SpriteFlash))]
    public class HitEffect : MonoBehaviour
    {
        [SerializeField] private SpriteFlash flash;

        private Color _tint;

        /// <summary>特效可调数值（来自 Content/data/effects.json）。</summary>
        private EffectsTuning Tuning
        {
            get { return ContentLoader.Current.effects; }
        }

        private void Awake()
        {
            if (flash == null)
                flash = GetComponent<SpriteFlash>();

            var effects = Tuning;

            // 命中火花比枪口火光更大更慢，使用 hitFlash 档
            _tint = effects.hitEffectTint.ToColor();
            if (flash != null) flash.UseProfile(effects.hitFlash);
        }

        /// <summary>由子弹在生成时调用，让特效颜色与射出它的子弹保持一致。</summary>
        public void SetTint(Color color)
        {
            _tint = color;
        }

        private void Start()
        {
            if (flash == null)
            {
                Destroy(gameObject);
                return;
            }

            flash.Play(_tint);
            Destroy(gameObject, flash.Duration + Tuning.hitEffectDestroyDelay);
        }
    }
}
