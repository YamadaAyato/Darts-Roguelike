using UnityEngine;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     ダーツ1投の着弾計算結果を表す構造体
    /// </summary>
    public readonly struct DartThrowResult
    {
        public DartThrowResult(Vector2 landingPosition, DartboardHitResult hitResult)
        {
            LandingPosition = landingPosition;
            HitResult = hitResult;
        }

        /// <summary> ダーツの着弾位置を盤面の正規化座標で取得する </summary>
        public Vector2 LandingPosition { get; }

        /// <summary> ダーツの着弾位置の盤面判定結果を取得する </summary>
        public DartboardHitResult HitResult { get; }
    }
}
