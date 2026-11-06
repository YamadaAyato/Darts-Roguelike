using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     戦闘中の敵と通常攻撃の威力を表す
    /// </summary>
    public class EnemyModel : BattleCharacterModel
    {
        public EnemyModel(int maxHealth, int initialShield = 0, int attackDamage = 0)
            : base(maxHealth, initialShield)
        {
            if (attackDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackDamage));
            }

            AttackDamage = attackDamage;
        }

        /// <summary> 敵ターンの通常攻撃の基礎ダメージ </summary>
        public int AttackDamage { get; }
    }
}
