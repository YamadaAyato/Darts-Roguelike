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

        /// <summary> ダーツの着弾位置の正規化座標 </summary>
        public Vector2 LandingPosition { get; }

        /// <summary> 着弾位置の盤面判定結果 </summary>
        public DartboardHitResult HitResult { get; }
    }
}
