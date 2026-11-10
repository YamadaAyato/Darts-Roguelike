using System;
using System.Collections.Generic;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     盤面に配置するカードの効果を発動順に保持する
    /// </summary>
    public class BoardCard
    {
        public BoardCard(int baseDamage, AttackTargetScope scope = AttackTargetScope.SingleEnemy)
            : this(new IBoardCardEffect[] { new AttackCardEffect(baseDamage, scope) })
        {
        }

        public BoardCard(IReadOnlyList<IBoardCardEffect> effects)
        {
            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            if (effects.Count == 0)
            {
                throw new ArgumentException("カード効果を1つ以上指定してください", nameof(effects));
            }

            var copy = new List<IBoardCardEffect>(effects.Count);
            foreach (IBoardCardEffect effect in effects)
            {
                copy.Add(effect ?? throw new ArgumentException("空のカード効果は指定できません", nameof(effects)));
            }

            Effects = copy.AsReadOnly();
        }

        /// <summary> カードの効果を発動順に並べた一覧 </summary>
        public IReadOnlyList<IBoardCardEffect> Effects { get; }

        /// <summary>
        ///     シールドを付与する単体効果のカードを作る
        /// </summary>
        public static BoardCard CreateShield(int baseShield)
        {
            return new BoardCard(new IBoardCardEffect[] { new ShieldCardEffect(baseShield) });
        }
    }
}
