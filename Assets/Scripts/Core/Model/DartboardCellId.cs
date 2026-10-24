using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     ダーツ盤上のマスを一意に識別するための構造体
    /// </summary>
    public readonly struct DartboardCellId : IEquatable<DartboardCellId>
    {
        public DartboardCellId(int number, DartboardRing ring)
        {
            if (!IsValid(number, ring))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number), $"{number}, {ring}は無効なマスです。");
            }

            Number = number;
            Ring = ring;
        }

        /// <summary> 1から20までの番号を表す整数値。ブルと盤外では 0 を返す </summary>
        public int Number { get; }

        /// <summary> リングの種類を取得する </summary>
        public DartboardRing Ring { get; }

        public bool Equals(DartboardCellId other)
        {
            return Number == other.Number && Ring == other.Ring;
        }

        public override bool Equals(object? obj)
        {
            return obj is DartboardCellId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Number, Ring);
        }

        /// <summary>
        ///     番号とリングの組み合わせが有効なマスかどうかを判定する
        /// </summary>
        /// <param name="number"> マスの番号 </param>
        /// <param name="ring"> マスのリングの種類 </param>
        /// <returns> 有効なマスであれば true、そうでなければ false </returns>
        private static bool IsValid(int number, DartboardRing ring)
        {
            bool IsCenterOrOutside = ring is
                DartboardRing.InnerBull or
                DartboardRing.OuterBull or
                DartboardRing.OutSide;

            return IsCenterOrOutside ? number == 0 : number is >= 1 and <= 20;
        }
    }
}
