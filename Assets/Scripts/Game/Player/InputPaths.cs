using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ZeroWalker.Player
{
    /// <summary>
    /// 把 input.json 里的键名（"a"、"space"、"leftButton"）翻译成 Input System 的绑定路径
    /// （"<Keyboard>/a"、"<Keyboard>/space"、"<Mouse>/leftButton"）。
    ///
    /// 为什么需要翻译：动作的定义（有哪些动作、复合键的形状）写在 PlayerControls.inputactions 里，
    /// 但「具体是哪个键」仍由 input.json 决定——运行时用 ApplyBindingOverride 把表里的键盖上去，
    /// 这样换键只改 JSON，不用开 Unity 改资产，也不会出现「资产里一套、表里一套」两个真相。
    ///
    /// 键盘优先用设备上的真实路径（Keyboard 的 key 索引器给出 "&lt;Keyboard&gt;/1" 这类规范路径），
    /// 拿不到设备时才拼枚举名兜底。
    /// </summary>
    internal static class InputPaths
    {
        private const string KeyboardPrefix = "<Keyboard>/";
        private const string MousePrefix = "<Mouse>/";

        public static string KeyPath(string binding, Key fallback, string field)
        {
            return KeyPath(ParseKey(binding, fallback, field));
        }

        public static string AttackPath(string binding)
        {
            return MousePath(binding, MouseButton.Left, "input.attack");
        }

        public static string AimPath(string binding)
        {
            return MousePath(binding, MouseButton.Right, "input.aim");
        }

        private static Key ParseKey(string binding, Key fallback, string field)
        {
            var text = binding == null ? string.Empty : binding.Trim();
            if (text.Length == 0)
            {
                Debug.LogError("[Input] " + field + " 是空字符串，退回默认键 " + fallback + "。");
                return fallback;
            }

            if (Enum.TryParse(text, true, out Key key) && key != Key.None)
            {
                return key;
            }

            Debug.LogError("[Input] 无法识别 " + field + "=\"" + binding + "\"，退回默认键 " + fallback + "。");
            return fallback;
        }

        private static string KeyPath(Key key)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // Keyboard 的按键表以 Key.Space(1) 为第 0 项：(int)key - 1 就是下标。
                var index = (int)key - 1;
                if (index >= 0 && index < keyboard.allKeys.Count)
                {
                    var path = keyboard.allKeys[index].path;
                    if (!string.IsNullOrEmpty(path))
                    {
                        return path;
                    }
                }
            }

            return KeyboardPrefix + key;
        }

        private static string MousePath(string binding, MouseButton fallback, string field)
        {
            var text = binding == null ? string.Empty : binding.Trim().ToLowerInvariant();
            switch (text)
            {
                case "leftbutton":
                case "mouseleft":
                case "left":
                    return MousePrefix + "leftButton";
                case "rightbutton":
                case "mouseright":
                case "right":
                    return MousePrefix + "rightButton";
                case "middlebutton":
                case "mousemiddle":
                case "middle":
                    return MousePrefix + "middleButton";
                default:
                    Debug.LogError("[Input] " + field + "=\"" + binding +
                        "\" 不是鼠标键（leftButton / rightButton / middleButton），退回 " + fallback + "。");
                    return MousePrefix + ButtonName(fallback);
            }
        }

        private static string ButtonName(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Right:
                    return "rightButton";
                case MouseButton.Middle:
                    return "middleButton";
                default:
                    return "leftButton";
            }
        }
    }
}
