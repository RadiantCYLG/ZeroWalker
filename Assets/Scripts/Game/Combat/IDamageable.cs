namespace ZeroWalker.Combat
{
    /// <summary>
    /// 能被子弹打掉血的东西（靶子、敌人、将来的可破坏物）。
    /// 子弹只认这个接口，不认具体组件，所以新增可击中目标只要实现它。
    /// </summary>
    public interface IDamageable
    {
        /// <summary>扣血；amount 为伤害点数。</summary>
        void TakeDamage(int amount);
    }
}
