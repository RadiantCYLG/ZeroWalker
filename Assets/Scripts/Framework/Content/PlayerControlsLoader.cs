using UnityEngine;
using UnityEngine.InputSystem;

namespace ZeroWalker.Framework.Content
{
    /// <summary>
    /// 定位 PlayerControls.inputactions（放在 Resources/Content/Input 下）。
    ///
    /// 为什么放 Framework/Content：约定「资源读取只出现在 Framework/Content」，
    /// 输入动作资产也是一种内容表，路径只在这一处写，Game 层只问它要资产。
    /// 用 Resources 而不是 StreamingAssets：安卓上 StreamingAssets 得走 UnityWebRequest 异步读，
    /// 输入在 Awake 里就要用，同步加载更省事（与 Content/data 的 JSON 同一套思路）。
    /// </summary>
    public static class PlayerControlsLoader
    {
        /// <summary>相对 Resources 的路径，不带扩展名。</summary>
        public const string ResourcePath = "Content/Input/PlayerControls";

        /// <summary>加载输入动作资产；找不到时返回 null 并报错（调用方应降级为「无输入」而不是崩）。</summary>
        public static InputActionAsset Load()
        {
            var asset = Resources.Load<InputActionAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError("[Input] 找不到 Resources/" + ResourcePath + ".inputactions。" +
                    "检查这个文件是否被 Unity 导入成 InputActionAsset（.meta 里的 ScriptedImporter 指向 InputActionImporter）。");
            }

            return asset;
        }
    }
}
