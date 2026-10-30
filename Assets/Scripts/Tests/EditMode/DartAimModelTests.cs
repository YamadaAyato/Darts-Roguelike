using System;
using DartsRoguelike.Core.Model;
using NUnit.Framework;
using UnityEngine;

namespace DartsRoguelike.Tests.EditMode
{
    /// <summary>
    ///     照準状態の遷移と投擲コマンドの生成を検証する。
    /// </summary>
    public sealed class DartAimModelTests
    {
        private const float RADIUS_TOLERANCE = 0.0001f;

        /// <summary>
        ///     照準開始時に位置・時間・半径が設定されることを検証する。
        /// </summary>
        [Test]
        public void StartAim_SetsInitialAimState()
        {
            var parameters = new DartAimParameters(0.1f, 0.5f, 2f);
            var model = new DartAimModel(parameters);
            Vector2 aimPosition = new Vector2(0.2f, 0.4f);

            model.StartAim(aimPosition);

            Assert.That(model.IsAiming, Is.True);
            Assert.That(model.AimPosition, Is.EqualTo(aimPosition));
            Assert.That(model.ElapsedTime, Is.Zero);
            Assert.That(
                model.SpreadRadius,
                Is.EqualTo(0.5f).Within(RADIUS_TOLERANCE));
        }

        /// <summary>
        ///     時間更新が加算され、半周期で最小半径になることを検証する。
        /// </summary>
        [Test]
        public void AdvanceTime_AccumulatesTimeAndUpdatesRadius()
        {
            var parameters = new DartAimParameters(0.1f, 0.5f, 2f);
            var model = new DartAimModel(parameters);
            model.StartAim(Vector2.zero);

            model.AdvanceTime(0.25f);
            model.AdvanceTime(0.75f);

            Assert.That(
                model.ElapsedTime,
                Is.EqualTo(1f).Within(RADIUS_TOLERANCE));
            Assert.That(
                model.SpreadRadius,
                Is.EqualTo(0.1f).Within(RADIUS_TOLERANCE));
        }

        /// <summary>
        ///     確定時点の照準情報を持つコマンドを返し、二重投擲を防ぐことを検証する。
        /// </summary>
        [Test]
        public void Release_ReturnsCurrentAimAndEndsAiming()
        {
            var parameters = new DartAimParameters(0.1f, 0.5f, 2f);
            var model = new DartAimModel(parameters);
            Vector2 finalPosition = new Vector2(0.3f, 0.6f);
            model.StartAim(Vector2.zero);
            model.UpdateAimPosition(finalPosition);
            model.AdvanceTime(1f);

            DartThrowCommand command = model.Release(0.25f, 0.75f);

            Assert.That(model.IsAiming, Is.False);
            Assert.That(command.AimPosition, Is.EqualTo(finalPosition));
            Assert.That(
                command.SpreadRadius,
                Is.EqualTo(0.1f).Within(RADIUS_TOLERANCE));
            Assert.That(command.RandomAngleRate, Is.EqualTo(0.25f));
            Assert.That(command.RandomDistanceRate, Is.EqualTo(0.75f));
            Assert.Throws<InvalidOperationException>(
                () => model.Release(0.25f, 0.75f));
        }

        /// <summary>
        ///     キャンセルで初期化され、再び照準を開始できることを検証する。
        /// </summary>
        [Test]
        public void CancelAim_ResetsStateAndAllowsRestart()
        {
            var parameters = new DartAimParameters(0.1f, 0.5f, 2f);
            var model = new DartAimModel(parameters);
            model.StartAim(Vector2.up);
            model.AdvanceTime(1f);

            model.CancelAim();

            Assert.That(model.IsAiming, Is.False);
            Assert.That(model.AimPosition, Is.EqualTo(Vector2.zero));
            Assert.That(model.ElapsedTime, Is.Zero);
            Assert.That(
                model.SpreadRadius,
                Is.EqualTo(0.5f).Within(RADIUS_TOLERANCE));

            model.StartAim(Vector2.right);

            Assert.That(model.IsAiming, Is.True);
            Assert.That(model.AimPosition, Is.EqualTo(Vector2.right));
        }

        /// <summary>
        ///     照準開始前の操作が拒否されることを検証する。
        /// </summary>
        [Test]
        public void BeforeStart_RejectsAimOperations()
        {
            var parameters = new DartAimParameters(0.1f, 0.5f, 2f);
            var model = new DartAimModel(parameters);

            Assert.Throws<InvalidOperationException>(
                () => model.UpdateAimPosition(Vector2.up));
            Assert.Throws<InvalidOperationException>(
                () => model.AdvanceTime(0.1f));
            Assert.Throws<InvalidOperationException>(
                () => model.Release(0.5f, 0.5f));
        }

        /// <summary>
        ///     二重に照準を開始しても、それまでの状態が失われないことを検証する。
        /// </summary>
        [Test]
        public void StartAim_WhileAiming_PreservesExistingState()
        {
            var parameters = new DartAimParameters(0.1f, 0.5f, 2f);
            var model = new DartAimModel(parameters);
            model.StartAim(Vector2.up);
            model.AdvanceTime(1f);

            Assert.Throws<InvalidOperationException>(
                () => model.StartAim(Vector2.right));

            Assert.That(model.AimPosition, Is.EqualTo(Vector2.up));
            Assert.That(model.ElapsedTime, Is.EqualTo(1f));
            Assert.That(model.IsAiming, Is.True);
        }

        /// <summary>
        ///     不正な抽選値では照準を維持し、正しい値で投擲できることを検証する。
        /// </summary>
        [Test]
        public void Release_InvalidRandomRate_KeepsAiming()
        {
            var parameters = new DartAimParameters(0.1f, 0.5f, 2f);
            var model = new DartAimModel(parameters);
            model.StartAim(Vector2.up);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => model.Release(-0.1f, 0.5f));

            Assert.That(model.IsAiming, Is.True);

            DartThrowCommand command = model.Release(0.5f, 0.5f);

            Assert.That(command.AimPosition, Is.EqualTo(Vector2.up));
            Assert.That(model.IsAiming, Is.False);
        }
    }
}
