using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System.IO;

public enum JsonType
{
    JsonUtlity,
    LitJson,
}

public class JsonManager
{
    private static JsonManager instance = new JsonManager();
    public static JsonManager Instance => instance;
    private JsonManager()
    {
    }

    /// <summary>
    /// 保存数据到json文件中
    /// </summary>
    /// <param name="data">数据对象</param>
    /// <param name="fileName">json文件名</param>
    /// <param name="jsonType"></param>
    public void SaveData(object data, string fileName, JsonType jsonType = JsonType.LitJson)
    {
        //确定路径
        string path = Application.persistentDataPath + "/" + fileName + ".json";
        Debug.Log(path);
        //序列化，得到json字符串
        string jsonStr = "";
        switch (jsonType)
        {
            case JsonType.JsonUtlity:
                jsonStr = JsonUtility.ToJson(data);
                break;
            case JsonType.LitJson:
                jsonStr = JsonMapper.ToJson(data);
                break;
        }
        //存储Json字符串到路径
        File.WriteAllText(path, jsonStr);
    }

    /// <summary>
    /// 加载对应的json文件数据到对象
    /// </summary>
    /// <typeparam name="T">加载的数据的类型</typeparam>
    /// <param name="fileName">json文件名</param>
    /// <param name="jsonType"></param>
    /// <returns></returns>
    public T LoadData<T>(string fileName, JsonType jsonType = JsonType.LitJson) where T : new()
    {
        //确定路径
        //首先判断 自创建的默认数据文件StreamingAssets下 是否有想要的数据（json文件），如果有，则获取
        string path = Application.streamingAssetsPath + "/" + fileName + ".json";
        //如果没有，则去读写文件夹中找
        if (!File.Exists(path))
            path = Application.persistentDataPath + "/" + fileName + ".json";
        //如果读写文件夹中没有，则return一个对象
        if (!File.Exists(path))
            return new T();

        string jsonStr = "";
        T dataObj = default(T);//T是值类型为0，是引用类型为null
        switch (jsonType)
        {
            case JsonType.JsonUtlity:
                jsonStr = File.ReadAllText(path);
                dataObj = JsonUtility.FromJson<T>(jsonStr);
                break;
            case JsonType.LitJson:
                jsonStr = File.ReadAllText(path);
                dataObj = JsonMapper.ToObject<T>(jsonStr);
                break;
        }

        return dataObj;
    }
}
