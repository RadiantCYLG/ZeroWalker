using UnityEngine;
using ZeroWalker.Combat;
using ZeroWalker.Core.Ai;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.Enemies
{
    /// <summary>
    /// 远程型敌人：固定不动，玩家进入视野后瞄准 → 开火 → 冷却。
    /// AI 决策全在 <see cref="RangedEnemyBrain"/>（纯 C#），这里只负责把「该开枪了」变成一颗子弹。
    /// </summary>
    public sealed class RangedEnemy : EnemyAgent
    {
        [Tooltip("子弹预制体（与玩家共用，靠 BulletSide 区分阵营）")]
        [SerializeField] private Bullet bulletPrefab;

        [Tooltip("出膛点；留空则用自身位置")]
        [SerializeField] private Transform muzzle;

        private RangedEnemyBrain _brain;

        protected override void Start()
        {
            base.Start();
            _brain = new RangedEnemyBrain(ContentLoader.Current.enemies.ranged);
        }

        private void FixedUpdate()
        {
            if (!TryGetSenses(out var senses))
                return;

            var intent = _brain.Tick(Time.fixedDeltaTime, senses);
            ApplyIntent(intent);

            if (intent.Shoot)
                Shoot();
        }

        private void Shoot()
        {
            if (bulletPrefab == null || Player == null)
                return;

            var origin = muzzle != null ? (Vector2)muzzle.position : (Vector2)transform.position;
            var direction = (Vector2)Player.transform.position - origin;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right;

            var bullet = Instantiate(bulletPrefab, origin, Quaternion.identity);
            bullet.Launch(direction, BulletSide.Enemy, false, ContentLoader.Current.enemies.ranged.bulletSpeed);
        }
    }
}
