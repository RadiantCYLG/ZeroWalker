using UnityEngine;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.Enemies
{
    /// <summary>
    /// 敌人被打时的表现（视图层）：订阅 <see cref="EnemyHealth"/> 的事件做闪色与倒地消失。
    /// 逻辑层只广播「掉血了」「死了」，改成播放死亡动画也只需要换这个组件。
    /// 注意：这里直接改渲染器颜色，不用 <c>SpriteFlash</c>——那个组件是「一次性特效」，
    /// Awake 里会把自己隐藏，挂在敌人身上会让敌人一进 Play 就看不见。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyView : MonoBehaviour
    {
        [Tooltip("受击闪色用的渲染器；留空则取子物体上的第一个 SpriteRenderer")]
        [SerializeField] private SpriteRenderer body;

        private EnemyHealth _health;
        private Color _baseColor = Color.white;
        private float _flashTimer;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();

            if (body == null)
                body = GetComponentInChildren<SpriteRenderer>();

            if (body != null)
                _baseColor = body.color;
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

        private void Update()
        {
            if (_flashTimer <= 0f) return;

            _flashTimer -= Time.deltaTime;
            if (_flashTimer > 0f || body == null) return;

            body.color = _baseColor;
        }

        private void HandleDamaged()
        {
            if (body == null) return;

            var tuning = ContentLoader.Current.enemies;
            body.color = tuning.hitColor.ToColor();
            _flashTimer = tuning.hitFlashDuration;
        }

        private void HandleDied()
        {
            // 倒地后留一点点时间再销毁，避免最后一帧突然消失
            _flashTimer = 0f;
            if (body != null) body.color = _baseColor;

            Destroy(gameObject, ContentLoader.Current.enemies.deathDelay);
        }
    }
}
