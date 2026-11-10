using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     着弾リングの倍率をカードの数値効果に適用する
    /// </summary>
    internal static class DartboardEffectMultiplier
    {
        /// <summary>
        ///     ダブルは2倍、トリプルは3倍し、整数で表せる範囲に収める
        /// </summary>
        internal static int Apply(int baseAmount, DartboardRing ring)
        {
            int multiplier = ring switch
            {
                DartboardRing.Double => 2,
                DartboardRing.Triple => 3,
                _ => 1
            };

            return (int)Math.Min((long)baseAmount * multiplier, int.MaxValue);
        }
    }
}
