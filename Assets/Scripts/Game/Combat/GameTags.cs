namespace ZeroWalker.Combat
{
    /// <summary>
    /// 玩法里用到的 Unity Tag 名集中写在这里，避免字符串散落在各处。
    /// Tag 本身必须在 ProjectSettings/TagManager 里存在；菜单
    /// 「ZeroWalker/给靶子打 Enemy 标签」会自动补上并给场景里的目标打标签。
    /// </summary>
    public static class GameTags
    {
        /// <summary>可被子弹击中的目标（靶子、将来的敌人）。</summary>
        public const string Enemy = "Enemy";
    }
}
