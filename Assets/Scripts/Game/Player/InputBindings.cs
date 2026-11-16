using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ZeroWalker.Core.Model;

namespace ZeroWalker.Player
{
    /// <summary>
    /// 把 Content/data/input.json 里的字符串绑定，解析成 Input System 的按键/鼠标键。
    /// 只负责「字符串 → 设备控制」，轮询与边沿判定在 PlayerInputReader。
    /// </summary>
    public struct InputBindings
    {
        public Key moveLeft;
        public Key moveRight;
        public Key moveUp;
        public Key moveDown;
        public Key dash;
        public Key melee;
        public Key attackKey;
        public MouseButton? attackMouseButton;

        /// <summary>按内容表解析全部绑定；解析失败的项退回默认键并报错。</summary>
        public static InputBindings FromTuning(InputTuning tuning)
        {
            var bindings = default(InputBindings);
            bindings.moveLeft = ParseKey(tuning.moveLeft, Key.A, "input.moveLeft");
            bindings.moveRight = ParseKey(tuning.moveRight, Key.D, "input.moveRight");
            bindings.moveUp = ParseKey(tuning.moveUp, Key.W, "input.moveUp");
            bindings.moveDown = ParseKey(tuning.moveDown, Key.S, "input.moveDown");
            bindings.dash = ParseKey(tuning.dash, Key.LeftShift, "input.dash");
            bindings.melee = ParseKey(tuning.melee, Key.F, "input.melee");
            bindings.ParseAttack(tuning.attack);
            return bindings;
        }

        /// <summary>键位绑定下按键是否按住。</summary>
        public static bool IsHeld(Keyboard keyboard, Key key)
        {
            return keyboard != null && key != Key.None && keyboard[key].isPressed;
        }

        /// <summary>鼠标键绑定下按键是否按住。</summary>
        public static bool IsHeld(Mouse mouse, MouseButton button)
        {
            if (mouse == null)
            {
                return false;
            }

            switch (button)
            {
                case MouseButton.Left:
                    return mouse.leftButton.isPressed;
                case MouseButton.Right:
                    return mouse.rightButton.isPressed;
                case MouseButton.Middle:
                    return mouse.middleButton.isPressed;
                default:
                    return false;
            }
        }

        /// <summary>射击键是否按住：绑到鼠标就读鼠标，绑到键盘就读键盘。</summary>
        public bool ReadAttack()
        {
            if (attackMouseButton.HasValue)
            {
                return IsHeld(Mouse.current, attackMouseButton.Value);
            }

            return IsHeld(Keyboard.current, attackKey);
        }

        private void ParseAttack(string binding)
        {
            attackMouseButton = null;
            attackKey = Key.None;

            if (string.IsNullOrEmpty(binding))
            {
                attackMouseButton = MouseButton.Left;
                return;
            }

            switch (binding.ToLowerInvariant())
            {
                case "leftbutton":
                case "mouseleft":
                    attackMouseButton = MouseButton.Left;
                    return;
                case "rightbutton":
                case "mouseright":
                    attackMouseButton = MouseButton.Right;
                    return;
                case "middlebutton":
                case "mousemiddle":
                    attackMouseButton = MouseButton.Middle;
                    return;
            }

            attackKey = ParseKey(binding, Key.None, "input.attack");
            if (attackKey == Key.None)
            {
                Debug.LogError("[Input] 无法识别 input.attack=\"" + binding + "\"，退回鼠标左键");
                attackMouseButton = MouseButton.Left;
            }
        }

        private static Key ParseKey(string binding, Key fallback, string field)
        {
            if (!string.IsNullOrEmpty(binding))
            {
                Key parsed;
                if (Enum.TryParse(binding, true, out parsed) && parsed != Key.None)
                {
                    return parsed;
                }
            }

            Debug.LogError("[Input] 无法识别 " + field + "=\"" + binding + "\"，退回默认键 " + fallback);
            return fallback;
        }
    }
}
