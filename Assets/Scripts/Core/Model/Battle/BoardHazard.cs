using System;
using System.Collections.Generic;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     命中時に順に発動する危険効果を保持する
    /// </summary>
    public class BoardHazard
    {
        public BoardHazard(int damage)
            : this(new IBoardHazardEffect[] { new HazardDamageEffect(damage) })
        {
        }

        public BoardHazard(IReadOnlyList<IBoardHazardEffect> effects)
        {
            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            if (effects.Count == 0)
            {
                throw new ArgumentException("危険効果を1つ以上指定してください", nameof(effects));
            }

            var copy = new List<IBoardHazardEffect>(effects.Count);
            foreach (IBoardHazardEffect effect in effects)
            {
                copy.Add(effect ?? throw new ArgumentException("未設定の危険効果があります", nameof(effects)));
            }

            Effects = copy.AsReadOnly();
        }

        /// <summary> 命中時に登録順で発動する危険効果 </summary>
        public IReadOnlyList<IBoardHazardEffect> Effects { get; }
    }
}

