using UnityEngine;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     照準開始からの経過時間に基づいて、スプレッド半径を計算するためのダーツ照準計算機
    /// </summary>
    public static class DartAimCalculator
    {
        // 円が縮小して拡大する際の、1サイクルのラジアン値
        private const float FULL_CYCLE_RADIANS = Mathf.PI * 2;

        /// <summary>
        ///     最大から最小、最小から最大へ滑らかに変化するスプレッド半径を計算します
        /// </summary>
        /// <param name="parameters"> ダーツのエイムパラメータ </param>
        /// <param name="elapsedTime"> 経過時間 </param>
        /// <returns> 計算されたスプレッド半径 </returns>
        public static float CalculateSpreadRadius(
            DartAimParameters parameters, float elapsedTime)
        {
            if (float.IsNaN(elapsedTime) || float.IsInfinity(elapsedTime) || elapsedTime < 0f)
            {
                throw new System.ArgumentException("経過時間は有効な数値でなければなりません。");
            }

            if (parameters.CycleDuration <= 0f)
            {
                throw new System.ArgumentException("スプレッド半径のサイクル時間は正の値でなければなりません。");
            }

            // 経過時間をサイクル時間で割った余りを計算し、現在のサイクル内での位置を取得する
            float cycleRate = Mathf.Repeat(elapsedTime, parameters.CycleDuration) / parameters.CycleDuration;

            // サイクル内の位置に基づいて、スプレッド半径を計算する
            float radiusRate = (Mathf.Cos(cycleRate * FULL_CYCLE_RADIANS) + 1f) * 0.5f;

            // 最小スプレッド半径と最大スプレッド半径の間で線形補間を行い、最終的なスプレッド半径を計算する
            return Mathf.Lerp(parameters.MinimumSpreadRadius, parameters.MaximumSpreadRadius, radiusRate);
        }
    }
}
