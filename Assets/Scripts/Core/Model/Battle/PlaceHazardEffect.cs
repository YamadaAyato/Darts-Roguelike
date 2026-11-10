using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     カード効果と敵行動で共通して使う危険マス設置のデータ
    /// </summary>
    public class PlaceHazardEffect : IBoardCardEffect, IEnemyAction
    {
        public PlaceHazardEffect(DartboardCellId cell, BoardHazard hazard)
        {
            if (cell.Ring == DartboardRing.OutSide)
            {
                throw new ArgumentException("盤外に危険効果は配置できません", nameof(cell));
            }

            Cell = cell;
            Hazard = hazard ?? throw new ArgumentNullException(nameof(hazard));
        }

        /// <summary> 危険効果を配置するマス </summary>
        public DartboardCellId Cell { get; }

        /// <summary> 配置する危険効果 </summary>
        public BoardHazard Hazard { get; }
    }
}
