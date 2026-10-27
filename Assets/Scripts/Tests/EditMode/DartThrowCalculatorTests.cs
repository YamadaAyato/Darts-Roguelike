using DartsRoguelike.Core.Model;
using NUnit.Framework;
using UnityEngine;

namespace DartsRoguelike.Tests.EditMode
{
    /// <summary>
    ///     DartThrowCalculatorの着弾計算を検証する。
    /// </summary>
    public sealed class DartThrowCalculatorTests
    {
        private const float POSITION_TOLERANCE = 0.0001f;

        /// <summary>
        ///     着弾範囲が0の場合、乱数に関係なく狙った位置へ着弾することを検証する。
        /// </summary>
        [Test]
        public void Calculate_ZeroSpread_ReturnsAimPosition()
        {
            Vector2 aimPosition = Vector2.up * 0.5f;
            var command = new DartThrowCommand(
                aimPosition,
                0f,
                0.75f,
                0.8f);

            DartThrowResult result =
                DartThrowCalculator.Calculate(command);

            float distance = Vector2.Distance(
                result.LandingPosition,
                aimPosition);

            Assert.That(
                distance,
                Is.LessThan(POSITION_TOLERANCE));
            Assert.That(
                result.HitResult.Number,
                Is.EqualTo(20));
            Assert.That(
                result.HitResult.Ring,
                Is.EqualTo(DartboardRing.InnerSingle));
        }

        /// <summary>
        ///     角度抽選値が0.25の場合、照準中心から上方向へ着弾することを検証する。
        /// </summary>
        [Test]
        public void Calculate_QuarterAngle_ReturnsUpwardOffset()
        {
            var command = new DartThrowCommand(
                Vector2.zero,
                0.4f,
                0.25f,
                1f);

            DartThrowResult result =
                DartThrowCalculator.Calculate(command);

            Vector2 expectedPosition =
                Vector2.up * 0.4f;

            float distance = Vector2.Distance(
                result.LandingPosition,
                expectedPosition);

            Assert.That(
                distance,
                Is.LessThan(POSITION_TOLERANCE));
        }

        /// <summary>
        ///     距離抽選値が0.25の場合、着弾範囲半径の半分だけずれることを検証する。
        /// </summary>
        [Test]
        public void Calculate_QuarterDistanceRate_UsesHalfRadius()
        {
            const float spreadRadius = 0.2f;

            var command = new DartThrowCommand(
                Vector2.zero,
                spreadRadius,
                0f,
                0.25f);

            DartThrowResult result =
                DartThrowCalculator.Calculate(command);

            float expectedDistance =
                spreadRadius * 0.5f;

            Assert.That(
                result.LandingPosition.magnitude,
                Is.EqualTo(expectedDistance)
                    .Within(POSITION_TOLERANCE));
        }

        /// <summary>
        ///     抽選された着弾位置が盤面を越えた場合、盤外として判定されることを検証する。
        /// </summary>
        [Test]
        public void Calculate_LandingOutsideBoard_ReturnsOutside()
        {
            var command = new DartThrowCommand(
                Vector2.right * 0.95f,
                0.1f,
                0f,
                1f);

            DartThrowResult result =
                DartThrowCalculator.Calculate(command);

            Assert.That(
                result.LandingPosition.x,
                Is.EqualTo(1.05f)
                    .Within(POSITION_TOLERANCE));
            Assert.That(
                result.HitResult.IsOutside,
                Is.True);
            Assert.That(
                result.HitResult.Score,
                Is.Zero);
        }
    }
}
