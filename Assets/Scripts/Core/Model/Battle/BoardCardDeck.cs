#nullable enable

using System;
using System.Collections.Generic;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     使用済みマスの再抽選に使うカード候補と乱数を保持する
    /// </summary>
    public class BoardCardDeck
    {
        public BoardCardDeck(IReadOnlyList<BoardCard?> candidates, int seed)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            _candidates = new List<BoardCard?>(candidates);
            _random = new Random(seed);
        }

        private readonly List<BoardCard?> _candidates;
        private readonly Random _random;

        /// <summary>
        ///     使用済みカード以外から1枚抽選する。nullは通常マスを表す
        /// </summary>
        internal BoardCard? DrawReplacement(BoardCard usedCard)
        {
            var eligible = new List<BoardCard?>(_candidates.Count);
            foreach (BoardCard? card in _candidates)
            {
                if (!ReferenceEquals(card, usedCard))
                {
                    eligible.Add(card);
                }
            }

            return eligible.Count == 0 ? null : eligible[_random.Next(eligible.Count)];
        }
    }
}
