using UnityEngine;
using UnityEngine.InputSystem;
using ZeroWalker.Core.Model;

namespace ZeroWalker.Player
{
    /// <summary>
    /// 玩家输入的动作封装：持有 PlayerControls.inputactions 里 "Player" 这张映射的六个动作，
    /// 负责启停，并按 input.json 覆盖每条绑定的键。
    ///
    /// 分层：动作的「形状」在资产里（Move 是 2D 向量复合、Dash 两条绑定），「具体键位」在 input.json 里。
    /// 这样换键只改表，不用回编辑器拖资产；同时读输入的地方一律是 InputAction，
    /// 不再有 Keyboard.current[key].isPressed 这种散落的手工采样。
    ///
    /// 故意不用 InputActionImporter 生成的包装类（PlayerControls 的 Generate C# Class）：
    /// 那份代码要跟着资产重新生成，键位又会变成「生成代码里的硬编码」，
    /// 而这里恰恰要求键位由 input.json 决定。
    /// </summary>
    public sealed class PlayerControls
    {
        private const string MapName = "Player";

        public PlayerControls(InputActionAsset asset)
        {
            Asset = asset;
            var map = asset != null ? asset.FindActionMap(MapName, false) : null;
            if (map == null)
            {
                Debug.LogError("[Input] 输入资产里找不到动作映射 \"" + MapName +
                    "\"。检查 Resources/Content/Input/PlayerControls.inputactions 是否导入成功。");
                return;
            }

            Map = map;
            Move = map.FindAction("Move", false);
            Attack = map.FindAction("Attack", false);
            Aim = map.FindAction("Aim", false);
            Dash = map.FindAction("Dash", false);
            Melee = map.FindAction("Melee", false);
            Pointer = map.FindAction("Pointer", false);
        }

        public InputActionAsset Asset { get; }

        public InputActionMap Map { get; }

        public InputAction Move { get; }

        public InputAction Attack { get; }

        public InputAction Aim { get; }

        public InputAction Dash { get; }

        public InputAction Melee { get; }

        /// <summary>鼠标指针的屏幕坐标（UI 准星用）。</summary>
        public InputAction Pointer { get; }

        public bool IsReady => Map != null;

        public void Enable()
        {
            if (Map != null)
            {
                Map.Enable();
            }
        }

        public void Disable()
        {
            if (Map != null)
            {
                Map.Disable();
            }
        }

        /// <summary>按 input.json 覆盖所有绑定的键。重复调用是安全的（覆盖是幂等的）。</summary>
        public void ApplyTuning(InputTuning tuning)
        {
            if (tuning == null)
            {
                return;
            }

            ApplyMove(tuning);
            Override(Attack, 0, InputPaths.AttackPath(tuning.attack), "Attack");
            Override(Aim, 0, InputPaths.AimPath(tuning.aim), "Aim");
            Override(Dash, 0, InputPaths.KeyPath(tuning.dash, Key.LeftShift, "input.dash"), "Dash[0]");
            Override(Dash, 1, InputPaths.KeyPath(tuning.dashSecondary, Key.Space, "input.dashSecondary"), "Dash[1]");
            Override(Melee, 0, InputPaths.KeyPath(tuning.melee, Key.F, "input.melee"), "Melee");
        }

        private void ApplyMove(InputTuning tuning)
        {
            if (Move == null)
            {
                return;
            }

            for (var i = 0; i < Move.bindings.Count; i++)
            {
                var binding = Move.bindings[i];
                if (!binding.isPartOfComposite)
                {
                    continue;
                }

                string path = null;
                switch (binding.name)
                {
                    case "up":
                        path = InputPaths.KeyPath(tuning.moveUp, Key.W, "input.moveUp");
                        break;
                    case "down":
                        path = InputPaths.KeyPath(tuning.moveDown, Key.S, "input.moveDown");
                        break;
                    case "left":
                        path = InputPaths.KeyPath(tuning.moveLeft, Key.A, "input.moveLeft");
                        break;
                    case "right":
                        path = InputPaths.KeyPath(tuning.moveRight, Key.D, "input.moveRight");
                        break;
                }

                if (path != null)
                {
                    Move.ApplyBindingOverride(i, path);
                }
            }
        }

        private static void Override(InputAction action, int index, string path, string label)
        {
            if (action == null || path == null)
            {
                return;
            }

            if (index < 0 || index >= action.bindings.Count)
            {
                Debug.LogError("[Input] " + label + " 的绑定下标 " + index + " 越界（共 " +
                    action.bindings.Count + " 条），这次覆盖被跳过。");
                return;
            }

            action.ApplyBindingOverride(index, path);
        }
    }
}
