using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     敵にダメージを与えるカード効果
    /// </summary>
    public class AttackCardEffect : IBoardCardEffect
    {
        public AttackCardEffect(int baseDamage, AttackTargetScope scope = AttackTargetScope.SingleEnemy)
        {
            if (baseDamage <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDamage));
            }

            BaseDamage = baseDamage;
            Scope = scope;
        }

        /// <summary> リング倍率を適用する前のダメージ </summary>
        public int BaseDamage { get; }

        /// <summary> 攻撃が届く範囲 </summary>
        public AttackTargetScope Scope { get; }
    }
}
