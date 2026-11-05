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
    }
}
