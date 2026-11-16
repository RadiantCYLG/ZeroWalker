using LitJson;
using UnityEngine;
using ZeroWalker.Core.Model;

namespace ZeroWalker.Framework.Content
{
    /// <summary>
    /// 内容数据入口：从 Resources/Content/data/*.json 读取数值，转成 Core 层的 GameContent。
    /// 只在首次访问时读取，之后复用同一份对象；改完 JSON 想立即生效可调用 Reload()。
    /// </summary>
    public static class ContentLoader
    {
        /// <summary>相对 Resources 的目录。</summary>
        private const string Root = "Content/data/";

        private static GameContent _current;

        public static GameContent Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Load();
                }

                return _current;
            }
        }

        public static void Reload()
        {
            _current = null;
        }

        /// <summary>一次性读取全部内容文件；任一段失败则沿用该段内置默认值并报错。</summary>
        private static GameContent Load()
        {
            var content = new GameContent();
            content.player = Read("player", content.player);
            content.input = Read("input", content.input);
            content.bullet = Read("bullet", content.bullet);
            content.target = Read("target", content.target);
            content.camera = Read("camera", content.camera);
            content.arena = Read("arena", content.arena);
            content.enemies = Read("enemies", content.enemies);
            content.effects = Read("effects", content.effects);
            content.animation = Read("animation", content.animation);
            content.muzzle = Read("muzzle", content.muzzle);
            content.assets = Read("assets", content.assets);

            var problems = content.Validate();
            for (var i = 0; i < problems.Count; i++)
            {
                Debug.LogError("[Content] 数值不合法：" + problems[i]);
            }

            return content;
        }

        private static T Read<T>(string fileName, T fallback) where T : class
        {
            var asset = Resources.Load<TextAsset>(Root + fileName);
            if (asset == null)
            {
                Debug.LogError("[Content] 缺少 Resources/" + Root + fileName + ".json，改用内置默认值");
                return fallback;
            }

            try
            {
                var parsed = JsonMapper.ToObject<T>(asset.text);
                if (parsed != null)
                {
                    return parsed;
                }

                Debug.LogError("[Content] " + fileName + ".json 解析结果为空，改用内置默认值");
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[Content] " + fileName + ".json 解析失败：" + exception.Message + "，改用内置默认值");
            }

            return fallback;
        }
    }
}
