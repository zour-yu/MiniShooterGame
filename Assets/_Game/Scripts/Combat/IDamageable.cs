namespace MiniShooterGame.Combat
{
    /// <summary>
    /// Common contract for any entity (enemies, destructibles, player) that can receive damage.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applies incoming damage to this entity.
        /// </summary>
        /// <param name="damage">Amount of damage to inflict.</param>
        void TakeDamage(float damage);
    }
}
