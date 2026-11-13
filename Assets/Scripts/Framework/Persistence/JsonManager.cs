using System.IO;
using LitJson;
using UnityEngine;

namespace ZeroWalker.Framework.Persistence
{
    public enum JsonType
    {
        JsonUtlity,
        LitJson,
    }

    /// <summary>
    /// 存档读写的统一入口：读走 StreamingAssets（随包分发的默认档）→ persistentDataPath（玩家实际存档），
    /// 写只落 persistentDataPath。数据格式支持 JsonUtility 与 LitJson 两种。
    /// </summary>
    public class JsonManager
    {
        private static readonly JsonManager CachedInstance = new JsonManager();

        public static JsonManager Instance
        {
            get { return CachedInstance; }
        }

        private JsonManager()
        {
        }

        /// <summary>把数据对象序列化后写入 persistentDataPath/&lt;fileName&gt;.json。</summary>
        public void SaveData(object data, string fileName, JsonType jsonType = JsonType.LitJson)
        {
            var path = Application.persistentDataPath + "/" + fileName + ".json";
            var jsonStr = string.Empty;
            switch (jsonType)
            {
                case JsonType.JsonUtlity:
                    jsonStr = JsonUtility.ToJson(data);
                    break;
                case JsonType.LitJson:
                    jsonStr = JsonMapper.ToJson(data);
                    break;
            }

            File.WriteAllText(path, jsonStr);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[JsonManager] 已保存存档：" + path);
#endif
        }

        /// <summary>
        /// 读取存档：优先 StreamingAssets 下的默认档，其次 persistentDataPath 下的玩家存档；
        /// 两处都没有时返回一个默认构造的新对象。
        /// </summary>
        public T LoadData<T>(string fileName, JsonType jsonType = JsonType.LitJson) where T : new()
        {
            var path = Application.streamingAssetsPath + "/" + fileName + ".json";
            if (!File.Exists(path))
            {
                path = Application.persistentDataPath + "/" + fileName + ".json";
            }

            if (!File.Exists(path))
            {
                return new T();
            }

            var jsonStr = File.ReadAllText(path);
            T dataObj = default(T);
            switch (jsonType)
            {
                case JsonType.JsonUtlity:
                    dataObj = JsonUtility.FromJson<T>(jsonStr);
                    break;
                case JsonType.LitJson:
                    dataObj = JsonMapper.ToObject<T>(jsonStr);
                    break;
            }

            return dataObj;
        }
    }
}
