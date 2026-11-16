using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ZeroWalker.Core.Model;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.Player
{
    /// <summary>
    /// 玩家输入的统一读取入口，基于新版 Input System Package（Keyboard/Mouse 轮询）。
    /// 默认按键：A/D 左右、W/S 前后纵深、左 Shift 冲刺、F 近战、鼠标左键射击；
    /// 具体绑定值来自 Content/data/input.json，改表即可换键，无需改代码（解析在 InputBindings）。
    /// 另提供「调试输入」覆盖，便于无人值守地验证状态机流程（仅编辑器/开发版生效）。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("调试输入（覆盖真实按键，仅编辑器/开发版生效）")]
        [SerializeField] private bool useOverride;
        [SerializeField, Range(-1f, 1f)] private float overrideMoveAxis;
        [SerializeField, Range(-1f, 1f)] private float overrideDepthAxis;
        [SerializeField] private bool overrideAttackHeld;
        [SerializeField] private bool overrideDashHeld;
        [SerializeField] private bool overrideMeleeHeld;

        private InputBindings _bindings;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 诊断用：只在鼠标键状态切换时打一行，确认「按的是哪个键、被读成了什么」
        private bool _loggedAttackHeld;
        private bool _loggedAimHeld;
#endif
        private bool _attackPressed;
        private bool _dashPressed;
        private bool _dashHeld;
        private bool _dashSecondaryHeld;
        private bool _dashSecondaryPressed;
        private bool _meleePressed;
        private bool _meleeHeld;

        /// <summary>左右移动轴：-1 左、0 静止、1 右。</summary>
        public float MoveAxis { get; private set; }

        /// <summary>纵深移动轴：-1 向下（远）、0 不动、1 向上（近）。</summary>
        public float DepthAxis { get; private set; }

        /// <summary>把两个轴合成一个向量，交给 PlayerController.Move 使用。</summary>
        public Vector2 Axis2D
        {
            get { return new Vector2(MoveAxis, DepthAxis); }
        }

        /// <summary>射击键是否处于按住状态。</summary>
        public bool AttackHeld { get; private set; }

        /// <summary>射击键是否在本帧被按下。</summary>
        public bool AttackPressed => _attackPressed;

        /// <summary>冲刺键是否在本帧被按下（两个冲刺键任一按下即可，边沿触发，长按不会连续冲刺）。</summary>
        public bool DashPressed => _dashPressed || _dashSecondaryPressed;

        /// <summary>近战键（F）是否在本帧被按下（边沿触发）。</summary>
        public bool MeleePressed => _meleePressed;

        /// <summary>瞄准键是否按住（按住时显示准星、射击朝准星）。</summary>
        public bool AimHeld { get; private set; }

        /// <summary>准星所在的屏幕坐标（像素），瞄准时每帧更新。</summary>
        public Vector2 AimScreenPosition { get; private set; }

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogBindings();
#endif
        }

        /// <summary>把解析结果打出来，确认「表里写的键」真的变成了「代码里的键」。</summary>
        private void LogBindings()
        {
            Debug.Log("[Input] 绑定：attack=" + Describe(_bindings.attackMouseButton, _bindings.attackKey)
                + " aim=" + Describe(_bindings.aimMouseButton, Key.None)
                + " dash=" + _bindings.dash + "/" + _bindings.dashSecondary
                + " melee=" + _bindings.melee);
        }

        private static string Describe(MouseButton? mouseButton, Key key)
        {
            if (mouseButton.HasValue) return "鼠标" + mouseButton.Value;
            return key == Key.None ? "无" : key.ToString();
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
            var previousDashHeld = _dashHeld;
            var previousDashSecondaryHeld = _dashSecondaryHeld;
            var previousMeleeHeld = _meleeHeld;

            if (OverrideEnabled)
            {
                MoveAxis = Mathf.Clamp(overrideMoveAxis, -1f, 1f);
                DepthAxis = Mathf.Clamp(overrideDepthAxis, -1f, 1f);

                AttackHeld = overrideAttackHeld;
                _attackPressed = AttackHeld && !previousAttackHeld;

                _dashHeld = overrideDashHeld;
                _dashPressed = _dashHeld && !previousDashHeld;
                _dashSecondaryHeld = false;
                _dashSecondaryPressed = false;

                _meleeHeld = overrideMeleeHeld;
                _meleePressed = _meleeHeld && !previousMeleeHeld;

                // 调试覆盖不模拟鼠标瞄准，准星停在屏幕中心
                AimHeld = false;
                AimScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                return;
            }

            var keyboard = Keyboard.current;
            var left = InputBindings.IsHeld(keyboard, _bindings.moveLeft) ? 1f : 0f;
            var right = InputBindings.IsHeld(keyboard, _bindings.moveRight) ? 1f : 0f;
            MoveAxis = right - left;

            var up = InputBindings.IsHeld(keyboard, _bindings.moveUp) ? 1f : 0f;
            var down = InputBindings.IsHeld(keyboard, _bindings.moveDown) ? 1f : 0f;
            DepthAxis = up - down;

            AttackHeld = _bindings.ReadAttack();
            _attackPressed = AttackHeld && !previousAttackHeld;

            _dashHeld = InputBindings.IsHeld(keyboard, _bindings.dash);
            _dashPressed = _dashHeld && !previousDashHeld;

            _dashSecondaryHeld = InputBindings.IsHeld(keyboard, _bindings.dashSecondary);
            _dashSecondaryPressed = _dashSecondaryHeld && !previousDashSecondaryHeld;

            _meleeHeld = InputBindings.IsHeld(keyboard, _bindings.melee);
            _meleePressed = _meleeHeld && !previousMeleeHeld;

            AimHeld = _bindings.ReadAim();

            var mouse = Mouse.current;
            if (mouse != null)
            {
                AimScreenPosition = mouse.position.ReadValue();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (AttackHeld != _loggedAttackHeld || AimHeld != _loggedAimHeld)
            {
                _loggedAttackHeld = AttackHeld;
                _loggedAimHeld = AimHeld;
                Debug.Log("[Input] attack=" + AttackHeld + " aim=" + AimHeld
                    + " left=" + (mouse != null && mouse.leftButton.isPressed)
                    + " right=" + (mouse != null && mouse.rightButton.isPressed)
                    + " middle=" + (mouse != null && mouse.middleButton.isPressed));
            }
#endif
        }

        /// <summary>供自动化验证/演示使用的输入覆盖（发行版中为空操作）。</summary>
        public void SetOverride(
            bool enable,
            float moveAxis = 0f,
            float depthAxis = 0f,
            bool attackHeld = false,
            bool dashHeld = false,
            bool meleeHeld = false)
        {
            useOverride = enable;
            overrideMoveAxis = Mathf.Clamp(moveAxis, -1f, 1f);
            overrideDepthAxis = Mathf.Clamp(depthAxis, -1f, 1f);
            overrideAttackHeld = attackHeld;
            overrideDashHeld = dashHeld;
            overrideMeleeHeld = meleeHeld;
        }
    }
}
