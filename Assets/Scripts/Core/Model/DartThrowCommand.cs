using System;
using UnityEngine;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     ダーツ1投の投擲情報を表す構造体
    /// </summary>
    public readonly struct DartThrowCommand
    {
        public DartThrowCommand(
            Vector2 aimPosition,
            float spreadRadius,
            float randomAngleRate,
            float randomDistanceRate)
        {
            if (spreadRadius < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(spreadRadius), spreadRadius, "着弾半径の範囲にはは0以上である必要があります。");
            }

            ValidateRate(randomAngleRate, nameof(randomAngleRate));
            ValidateRate(randomDistanceRate, nameof(randomDistanceRate));

            AimPosition = aimPosition;
            SpreadRadius = spreadRadius;
            RandomAngleRate = randomAngleRate;
            RandomDistanceRate = randomDistanceRate;
        }

        /// <summary> 狙った位置の盤面の正規化座標で取得する </summary>
        public Vector2 AimPosition { get; }

        /// <summary> 着弾半径を盤面の正規化座標で取得する </summary>
        public float SpreadRadius { get; }

        /// <summary> 着弾角度の割合を取得する </summary>
        public float RandomAngleRate { get; }

        /// <summary> 中心からの着弾距離の割合を取得する </summary>
        public float RandomDistanceRate { get; }

        /// <summary>
        ///     抽選値の検証をする。
        /// </summary>
        /// <param name="rate"> 検証する抽選値 </param>
        /// <param name="parameterName"> 検証するパラメータ名 </param>
        private static void ValidateRate(float rate, string parameterName)
        {
            if (float.IsNaN(rate) || float.IsInfinity(rate) ||
                rate < 0f || rate > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName, rate, $"{parameterName}は0以上1以下である必要があります。");
            }
        }
    }
}
