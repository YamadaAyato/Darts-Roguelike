using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     設置先を選ぶ前の危険マス設置の設定
    /// </summary>
    public class HazardPlacementRequest : IBoardCardEffect, IEnemyAction
    {
        public HazardPlacementRequest(
            HazardPlacementMode mode, DartboardCellId fixedCell, BoardHazard hazard)
        {
            if (!Enum.IsDefined(typeof(HazardPlacementMode), mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            if (mode == HazardPlacementMode.Fixed && fixedCell.Ring == DartboardRing.OutSide)
            {
                throw new ArgumentException("盤外に危険効果は配置できません", nameof(fixedCell));
            }

            Mode = mode;
            FixedCell = fixedCell;
            Hazard = hazard ?? throw new ArgumentNullException(nameof(hazard));
        }

        /// <summary> 設置先の選び方 </summary>
        public HazardPlacementMode Mode { get; }

        /// <summary> 固定指定の場合の配置先 </summary>
        public DartboardCellId FixedCell { get; }

        /// <summary> 配置する危険効果 </summary>
        public BoardHazard Hazard { get; }
    }
}
