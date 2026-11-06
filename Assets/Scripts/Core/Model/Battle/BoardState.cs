#nullable enable

using System;
using System.Collections.Generic;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     戦闘中に盤面の各マスへ配置されているカードを保持する
    /// </summary>
    public class BoardState
    {
        private readonly Dictionary<DartboardCellId, BoardCard> _cards =
            new Dictionary<DartboardCellId, BoardCard>();
        private readonly HashSet<DartboardCellId> _usedCards = new HashSet<DartboardCellId>();

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
            return card != null && _usedCards.Add(cell) ? card : null;
        }

        /// <summary>
        ///     次のプレイヤーターン用に使用済み状態を解除する
        /// </summary>
        internal void ResetUsedCards()
        {
            _usedCards.Clear();
        }
    }
}
