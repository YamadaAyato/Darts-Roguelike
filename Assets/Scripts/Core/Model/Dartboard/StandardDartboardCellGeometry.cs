using System;
using UnityEngine;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     標準盤面のマスを正規化座標の帯状頂点列に変換する
    /// </summary>
    public static class StandardDartboardCellGeometry
    {
        private const float SECTOR_ANGLE = 18f;

        /// <summary>
        ///     内周、外周の順に並ぶ頂点対を円弧に沿って作る
        /// </summary>
        /// <param name="arcSegments"> 円弧の分割数 </param>
        public static Vector2[] CreateStrip(DartboardCellId cell, int arcSegments)
        {
            if (arcSegments < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(arcSegments));
            }

            (float inner, float outer) = GetRadii(cell.Ring);
            bool isBull = cell.Ring is DartboardRing.InnerBull or DartboardRing.OuterBull;
            float startAngle = isBull ? 0f : FindSectorAngle(cell.Number) - SECTOR_ANGLE * 0.5f;
            float angleRange = isBull ? 360f : SECTOR_ANGLE;
            var points = new Vector2[(arcSegments + 1) * 2];
            for (int i = 0; i <= arcSegments; i++)
            {
                float radians = (startAngle + angleRange * i / arcSegments) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
                points[i * 2] = direction * inner;
                points[i * 2 + 1] = direction * outer;
            }

            return points;
        }

        /// <summary>
        ///     着弾判定と同じ半径でマスの内外周を求める
        /// </summary>
        private static (float inner, float outer) GetRadii(DartboardRing ring)
        {
            return ring switch
            {
                DartboardRing.InnerBull => (0f, StandardDartboardHitCalculator.INNER_BULL_RADIUS),
                DartboardRing.OuterBull => (StandardDartboardHitCalculator.INNER_BULL_RADIUS,
                    StandardDartboardHitCalculator.OUTER_BULL_RADIUS),
                DartboardRing.InnerSingle => (StandardDartboardHitCalculator.OUTER_BULL_RADIUS,
                    StandardDartboardHitCalculator.INNER_SINGLE_RADIUS),
                DartboardRing.Triple => (StandardDartboardHitCalculator.INNER_SINGLE_RADIUS,
                    StandardDartboardHitCalculator.TRIPLE_RADIUS),
                DartboardRing.OuterSingle => (StandardDartboardHitCalculator.TRIPLE_RADIUS,
                    StandardDartboardHitCalculator.OUTER_SINGLE_RADIUS),
                DartboardRing.Double => (StandardDartboardHitCalculator.OUTER_SINGLE_RADIUS,
                    StandardDartboardHitCalculator.DOUBLE_RADIUS),
                _ => throw new ArgumentException("盤外には表示領域がありません", nameof(ring))
            };
        }

        /// <summary>
        ///     番号の並びを重複定義せず着弾判定から扇形の中心角を調べる
        /// </summary>
        private static float FindSectorAngle(int number)
        {
            for (int i = 0; i < 20; i++)
            {
                float angle = i * SECTOR_ANGLE;
                float radians = angle * Mathf.Deg2Rad;
                var position = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * 0.8f;
                if (StandardDartboardHitCalculator.Calculate(position).Cell.Number == number)
                {
                    return angle;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(number));
        }
    }
}
