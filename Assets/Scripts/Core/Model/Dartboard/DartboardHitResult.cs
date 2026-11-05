namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     ダーツ1投の盤面判定の結果を表す構造体
    /// </summary>
    public readonly struct DartboardHitResult
    {
        public DartboardHitResult(DartboardCellId cell, int score)
        {
            Cell = cell;
            Score = score;
        }

        /// <summary> 着弾したマス </summary>
        public DartboardCellId Cell { get; }

        /// <summary> 着弾したマスの番号 </summary>
        public int Number => Cell.Number;

        /// <summary> 着弾したマスのリング </summary>
        public DartboardRing Ring => Cell.Ring;

        /// <summary> 着弾したマスのスコア </summary>
        public int Score { get; }

        /// <summary> 着弾が盤外かどうか </summary>
        public bool IsOutside => Ring == DartboardRing.OutSide;
    }
}
