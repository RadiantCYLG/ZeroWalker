using UnityEngine;
using UnityEngine.InputSystem;
using ZeroWalker.Core.Model;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.Player
{
    /// <summary>
    /// 玩家输入的统一读取入口，基于新版 Input System Package（Keyboard/Mouse 轮询）。
    /// 默认按键：A/D 左右移动、空格跳跃、左 Shift 冲刺、F 近战、鼠标左键射击；
    /// 具体绑定值来自 Content/data/input.json，改表即可换键，无需改代码（解析在 InputBindings）。
    /// 另提供「调试输入」覆盖，便于无人值守地验证状态机流程（仅编辑器/开发版生效）。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("调试输入（覆盖真实按键，仅编辑器/开发版生效）")]
        [SerializeField] private bool useOverride;
        [SerializeField, Range(-1f, 1f)] private float overrideMoveAxis;
        [SerializeField] private bool overrideAttackHeld;
        [SerializeField] private bool overrideJumpHeld;
        [SerializeField] private bool overrideDashHeld;
        [SerializeField] private bool overrideMeleeHeld;

        private InputBindings _bindings;
        private bool _attackPressed;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private bool _dashPressed;
        private bool _dashHeld;
        private bool _meleePressed;
        private bool _meleeHeld;

        /// <summary>水平移动轴：-1 左、0 静止、1 右。</summary>
        public float MoveAxis { get; private set; }

        /// <summary>射击键是否处于按住状态。</summary>
        public bool AttackHeld { get; private set; }

        /// <summary>射击键是否在本帧被按下。</summary>
        public bool AttackPressed => _attackPressed;

        /// <summary>跳跃键是否在本帧被按下（边沿触发，长按不会连跳）。</summary>
        public bool JumpPressed => _jumpPressed;

        /// <summary>冲刺键是否在本帧被按下（边沿触发，长按不会连续冲刺）。</summary>
        public bool DashPressed => _dashPressed;

        /// <summary>近战键（F）是否在本帧被按下（边沿触发）。</summary>
        public bool MeleePressed => _meleePressed;

        private bool OverrideEnabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return useOverride;
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            ApplyBindings(ContentLoader.Current.input);
        }

        /// <summary>按内容表重新解析按键绑定；解析失败的项会退回默认键并报错。</summary>
        public void ApplyBindings(InputTuning tuning)
        {
            if (tuning == null)
            {
                return;
            }

            _bindings = InputBindings.FromTuning(tuning);
        }

        /// <summary>由 PlayerController 在每帧开始时调用，保证同一帧内读取到一致的值。</summary>
        public void Poll()
        {
            var previousAttackHeld = AttackHeld;
            var previousJumpHeld = _jumpHeld;
            var previousDashHeld = _dashHeld;
            var previousMeleeHeld = _meleeHeld;

            if (OverrideEnabled)
            {
                MoveAxis = Mathf.Clamp(overrideMoveAxis, -1f, 1f);
                AttackHeld = overrideAttackHeld;
                _attackPressed = AttackHeld && !previousAttackHeld;

                _jumpHeld = overrideJumpHeld;
                _jumpPressed = _jumpHeld && !previousJumpHeld;

                _dashHeld = overrideDashHeld;
                _dashPressed = _dashHeld && !previousDashHeld;

                _meleeHeld = overrideMeleeHeld;
                _meleePressed = _meleeHeld && !previousMeleeHeld;
                return;
            }

            var keyboard = Keyboard.current;
            var left = InputBindings.IsHeld(keyboard, _bindings.moveLeft) ? 1f : 0f;
            var right = InputBindings.IsHeld(keyboard, _bindings.moveRight) ? 1f : 0f;
            MoveAxis = right - left;

            AttackHeld = _bindings.ReadAttack();
            _attackPressed = AttackHeld && !previousAttackHeld;

            _jumpHeld = InputBindings.IsHeld(keyboard, _bindings.jump);
            _jumpPressed = _jumpHeld && !previousJumpHeld;

            _dashHeld = InputBindings.IsHeld(keyboard, _bindings.dash);
            _dashPressed = _dashHeld && !previousDashHeld;

            _meleeHeld = InputBindings.IsHeld(keyboard, _bindings.melee);
            _meleePressed = _meleeHeld && !previousMeleeHeld;
        }

        /// <summary>供自动化验证/演示使用的输入覆盖（发行版中为空操作）。</summary>
        public void SetOverride(
            bool enable,
            float moveAxis = 0f,
            bool attackHeld = false,
            bool jumpHeld = false,
            bool dashHeld = false,
            bool meleeHeld = false)
        {
            useOverride = enable;
            overrideMoveAxis = Mathf.Clamp(moveAxis, -1f, 1f);
            overrideAttackHeld = attackHeld;
            overrideJumpHeld = jumpHeld;
            overrideDashHeld = dashHeld;
            overrideMeleeHeld = meleeHeld;
        }
    }
}
