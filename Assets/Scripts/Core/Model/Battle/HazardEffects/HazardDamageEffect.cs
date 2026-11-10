using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     プレイヤーに固定ダメージを与える危険効果
    /// </summary>
    public class HazardDamageEffect : IBoardHazardEffect
    {
        public HazardDamageEffect(int damage)
        {
            if (damage <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            Damage = damage;
        }

        /// <summary> 直接命中したときのダメージ </summary>
        public int Damage { get; }
    }
}
