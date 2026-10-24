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

        /// <summary> 着弾したマスを取得する </summary>
        public DartboardCellId Cell { get; }

        /// <summary> 着弾したマスの番号を取得する </summary>
        public int Number => Cell.Number;

        /// <summary> 着弾したマスのリングを取得する </summary>
        public DartboardRing Ring => Cell.Ring;

        /// <summary> 着弾したマスのスコアを取得する </summary>
        public int Score { get; }

        /// <summary> 着弾したマスが盤外かどうかを取得する </summary>
        public bool IsOutside => Ring == DartboardRing.OutSide;
    }
}
