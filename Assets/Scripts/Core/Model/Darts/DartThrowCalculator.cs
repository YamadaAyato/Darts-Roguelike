using UnityEngine;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     着弾位置とヒット結果を計算するためのダーツ投擲計算機
    /// </summary>
    public static class DartThrowCalculator
    {
        private const float FULL_ANGLE_RADIANS = Mathf.PI * 2;

        /// <summary>
        ///     ランダムな角度と距離を使用して、ダーツの着弾位置とヒット結果を計算します。
        /// </summary>
        /// <param name="command"> ダーツの投擲コマンド </param>
        /// <returns> ダーツの投擲結果 </returns>
        public static DartThrowResult Calculate(DartThrowCommand command)
        {
            // 標準の中心からのランダムな角度を計算する
            float angle = command.RandomAngleRate * FULL_ANGLE_RADIANS;

            // 標準の中心からのランダムな距離を計算する
            // 円の外側の面積が広く飛びやすくなるので、距離の平方根を取ることで均等に分布させる
            float distance = Mathf.Sqrt(command.RandomDistanceRate) * command.SpreadRadius;

            // 方向と距離を組み合わせて、最終的な着弾位置を計算する
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            Vector2 landingPosition = command.AimPosition + offset;

            // 着弾位置に基づいて、ダーツボードのヒット結果を計算する
            DartboardHitResult hitResult =
                StandardDartboardHitCalculator.Calculate(landingPosition);

            return new DartThrowResult(landingPosition, hitResult);
        }
    }
}
