using UnityEngine;

namespace DartsRoguelike.Core.Model
{
    public static class StandardDartboardHitCalculator
    {
        #region 定数
        #region 正規化された半径の割合の定数

        // <summary> インナーブルの外周半径割合 </summary>
        public const float INNER_BULL_RADIUS = 6.35f / BOARD_RADIUS_MM;

        // <summary> アウターブルの外周半径割合 </summary>
        public const float OUTER_BULL_RADIUS = 15.9f / BOARD_RADIUS_MM;

        // <summary> インナートリプルの外周半径割合 </summary>
        public const float INNER_SINGLE_RADIUS = 99.4f / BOARD_RADIUS_MM;

        // <summary> アウタートリプルの外周半径割合 </summary>
        public const float TRIPLE_RADIUS = 99.4f / BOARD_RADIUS_MM;

        // <summary> インナーダブルの外周半径割合 </summary>
        public const float OUTER_SINGLE_RADIUS = 162.0f / BOARD_RADIUS_MM;

        // <summary> アウターダブルの外周半径割合 </summary>
        public const float DOUBLE_RADIUS = 1f;
        #endregion
        #region ダーツボードの物理的な寸法や角度の定数

        // 実寸大の中心からダブル外周までの距離（mm）
        private const float BOARD_RADIUS_MM = 170f;

        // 1セクターの半角（度）
        private const float HALF_SECTOR_ANGLE = 9f;

        // 1セクターの角度（度）
        private const float SECTOR_ANGLE = 18f;

        private const float FULL_ANGLE = 360f;

        // ダーツボードのセクター番号（上から時計回り）
        private static readonly int[] NUMBERS_CLOCKWISE_FROM_TOP =
        {20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5};
        #endregion
        #endregion

        /// <summary>
        ///     盤面
        /// </summary>
        /// <param name="normalizedPos"> 正規化された座標 </param>
        /// <returns> ダーツボードの着弾判定結果 </returns>
        public static DartboardHitResult Calculate(Vector2 normalizedPos)
        {
            float radius = normalizedPos.magnitude;

            if (radius > DOUBLE_RADIUS)
            {
                return CreateResult(0, DartboardRing.OutSide);
            }

            if (radius <= INNER_BULL_RADIUS)
            {
                return CreateResult(0, DartboardRing.InnerBull);
            }

            if (radius <= OUTER_BULL_RADIUS)
            {
                return CreateResult(0, DartboardRing.OuterBull);
            }

            // 正規化された座標から番号とリングを計算
            int number = GetSectorNumber(normalizedPos);
            DartboardRing ring = GetboardRing(radius);

            return CreateResult(number, ring);
        }

        /// <summary>
        ///     番号とリングからスコアを計算し、DartboardHitResultを作成する
        /// </summary>
        /// <param name="number">ダーツボードの番号</param>
        /// <param name="ring">ダーツボードのリング</param>
        /// <returns> 判定結果 </returns>
        private static DartboardHitResult CreateResult(int number, DartboardRing ring)
        {
            int score;

            // スコアを計算
            switch (ring)
            {
                case DartboardRing.InnerBull:
                    score = 50;
                    break;
                case DartboardRing.OuterBull:
                    score = 25;
                    break;
                case DartboardRing.Triple:
                    score = number * 3;
                    break;
                case DartboardRing.Double:
                    score = number * 2;
                    break;
                case DartboardRing.InnerSingle:
                case DartboardRing.OuterSingle:
                    score = number;
                    break;
                default:
                    score = 0;
                    break;
            }

            var cell = new DartboardCellId(number, ring);
            return new DartboardHitResult(cell, score);
        }


        /// <summary>
        ///     着弾位置の正規化された座標から、ダーツボードの番号を計算する
        /// </summary>
        /// <param name="normalizedPos"> 正規化された座標 </param>
        /// <returns> ダーツボードの番号 </returns>
        private static int GetSectorNumber(Vector2 normalizedPos)
        {
            // 正規化された座標から角度を計算
            float clockwiseAngle =
                Mathf.Repeat(Mathf.Atan2(normalizedPos.x, normalizedPos.y) *
                Mathf.Rad2Deg, FULL_ANGLE);

            int sectorIndex =
                Mathf.FloorToInt((clockwiseAngle / HALF_SECTOR_ANGLE) / SECTOR_ANGLE) %
                NUMBERS_CLOCKWISE_FROM_TOP.Length;

            return NUMBERS_CLOCKWISE_FROM_TOP[sectorIndex];
        }

        /// <summary>
        ///     中心からの距離から、ダーツボードのリングを計算する
        /// </summary>
        /// <param name="radius"> 中心からの距離 </param>
        /// <returns> ダーツボードのリング </returns>
        private static DartboardRing GetboardRing(float radius)
        {
            if (radius < INNER_SINGLE_RADIUS)
            {
                return DartboardRing.InnerSingle;
            }
            else if (radius < TRIPLE_RADIUS)
            {
                return DartboardRing.Triple;
            }
            else if (radius < OUTER_SINGLE_RADIUS)
            {
                return DartboardRing.OuterSingle;
            }
            else
            {
                return DartboardRing.Double;
            }
        }
    }
}