using System;
using UnityEngine;
using ZeroWalker.Core.Model;
using ZeroWalker.Core.Rules;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.Combat
{
    /// <summary>
    /// 可被击中的标靶（逻辑/控制层）：只负责血量、碰撞开关和复活计时。
    /// 它不碰渲染——闪色、死亡缩放、隐藏都由 <see cref="TargetView"/> 订阅下面三个事件完成，
    /// 所以换表现（改颜色、加音效、换成粒子）不用动这里的判定逻辑。
    /// 数值来自 Content/data/target.json，扣血规则在 Core/Rules/Health。
    /// </summary>
    public class ShootableTarget : MonoBehaviour
    {
        /// <summary>扣血后广播（无论这一下是否致死）。</summary>
        public event Action Damaged;

        /// <summary>血量归零、倒下时广播。</summary>
        public event Action Died;

        /// <summary>复活时广播。</summary>
        public event Action Revived;

        private Health _health;
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
            _colliders = GetComponentsInChildren<Collider2D>();
            _health = new Health(Tuning.maxHealth);

            // 场景里的标靶没有挂视图时补一个：逻辑照常广播，视图自己订阅。
            // 以后做成标靶预制体时把 TargetView 挂在预制体上，这里就不会再补。
            if (GetComponent<TargetView>() == null) gameObject.AddComponent<TargetView>();
        }

        private void Update()
        {
            if (!_dead || !Tuning.respawn) return;

            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f) Revive();
        }

        /// <summary>受到伤害：扣血、广播受击；血量归零则倒下。</summary>
        public void TakeDamage(int amount)
        {
            if (_dead) return;

            _health.ApplyDamage(amount);
            Damaged?.Invoke();

            if (_health.IsDead) Die();
        }

        private void Die()
        {
            _dead = true;
            _respawnTimer = Tuning.respawnDelay;

            // 关碰撞属于判定逻辑：倒地期间不该再被子弹命中
            foreach (var collider in _colliders) collider.enabled = false;
            Died?.Invoke();
        }

        private void Revive()
        {
            _dead = false;
            _health.Reset();

            foreach (var collider in _colliders) collider.enabled = true;
            Revived?.Invoke();
        }
    }
}
