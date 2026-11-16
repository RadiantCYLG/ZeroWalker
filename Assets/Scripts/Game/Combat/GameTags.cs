namespace ZeroWalker.Combat
{
    /// <summary>
    /// 玩法里用到的 Unity Tag 名集中写在这里，避免字符串散落在各处。
    /// Tag 本身必须在 ProjectSettings/TagManager 里存在；菜单
    /// 「ZeroWalker/给靶子打 Enemy 标签」会自动补上并给场景里的目标打标签。
    /// </summary>
    public static class GameTags
    {
        /// <summary>可被玩家子弹击中的目标（靶子、敌人）。</summary>
        public const string Enemy = "Enemy";

        /// <summary>可被敌人子弹击中的玩家。Unity 自带的 Tag，不需要新建。</summary>
        public const string Player = "Player";
    }
}
