namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     1体の敵に対する攻撃結果
    /// </summary>
    public readonly struct EnemyHitResult
    {
        internal EnemyHitResult(EnemyModel target, int healthLost, bool wasDefeated)
        {
            Target = target;
            HealthLost = healthLost;
            WasDefeated = wasDefeated;
        }

        /// <summary> 攻撃を受けた敵 </summary>
        public EnemyModel Target { get; }

        /// <summary> 実際に減ったHP </summary>
        public int HealthLost { get; }

        /// <summary> この攻撃で撃破されたかどうか </summary>
        public bool WasDefeated { get; }
    }
}
