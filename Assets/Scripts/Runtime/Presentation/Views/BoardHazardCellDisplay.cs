using DartsRoguelike.Core.Model;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     1マスの設置済み危険効果と設置予告の表示情報
    /// </summary>
    public readonly struct BoardHazardCellDisplay
    {
        public BoardHazardCellDisplay(DartboardCellId cell, bool hasHazard, bool hasPreview)
        {
            Cell = cell;
            HasHazard = hasHazard;
            HasPreview = hasPreview;
        }

        /// <summary> 表示するマス </summary>
        public DartboardCellId Cell { get; }

        /// <summary> 設置済みの危険効果があるかどうか </summary>
        public bool HasHazard { get; }

        /// <summary> 敵の設置予告があるかどうか </summary>
        public bool HasPreview { get; }
    }
}
