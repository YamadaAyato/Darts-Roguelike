using System.Collections.Generic;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     危険マスと設置予告の表示窓口
    /// </summary>
    public interface IBoardHazardView
    {
        /// <summary>
        ///     表示対象を置き換え、一覧にないマスの表示を消す
        /// </summary>
        void Render(IReadOnlyList<BoardHazardCellDisplay> cells);
    }
}
