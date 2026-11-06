using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     攻撃ダメージを計算し、対象のシールドとHPに適用する
    /// </summary>
    public static class DamageResolver
    {
        /// <summary>
        ///     基礎値と着弾リングからダメージを計算し、実際に減った値を記録する
        /// </summary>
        public static EnemyHitResult Resolve(EnemyModel target, int baseDamage, DartboardRing ring)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (baseDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDamage));
            }

            if (!target.IsAlive)
            {
                return new EnemyHitResult(target, 0, 0, false);
            }

            int ringMultiplier = ring switch
            {
                DartboardRing.Double => 2,
                DartboardRing.Triple => 3,
                _ => 1
            };

            // リング倍率を掛けてもダメージが負にあふれないようにする
            int damage = (int)Math.Min((long)baseDamage * ringMultiplier, int.MaxValue);
            int shieldLost = target.AbsorbDamage(damage);
            int healthLost = target.TakeDamage(damage - shieldLost);
            return new EnemyHitResult(target, healthLost, shieldLost, !target.IsAlive);
        }
    }
}
