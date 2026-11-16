using UnityEngine;
using ZeroWalker.Core.Ai;
using ZeroWalker.Player;

namespace ZeroWalker.Enemies
{
    /// <summary>
    /// 敌人基类：负责「把大脑的意图接到刚体和朝向上」。
    /// 具体是巡逻还是定点射击交给子类里的状态机（都在 ZeroWalker.Core.Ai，纯 C#）。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(EnemyHealth))]
    public abstract class EnemyAgent : MonoBehaviour
    {
        [Tooltip("用于左右翻面的视觉节点；留空则翻自身")]
        [SerializeField] private Transform visual;

        private Rigidbody2D _body;
        private EnemyHealth _health;
        private PlayerController _player;

        protected Rigidbody2D Body => _body;
        protected EnemyHealth Health => _health;
        protected PlayerController Player => _player;

        protected virtual void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _health = GetComponent<EnemyHealth>();

            // 平面走位：敌人也不吃重力，位置只由速度决定
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (visual == null)
                visual = transform;
        }

        protected virtual void Start()
        {
            _player = FindObjectOfType<PlayerController>();
        }

        private void OnEnable()
        {
            if (_health != null)
                _health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Died -= HandleDied;
        }

        private void HandleDied()
        {
            _body.velocity = Vector2.zero;
            enabled = false;
        }

        /// <summary>采集本帧感知。玩家不在场景里时返回 false。</summary>
        protected bool TryGetSenses(out EnemySenses senses)
        {
            senses = default;

            if (_player == null)
                return false;

            var self = (Vector2)transform.position;
            var other = (Vector2)_player.transform.position;

            senses = new EnemySenses
            {
                HasPlayer = true,
                SelfX = self.x,
                SelfY = self.y,
                PlayerX = other.x,
                PlayerY = other.y,
                PlayerDistance = Vector2.Distance(self, other)
            };
            return true;
        }

        /// <summary>把大脑给出的意图落实成速度与朝向。</summary>
        protected void ApplyIntent(in EnemyIntent intent)
        {
            _body.velocity = new Vector2(intent.MoveX, 0f);

            if (intent.Facing == 0 || visual == null)
                return;

            var scale = visual.localScale;
            visual.localScale = new Vector3(Mathf.Abs(scale.x) * intent.Facing, scale.y, scale.z);
        }
    }
}
