using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     戦闘中の敵のHPを管理する
    /// </summary>
    public class EnemyModel
    {
        public EnemyModel(int maxHealth)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHealth), maxHealth, "最大HPは1以上にしてください");
            }

            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        /// <summary> 最大HP </summary>
        public int MaxHealth { get; }

        /// <summary> 現在HP </summary>
        public int CurrentHealth { get; private set; }

        /// <summary> 生存しているかどうか </summary>
        public bool IsAlive => CurrentHealth > 0;

        /// <summary>
        ///     敵のHPにダメージを適用する
        /// </summary>
        /// <param name="damage"> HPへ適用するダメージ量 </param>
        /// <returns> オーバーキル分を除いた、実際に減ったHP </returns>
        public int TakeDamage(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damage), damage, "ダメージは0以上にしてください");
            }

            int healthLost = Math.Min(CurrentHealth, damage);
            CurrentHealth -= healthLost;
            return healthLost;
        }
    }
}
