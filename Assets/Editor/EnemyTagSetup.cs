using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ZeroWalker.Combat;

namespace ZeroWalker.EditorTools
{
    /// <summary>
    /// 一次性场景配置：把 "Enemy" 这个 Tag 建出来，并给场景里所有可被击中的目标打上。
    /// 子弹靠这个 Tag 判断「该不该被打掉」，标签比「碰撞体是不是触发器」更明确，
    /// 也不会被相机边界、触发区这类技术性碰撞体误伤。
    /// 幂等：Tag 已存在就不重复加，目标已带标签就跳过。
    /// </summary>
    public static class EnemyTagSetup
    {
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";

        [MenuItem("ZeroWalker/给靶子打 Enemy 标签", priority = 20)]
        public static void Apply()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[EnemyTag] 播放模式下不能改场景标签，请先停止播放。");
                return;
            }

            var created = EnsureTag(GameTags.Enemy);

            var targets = Object.FindObjectsOfType<ShootableTarget>(true);
            var tagged = 0;
            foreach (var target in targets)
            {
                if (target.gameObject.tag == GameTags.Enemy) continue;

                target.gameObject.tag = GameTags.Enemy;
                tagged++;
            }

            if (tagged > 0 && targets.Length > 0)
            {
                EditorSceneManager.MarkSceneDirty(targets[0].gameObject.scene);
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log("[EnemyTag] Tag" + (created ? " 已新建" : " 已存在")
                + "；本次新打标签 " + tagged + " 个，场景里共有 " + targets.Length + " 个目标。");
        }

        /// <summary>确保 TagManager 里有这个标签，返回是否新建。</summary>
        private static bool EnsureTag(string tag)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[EnemyTag] 找不到 " + TagManagerPath + "，请手动在 Tag Manager 里添加 " + tag);
                return false;
            }

            var tagManager = new SerializedObject(assets[0]);
            var tags = tagManager.FindProperty("tags");

            for (var i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return false;
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
