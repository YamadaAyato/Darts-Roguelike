using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     プレイヤーにシールドを付与するカード効果
    /// </summary>
    public class ShieldCardEffect : IBoardCardEffect
    {
        public ShieldCardEffect(int baseShield)
        {
            if (baseShield <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseShield));
            }

            BaseShield = baseShield;
        }

        /// <summary> リング倍率を適用する前のシールド量 </summary>
        public int BaseShield { get; }
    }
}
