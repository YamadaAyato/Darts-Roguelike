#nullable enable

using System;
using System.Collections.Generic;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     盤面のカードと危険効果を別々に保持する
    /// </summary>
    public class BoardState
    {
        public BoardState(BoardCardDeck? cardDeck = null)
        {
            _cardDeck = cardDeck;
        }

        private readonly Dictionary<DartboardCellId, BoardCard> _cards =
            new Dictionary<DartboardCellId, BoardCard>();
        private readonly Dictionary<DartboardCellId, BoardHazard> _hazards =
            new Dictionary<DartboardCellId, BoardHazard>();
        private readonly List<DartboardCellId> _usedCards = new List<DartboardCellId>();
        private readonly BoardCardDeck? _cardDeck;

        /// <summary>
        ///     指定したマスへカードを配置し、すでにある場合は上書きする
        /// </summary>
        public void SetCard(DartboardCellId cell, BoardCard card)
        {
            if (cell.Ring == DartboardRing.OutSide)
            {
                throw new ArgumentException("盤外にカードは配置できません", nameof(cell));
            }

            _cards[cell] = card ?? throw new ArgumentNullException(nameof(card));
        }

        /// <summary>
        ///     指定したマスに配置されているカードを取得する
        /// </summary>
        /// <returns> 空マスまたは盤外ならnull </returns>
        public BoardCard? GetCard(DartboardCellId cell)
        {
            return _cards.TryGetValue(cell, out BoardCard? card) ? card : null;
        }

        /// <summary>
        ///     カードと独立して、指定したマスの危険効果を設定する
        /// </summary>
        public void SetHazard(DartboardCellId cell, BoardHazard hazard)
        {
            if (cell.Ring == DartboardRing.OutSide)
            {
                throw new ArgumentException("盤外に危険効果は配置できません", nameof(cell));
            }

            _hazards[cell] = hazard ?? throw new ArgumentNullException(nameof(hazard));
        }

        /// <summary>
        ///     指定したマスに配置された危険効果を取得する
        /// </summary>
        /// <returns> 危険効果がない場合はnull </returns>
        public BoardHazard? GetHazard(DartboardCellId cell)
        {
            return _hazards.TryGetValue(cell, out BoardHazard? hazard) ? hazard : null;
        }

        /// <summary>
        ///     指定したマスのカードが今のターンに発動済みかを確認する
        /// </summary>
        public bool IsCardUsed(DartboardCellId cell)
        {
            return _usedCards.Contains(cell);
        }

        /// <summary>
        ///     発動可能なカードを使用済みにし、そのカードを返す
        /// </summary>
        /// <returns> 空マスまたは使用済みマスならnull </returns>
        internal BoardCard? UseCard(DartboardCellId cell)
        {
            BoardCard? card = GetCard(cell);
            if (card == null || _usedCards.Contains(cell))
            {
                return null;
            }

            _usedCards.Add(cell);
            return card;
        }

        /// <summary>
        ///     使用済みマスを発動順に再抽選し、次のプレイヤーターンに備える
        /// </summary>
        internal void RefreshUsedCards()
        {
            if (_cardDeck != null)
            {
                foreach (DartboardCellId cell in _usedCards)
                {
                    BoardCard? replacement = _cardDeck.DrawReplacement(_cards[cell]);
                    if (replacement == null)
                    {
                        _cards.Remove(cell);
                    }
                    else
                    {
                        _cards[cell] = replacement;
                    }
                }
            }

            _usedCards.Clear();
        }
    }
}
