using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ZeroWalker.Core.Model;
using ZeroWalker.Framework.Content;

namespace ZeroWalker.Player
{
    /// <summary>
    /// 玩家输入的统一读取入口，基于新版 Input System Package 的 Input Actions。
    ///
    /// 数据来源分两层：
    /// 1. 动作的定义与形状在 Resources/Content/Input/PlayerControls.inputactions 里
    ///    （Move 是 2D 向量复合键、Dash 有两条绑定、Aim/Pointer 是鼠标）；
    /// 2. 具体是哪个键由 Content/data/input.json 决定，Awake 时通过覆盖绑定（ApplyBindingOverride）盖上去。
    ///    换键只改表，不用回编辑器改资产。
    ///
    /// PlayerController 每帧开头调 Poll()，把动作的当前状态抓成一组属性，
    /// 保证同一帧内所有状态机读到的是同一份快照。另有「调试输入」覆盖，仅编辑器/开发版生效。
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("输入动作资产（留空则从 Resources/Content/Input 加载）")]
        [SerializeField] private InputActionAsset actions;

        [Header("调试输入（覆盖真实按键，仅编辑器/开发版生效）")]
        [SerializeField] private bool useOverride;
        [SerializeField, Range(-1f, 1f)] private float overrideMoveAxis;
        [SerializeField, Range(-1f, 1f)] private float overrideDepthAxis;
        [SerializeField] private bool overrideAttackHeld;
        [SerializeField] private bool overrideDashHeld;
        [SerializeField] private bool overrideMeleeHeld;

        private PlayerControls _controls;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 诊断用：只在鼠标键状态切换时打一行，确认「按的是哪个键、被读成了什么」
        private bool _loggedAttackHeld;
        private bool _loggedAimHeld;
#endif
        private bool _attackHeld;
        private bool _dashHeld;
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
        public bool AttackPressed { get; private set; }

        /// <summary>冲刺键是否在本帧被按下（左 Shift 或空格，两条绑定合成一个动作，长按不会连续冲刺）。</summary>
        public bool DashPressed { get; private set; }

        /// <summary>近战键（F）是否在本帧被按下（边沿触发）。</summary>
        public bool MeleePressed { get; private set; }

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
            var asset = actions != null ? actions : PlayerControlsLoader.Load();
            _controls = new PlayerControls(asset);
            ApplyBindings(ContentLoader.Current.input);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogBindings();
#endif
        }

        private void OnEnable()
        {
            if (_controls != null)
            {
                _controls.Enable();
            }
        }

        private void OnDisable()
        {
            if (_controls != null)
            {
                _controls.Disable();
            }
        }

        /// <summary>按内容表覆盖动作的键位；解析失败的项会退回默认键并报错。</summary>
        public void ApplyBindings(InputTuning tuning)
        {
            if (_controls == null || tuning == null)
            {
                return;
            }

            _controls.ApplyTuning(tuning);
        }

        /// <summary>由 PlayerController 在每帧开始时调用，保证同一帧内读取到一致的值。</summary>
        public void Poll()
        {
            var previousDashHeld = _dashHeld;
            var previousMeleeHeld = _meleeHeld;

            if (OverrideEnabled)
            {
                MoveAxis = Mathf.Clamp(overrideMoveAxis, -1f, 1f);
                DepthAxis = Mathf.Clamp(overrideDepthAxis, -1f, 1f);

                AttackHeld = overrideAttackHeld;
                AttackPressed = AttackHeld && !_attackHeld;
                _attackHeld = AttackHeld;

                _dashHeld = overrideDashHeld;
                DashPressed = _dashHeld && !previousDashHeld;

                _meleeHeld = overrideMeleeHeld;
                MeleePressed = _meleeHeld && !previousMeleeHeld;

                // 调试覆盖不模拟鼠标瞄准，准星停在屏幕中心
                AimHeld = false;
                AimScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                return;
            }

            if (_controls == null || !_controls.IsReady)
            {
                MoveAxis = 0f;
                DepthAxis = 0f;
                AttackHeld = false;
                AttackPressed = false;
                DashPressed = false;
                MeleePressed = false;
                AimHeld = false;
                return;
            }

            var move = _controls.Move.ReadValue<Vector2>();
            MoveAxis = Mathf.Clamp(move.x, -1f, 1f);
            DepthAxis = Mathf.Clamp(move.y, -1f, 1f);

            AttackHeld = _controls.Attack.IsPressed();
            AttackPressed = _controls.Attack.WasPressedThisFrame();
            DashPressed = _controls.Dash.WasPressedThisFrame();
            MeleePressed = _controls.Melee.WasPressedThisFrame();
            AimHeld = _controls.Aim.IsPressed();

            if (AimHeld || Mouse.current != null)
            {
                AimScreenPosition = _controls.Pointer.ReadValue<Vector2>();
            }

            _attackHeld = AttackHeld;
            _dashHeld = DashPressed;
            _meleeHeld = MeleePressed;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (AttackHeld != _loggedAttackHeld || AimHeld != _loggedAimHeld)
            {
                _loggedAttackHeld = AttackHeld;
                _loggedAimHeld = AimHeld;
                var mouse = Mouse.current;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>把每条动作实际生效的绑定路径打出来，确认表里的键真的盖到了动作上。</summary>
        private void LogBindings()
        {
            if (_controls == null || !_controls.IsReady)
            {
                return;
            }

            Debug.Log("[Input] 绑定：Move=" + Describe(_controls.Move)
                + " Attack=" + Describe(_controls.Attack)
                + " Aim=" + Describe(_controls.Aim)
                + " Dash=" + Describe(_controls.Dash)
                + " Melee=" + Describe(_controls.Melee));
        }

        private static string Describe(InputAction action)
        {
            if (action == null)
            {
                return "无";
            }

            var parts = new List<string>();
            foreach (var binding in action.bindings)
            {
                if (binding.isComposite)
                {
                    continue;
                }

                var path = string.IsNullOrEmpty(binding.overridePath) ? binding.path : binding.overridePath;
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                parts.Add(path.Replace("<Keyboard>/", string.Empty).Replace("<Mouse>/", "鼠标"));
            }

            return parts.Count == 0 ? "无" : string.Join("/", parts);
        }
#endif
    }
}
