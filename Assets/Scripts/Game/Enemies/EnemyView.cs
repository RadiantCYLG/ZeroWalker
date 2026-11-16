using UnityEngine;
using ZeroWalker.Framework.Content;
using ZeroWalker.VFX;

namespace ZeroWalker.Enemies
{
    /// <summary>
    /// 敌人被打时的表现（视图层）：订阅 <see cref="EnemyHealth"/> 的事件做闪色与倒地消失。
    /// 逻辑层只广播「掉血了」「死了」，改成播放死亡动画也只需要换这个组件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyView : MonoBehaviour
    {
        [Tooltip("受击闪光组件；留空则取同物体上的 SpriteFlash")]
        [SerializeField] private SpriteFlash flash;

        private EnemyHealth _health;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();

            if (flash == null)
                flash = GetComponent<SpriteFlash>();
        }

        private void OnEnable()
        {
            if (_health == null) return;

            _health.Damaged += HandleDamaged;
            _health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (_health == null) return;

            _health.Damaged -= HandleDamaged;
            _health.Died -= HandleDied;
        }

        private void HandleDamaged()
        {
            if (flash == null) return;

            flash.Play(ContentLoader.Current.target.hitColor.ToColor());
        }

        private void HandleDied()
        {
            // 倒地后留一点点时间再销毁，避免最后一帧突然消失
            Destroy(gameObject, ContentLoader.Current.enemies.deathDelay);
        }
    }
}
