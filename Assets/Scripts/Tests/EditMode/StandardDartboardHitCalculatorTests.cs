using DartsRoguelike.Core.Model;
using NUnit.Framework;
using UnityEngine;

namespace DartsRoguelike.Tests.EditMode
{
    /// <summary>
    ///     StandardDartboardHitCalculatorの盤面判定を検証する。
    /// </summary>
    public sealed class StandardDartboardHitCalculatorTests
    {
        private const float STANDARD_TRIPLE_INNER_RADIUS = 0.58470588f;
        private const float STANDARD_TRIPLE_OUTER_RADIUS = 0.63176471f;

        /// <summary>
        ///     中心座標がインナーブルとして判定されることを検証する。
        /// </summary>
        [Test]
        public void Calculate_CenterPosition_ReturnsInnerBull()
        {
            DartboardHitResult result = StandardDartboardHitCalculator.Calculate(Vector2.zero);

            Assert.That(result.Ring, Is.EqualTo(DartboardRing.InnerBull));
            Assert.That(result.Number, Is.Zero);
            Assert.That(result.Score, Is.EqualTo(50));
        }

        /// <summary>
        ///     アウターブル内の座標がアウターブルとして判定されることを検証する。
        /// </summary>
        [Test]
        public void Calculate_OuterBullPosition_ReturnsOuterBull()
        {
            float radius = GetMiddleRadius(
                StandardDartboardHitCalculator.INNER_BULL_RADIUS,
                StandardDartboardHitCalculator.OUTER_BULL_RADIUS);

            DartboardHitResult result = StandardDartboardHitCalculator.Calculate(Vector2.up * radius);

            Assert.That(result.Ring, Is.EqualTo(DartboardRing.OuterBull));
            Assert.That(result.Number, Is.Zero);
            Assert.That(result.Score, Is.EqualTo(25));
        }

        /// <summary>
        ///     各方角が標準ダーツ盤の数字として判定されることを検証する。
        /// </summary>
        /// <param name="clockwiseAngle">盤面上方向を0度とした時計回りの角度。</param>
        /// <param name="expectedNumber">期待する数字。</param>
        [TestCase(0f, 20)]
        [TestCase(18f, 1)]
        [TestCase(90f, 6)]
        [TestCase(180f, 3)]
        [TestCase(270f, 11)]
        public void Calculate_SectorCenter_ReturnsExpectedNumber(
            float clockwiseAngle,
            int expectedNumber)
        {
            Vector2 position = CreatePosition(0.5f, clockwiseAngle);

            DartboardHitResult result = StandardDartboardHitCalculator.Calculate(position);

            Assert.That(result.Number, Is.EqualTo(expectedNumber));
        }

        /// <summary>
        ///     20の各リングが正しい得点として判定されることを検証する。
        /// </summary>
        [Test]
        public void Calculate_NumberTwentyRings_ReturnsExpectedScores()
        {
            DartboardHitResult innerSingle = StandardDartboardHitCalculator.Calculate(Vector2.up * 0.3f);
            DartboardHitResult triple = StandardDartboardHitCalculator.Calculate(
                Vector2.up * GetMiddleRadius(
                    STANDARD_TRIPLE_INNER_RADIUS,
                    STANDARD_TRIPLE_OUTER_RADIUS));
            DartboardHitResult outerSingle = StandardDartboardHitCalculator.Calculate(Vector2.up * 0.8f);
            DartboardHitResult doubleResult = StandardDartboardHitCalculator.Calculate(
                Vector2.up * GetMiddleRadius(
                    StandardDartboardHitCalculator.OUTER_SINGLE_RADIUS,
                    StandardDartboardHitCalculator.DOUBLE_RADIUS));

            Assert.That(innerSingle.Ring, Is.EqualTo(DartboardRing.InnerSingle));
            Assert.That(innerSingle.Score, Is.EqualTo(20));
            Assert.That(triple.Ring, Is.EqualTo(DartboardRing.Triple));
            Assert.That(triple.Score, Is.EqualTo(60));
            Assert.That(outerSingle.Ring, Is.EqualTo(DartboardRing.OuterSingle));
            Assert.That(outerSingle.Score, Is.EqualTo(20));
            Assert.That(doubleResult.Ring, Is.EqualTo(DartboardRing.Double));
            Assert.That(doubleResult.Score, Is.EqualTo(40));
        }

        /// <summary>
        ///     同じ数字でも内側と外側のシングルが別のマスとして扱われることを検証する。
        /// </summary>
        [Test]
        public void Calculate_InnerAndOuterSingle_ReturnsDifferentCellIds()
        {
            DartboardHitResult innerSingle = StandardDartboardHitCalculator.Calculate(Vector2.up * 0.3f);
            DartboardHitResult outerSingle = StandardDartboardHitCalculator.Calculate(Vector2.up * 0.8f);

            Assert.That(innerSingle.Cell, Is.Not.EqualTo(outerSingle.Cell));
        }

        /// <summary>
        ///     盤面の外側が得点なしとして判定されることを検証する。
        /// </summary>
        [Test]
        public void Calculate_OutsideBoard_ReturnsOutsideAndZeroScore()
        {
            Vector2 position = Vector2.up * (StandardDartboardHitCalculator.DOUBLE_RADIUS + 0.01f);

            DartboardHitResult result = StandardDartboardHitCalculator.Calculate(position);

            Assert.That(result.Ring, Is.EqualTo(DartboardRing.OutSide));
            Assert.That(result.Score, Is.Zero);
        }

        private static float GetMiddleRadius(float innerRadius, float outerRadius)
        {
            return (innerRadius + outerRadius) * 0.5f;
        }

        private static Vector2 CreatePosition(float radius, float clockwiseAngle)
        {
            float radians = clockwiseAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * radius;
        }
    }
}
