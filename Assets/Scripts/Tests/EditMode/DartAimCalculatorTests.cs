using DartsRoguelike.Core.Model;
using NUnit.Framework;

namespace DartsRoguelike.Tests.EditMode
{
    /// <summary>
    ///     照準円の半径と周期を検証する。
    /// </summary>
    public sealed class DartAimCalculatorTests
    {
        private const float RADIUS_TOLERANCE = 0.0001f;

        /// <summary>
        ///     照準開始時は最大半径になることを検証する。
        /// </summary>
        [Test]
        public void CalculateSpreadRadius_AtStart_ReturnsMaximumRadius()
        {
            var parameters = new DartAimParameters(0.1f, 0.5f, 2f);

            float radius =
                DartAimCalculator.CalculateSpreadRadius(parameters, 0f);

            Assert.That(
                radius,
                Is.EqualTo(0.5f).Within(RADIUS_TOLERANCE));
        }

        /// <summary>
        ///     半周期で最小半径になることを検証する。
        /// </summary>
        [Test]
        public void CalculateSpreadRadius_AtHalfCycle_ReturnsMinimumRadius()
        {
            var parameters = new DartAimParameters(0.1f, 0.5f, 2f);

            float radius =
                DartAimCalculator.CalculateSpreadRadius(parameters, 1f);

            Assert.That(
                radius,
                Is.EqualTo(0.1f).Within(RADIUS_TOLERANCE));
        }

        /// <summary>
        ///     四分の一周期と四分の三周期では中間の半径になることを検証する。
        /// </summary>
        [TestCase(0.5f)]
        [TestCase(1.5f)]
        public void CalculateSpreadRadius_AtQuarterCycles_ReturnsMiddleRadius(
            float elapsedTime)
        {
            var parameters = new DartAimParameters(0.1f, 0.5f, 2f);

            float radius =
                DartAimCalculator.CalculateSpreadRadius(
                    parameters,
                    elapsedTime);

            Assert.That(
                radius,
                Is.EqualTo(0.3f).Within(RADIUS_TOLERANCE));
        }

        /// <summary>
        ///     一周期が経過すると最大半径へ戻ることを検証する。
        /// </summary>
        [Test]
        public void CalculateSpreadRadius_AtFullCycle_ReturnsMaximumRadius()
        {
            var parameters = new DartAimParameters(0.1f, 0.5f, 2f);

            float radius =
                DartAimCalculator.CalculateSpreadRadius(parameters, 2f);

            Assert.That(
                radius,
                Is.EqualTo(0.5f).Within(RADIUS_TOLERANCE));
        }

        /// <summary>
        ///     短い周期でも、複数周期後の半周期位置で最小になることを検証する。
        /// </summary>
        [Test]
        public void CalculateSpreadRadius_AfterMultipleShortCycles_ReturnsMinimumRadius()
        {
            var parameters = new DartAimParameters(0.1f, 0.5f, 0.5f);

            float radius =
                DartAimCalculator.CalculateSpreadRadius(parameters, 1.25f);

            Assert.That(
                radius,
                Is.EqualTo(0.1f).Within(RADIUS_TOLERANCE));
        }
    }
}
