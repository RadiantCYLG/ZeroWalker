using UnityEngine;
using ZeroWalker.Core.Ai;
using ZeroWalker.Framework.Content;
using ZeroWalker.VFX;

namespace ZeroWalker.Enemies
{
    /// <summary>
    /// 近战型地面敌人：在出生点附近的固定路径巡逻，玩家进入发现半径后加速追击，
    /// 贴近到攻击距离时前摇 → 扑击。AI 决策全在 <see cref="MeleeEnemyBrain"/>（纯 C#）。
    /// </summary>
    public sealed class MeleeEnemy : EnemyAgent
    {
        [Tooltip("扑击命中玩家时播放的特效")]
        [SerializeField] private HitEffect hitEffectPrefab;

        private MeleeEnemyBrain _brain;

        protected override void Start()
        {
            base.Start();

            // 巡逻路径以出生点为中心，所以把出生时的 x 当原点交给大脑
            _brain = new MeleeEnemyBrain(ContentLoader.Current.enemies.melee, transform.position.x);
        }

        private void FixedUpdate()
        {
            if (!TryGetSenses(out var senses))
                return;

            var intent = _brain.Tick(Time.fixedDeltaTime, senses);
            ApplyIntent(intent);

            if (intent.Attack)
                HitPlayer();
        }

        /// <summary>玩家还没有血量，命中只播特效。</summary>
        private void HitPlayer()
        {
            if (hitEffectPrefab == null || Player == null)
                return;

            var effect = Instantiate(hitEffectPrefab, Player.transform.position, Quaternion.identity);
            effect.SetTint(ContentLoader.Current.effects.hitEffectTint.ToColor());
        }
    }
}
