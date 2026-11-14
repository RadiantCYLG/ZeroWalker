using UnityEngine;
using ZeroWalker.Core.Model;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.Combat
{
    /// <summary>
    /// 标靶的表现层（视图）：只订阅 <see cref="ShootableTarget"/> 的 Damaged / Died / Revived 事件，
    /// 负责受击闪色、倒地缩放与隐藏、复活还原。
    /// 它不参与任何判定——血量、碰撞开关、复活计时都在 ShootableTarget 里。
    /// 闪色时长与颜色、死亡缩放来自 Content/data/target.json。
    /// </summary>
    [DisallowMultipleComponent]
    public class TargetView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private ShootableTarget _target;
        private Color _baseColor;
        private float _flashTimer;

        /// <summary>标靶表现参数（来自 Content/data/target.json）。</summary>
        private TargetTuning Tuning
        {
            get { return ContentLoader.Current.target; }
        }

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            _target = GetComponent<ShootableTarget>();
        }

        private void OnEnable()
        {
            if (_target == null) return;

            _target.Damaged += HandleDamaged;
            _target.Died += HandleDied;
            _target.Revived += HandleRevived;
        }

        private void OnDisable()
        {
            if (_target == null) return;

            _target.Damaged -= HandleDamaged;
            _target.Died -= HandleDied;
            _target.Revived -= HandleRevived;
        }

        private void Update()
        {
            if (_flashTimer <= 0f) return;

            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f && spriteRenderer != null)
                spriteRenderer.color = _baseColor;
        }

        private void HandleDamaged()
        {
            if (spriteRenderer == null) return;

            spriteRenderer.color = Tuning.hitColor.ToColor();
            _flashTimer = Tuning.hitFlashDuration;
        }

        private void HandleDied()
        {
            _flashTimer = 0f;
            transform.localScale = Vector3.one * Tuning.deathScale;

            if (spriteRenderer != null) spriteRenderer.enabled = false;
        }

        private void HandleRevived()
        {
            _flashTimer = 0f;
            transform.localScale = Vector3.one;

            if (spriteRenderer == null) return;

            spriteRenderer.color = _baseColor;
            spriteRenderer.enabled = true;
        }
    }
}
