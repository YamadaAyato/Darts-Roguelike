using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     プレイヤーのHPを回復するカード効果
    /// </summary>
    public class HealCardEffect : IBoardCardEffect
    {
        public HealCardEffect(int baseHealing)
        {
            if (baseHealing <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseHealing));
            }

            BaseHealing = baseHealing;
        }

        /// <summary> リング倍率を適用する前の回復量 </summary>
        public int BaseHealing { get; }
    }
}
