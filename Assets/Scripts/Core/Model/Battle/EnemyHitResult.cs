namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     1体の敵に対する攻撃結果
    /// </summary>
    public readonly struct EnemyHitResult
    {
        internal EnemyHitResult(EnemyModel target, int healthLost, int shieldLost, bool wasDefeated)
        {
            Target = target;
            HealthLost = healthLost;
            ShieldLost = shieldLost;
            WasDefeated = wasDefeated;
        }

        /// <summary> 攻撃を受けた敵 </summary>
        public EnemyModel Target { get; }

        /// <summary> 実際に減ったHP </summary>
        public int HealthLost { get; }

        /// <summary> 実際に減ったシールド </summary>
        public int ShieldLost { get; }

        /// <summary> 実際に減ったシールドとHPの合計 </summary>
        public int EffectiveDamage => ShieldLost + HealthLost;

        /// <summary> この攻撃で撃破されたかどうか </summary>
        public bool WasDefeated { get; }
    }
}
