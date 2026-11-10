using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     プレイヤーを攻撃する敵の行動
    /// </summary>
    public class EnemyAttackAction : IEnemyAction
    {
        public EnemyAttackAction(double damageMultiplier = 1)
        {
            if (double.IsNaN(damageMultiplier) || double.IsInfinity(damageMultiplier) || damageMultiplier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damageMultiplier));
            }

            DamageMultiplier = damageMultiplier;
        }

        /// <summary> 敵の基礎攻撃力に掛ける倍率 </summary>
        public double DamageMultiplier { get; }
    }
}
