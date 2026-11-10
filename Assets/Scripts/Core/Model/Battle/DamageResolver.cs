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
            if (baseDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDamage));
            }

            int damage = DartboardEffectMultiplier.Apply(baseDamage, ring);
            (int healthLost, int shieldLost, bool wasDefeated) =
                Resolve(target, damage);
            return new EnemyHitResult(target, healthLost, shieldLost, wasDefeated);
        }

        /// <summary>
        ///     プレイヤーと敵に共通の手順でシールド、HPの順にダメージを適用する
        /// </summary>
        internal static (int HealthLost, int ShieldLost, bool WasDefeated) Resolve(
            BattleCharacterModel target, int baseDamage)
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
                return (0, 0, false);
            }

            int shieldLost = target.AbsorbDamage(baseDamage);
            int healthLost = target.TakeDamage(baseDamage - shieldLost);
            return (healthLost, shieldLost, !target.IsAlive);
        }
    }
}
