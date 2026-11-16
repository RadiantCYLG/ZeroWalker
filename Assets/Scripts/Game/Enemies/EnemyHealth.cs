using System;
using UnityEngine;
using ZeroWalker.Combat;
using ZeroWalker.Core.Rules;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.Enemies
{
    /// <summary>敌人种类：决定血量从 enemies.json 的哪一段取。</summary>
    public enum EnemyKind
    {
        Melee,
        Ranged
    }

    /// <summary>
    /// 敌人血量（逻辑层）。实现 <see cref="IDamageable"/>，子弹只认接口不认具体敌人类型。
    /// 死亡时关掉子碰撞体并广播事件；闪色、销毁这些表现交给 <see cref="EnemyView"/> 订阅。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Tooltip("敌人种类：决定血量取 enemies.json 的哪一段")]
        [SerializeField] private EnemyKind kind = EnemyKind.Melee;

        /// <summary>掉血（包括致死那一击）时触发。</summary>
        public event Action Damaged;

        /// <summary>血量归零时触发一次。</summary>
        public event Action Died;

        private Health _health;
        private Collider2D[] _colliders;
        private bool _dead;

        /// <summary>是否已经死亡（尸体不再受击）。</summary>
        public bool IsDead => _dead;

        /// <summary>当前血量（只读，调试用）。</summary>
        public int Current => _health != null ? _health.Current : 0;

        /// <summary>最大血量，按 kind 从内容表取。</summary>
        public int Max
        {
            get
            {
                var tuned = ContentLoader.Current.enemies;
                return kind == EnemyKind.Melee ? tuned.melee.maxHealth : tuned.ranged.maxHealth;
            }
        }

        private void Awake()
        {
            _colliders = GetComponentsInChildren<Collider2D>();
            _health = new Health(Max);
        }

        /// <summary>扣血；死亡后忽略后续伤害。</summary>
        public void TakeDamage(int amount)
        {
            if (_dead) return;

            _health.ApplyDamage(amount);
            Damaged?.Invoke();

            if (!_health.IsDead) return;

            _dead = true;

            // 关碰撞属于判定逻辑：尸体不该再挡子弹
            for (var i = 0; i < _colliders.Length; i++)
                _colliders[i].enabled = false;

            Died?.Invoke();
        }
    }
}
