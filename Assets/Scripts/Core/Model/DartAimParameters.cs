using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     ダーツの標準円を計算するためのパラメータを表す構造体
    /// </summary>
    public readonly struct DartAimParameters
    {
        public DartAimParameters(float minimumSpreadRadius, float maximumSpreadRadius, float cycleDuration)
        {
            ValidateFiniteValue(minimumSpreadRadius, nameof(minimumSpreadRadius));
            ValidateFiniteValue(maximumSpreadRadius, nameof(maximumSpreadRadius));
            ValidateFiniteValue(cycleDuration, nameof(cycleDuration));

            if (minimumSpreadRadius < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumSpreadRadius),
                    minimumSpreadRadius, "" +
                    "最小拡散半径は0以上である必要があります");
            }

            if (maximumSpreadRadius < minimumSpreadRadius)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumSpreadRadius),
                    maximumSpreadRadius, "" +
                    "最大拡散半径は最小拡散半径以上である必要があります");
            }

            if (cycleDuration <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cycleDuration),
                    cycleDuration, "" +
                    "サイクル時間は0より大きい必要があります");
            }

            MinimumSpreadRadius = minimumSpreadRadius;
            MaximumSpreadRadius = maximumSpreadRadius;
            CycleDuration = cycleDuration;
        }

        /// <summary> 標準円が最も小さいときの半径を取得する </summary>
        public float MinimumSpreadRadius { get; }

        /// <summary> 標準円が最も大きいときの半径を取得する </summary>
        public float MaximumSpreadRadius { get; }

        /// <summary> 照準円が一度縮小して拡大するまでの秒数を取得する </summary>
        public float CycleDuration { get; }

        /// <summary>
        ///     設定値が有限値であることを検証する
        /// </summary>
        /// <param name="value"> 検証する値 </param>　
        /// <param name="parameterName"> パラメータ名 </param>
        private static void ValidateFiniteValue(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "有限値を指定してください。");
            }
        }
    }
}
