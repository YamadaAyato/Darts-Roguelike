using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     戦闘参加者のHPとシールドを管理する
    /// </summary>
    public class BattleCharacterModel
    {
        public BattleCharacterModel(int maxHealth, int initialShield = 0, int maxShield = int.MaxValue)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxHealth), maxHealth, "最大HPは1以上にしてください");
            }

            if (maxShield < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxShield));
            }

            if (initialShield < 0 || initialShield > maxShield)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialShield), initialShield, "初期シールドは0以上かつ上限以下にしてください");
            }

            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            CurrentShield = initialShield;
            MaxShield = maxShield;
        }

        /// <summary> 最大HP </summary>
        public int MaxHealth { get; }

        /// <summary> 現在HP </summary>
        public int CurrentHealth { get; private set; }

        /// <summary> 現在のシールド </summary>
        public int CurrentShield { get; private set; }

        /// <summary> 蓄えられるシールドの上限 </summary>
        public int MaxShield { get; }

        /// <summary> 生存しているかどうか </summary>
        public bool IsAlive => CurrentHealth > 0;

        /// <summary>
        ///     HPにダメージを適用する
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

        /// <summary>
        ///     上限までシールドを加算する
        /// </summary>
        /// <returns> 実際に増えたシールド </returns>
        internal int GainShield(int amount)
        {
            int gained = Math.Min(amount, MaxShield - CurrentShield);
            CurrentShield += gained;
            return gained;
        }

        /// <summary>
        ///     シールドでダメージを吸収する
        /// </summary>
        /// <returns> 実際に減ったシールド </returns>
        internal int AbsorbDamage(int damage)
        {
            int shieldLost = Math.Min(CurrentShield, damage);
            CurrentShield -= shieldLost;
            return shieldLost;
        }
    }
}
