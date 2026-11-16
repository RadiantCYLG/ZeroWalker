namespace ZeroWalker.Combat
{
    /// <summary>
    /// 子弹阵营：决定它能打谁。
    /// 玩家子弹只认 Enemy 标签（靶子、敌人），敌人子弹只认 Player 标签，
    /// 所以同一套子弹预制体可以两边共用，不会误伤同阵营。
    /// </summary>
    public enum BulletSide
    {
        Player,
        Enemy
    }
}
