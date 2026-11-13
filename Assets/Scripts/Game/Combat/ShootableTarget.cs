using UnityEngine;
using ZeroWalker.Core.Model;
using ZeroWalker.Core.Rules;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.Combat
{
    /// <summary>
    /// 可被击中的标靶，用来验证子弹命中与伤害流程。
    /// 受击时闪色并缩放，血量归零后隐藏；开启复活时会在原地重新出现。
    /// 生命值/闪色/复活等数值来自 Content/data/target.json，扣血规则在 Core/Rules/Health。
    /// </summary>
    public class ShootableTarget : MonoBehaviour
    {
        [Header("受击表现")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Color _baseColor;
        private Health _health;
        private float _flashTimer;
        private float _respawnTimer;
        private Collider2D[] _colliders;
        private bool _dead;

        /// <summary>标靶可调数值（来自 Content/data/target.json）。</summary>
        private TargetTuning Tuning
        {
            get { return ContentLoader.Current.target; }
        }

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _colliders = GetComponentsInChildren<Collider2D>();
            _baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            _health = new Health(Tuning.maxHealth);
        }

        private void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f && spriteRenderer != null)
                    spriteRenderer.color = _baseColor;
            }

            if (_dead && Tuning.respawn)
            {
                _respawnTimer -= Time.deltaTime;
                if (_respawnTimer <= 0f) Revive();
            }
        }

        /// <summary>受到伤害。</summary>
        public void TakeDamage(int amount)
        {
            if (_dead) return;

            _health.ApplyDamage(amount);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Tuning.hitColor.ToColor();
                _flashTimer = Tuning.hitFlashDuration;
            }

            if (_health.IsDead) Die();
        }

        private void Die()
        {
            _dead = true;
            _respawnTimer = Tuning.respawnDelay;

            transform.localScale = Vector3.one * Tuning.deathScale;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            foreach (var collider in _colliders) collider.enabled = false;

            if (!Tuning.respawn) Destroy(gameObject);
        }

        private void Revive()
        {
            _dead = false;
            _health.Reset();
            _flashTimer = 0f;

            transform.localScale = Vector3.one;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = _baseColor;
                spriteRenderer.enabled = true;
            }

            foreach (var collider in _colliders) collider.enabled = true;
        }
    }
}
