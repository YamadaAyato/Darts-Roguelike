#nullable enable

using System;
using System.Collections.Generic;
using DartsRoguelike.Core.Model;
using NUnit.Framework;
using UnityEngine;

namespace DartsRoguelike.Tests.EditMode
{
    /// <summary>
    ///     危険効果の抽選条件と予告から設置までの一貫性を検証する
    /// </summary>
    public class HazardPlacementResolverTests
    {
        /// <summary>
        ///     完全ランダムはブルを含む82マス、帯限定は40マスから選ぶ
        /// </summary>
        [TestCase(HazardPlacementMode.RandomBoard, 82)]
        [TestCase(HazardPlacementMode.RandomDoubleOrTriple, 40)]
        public void Select_RandomMode_UsesAllEligibleCells(HazardPlacementMode mode, int expectedCount)
        {
            var board = new BoardState();
            var request = new HazardPlacementRequest(mode, default, new BoardHazard(3));
            var random = new SequentialRandom();
            var selected = new HashSet<DartboardCellId>();

            for (int i = 0; i < expectedCount; i++)
            {
                PlaceHazardEffect? placement = HazardPlacementResolver.Select(request, board, random);
                Assert.That(placement, Is.Not.Null);
                selected.Add(placement!.Cell);
                Assert.That(placement.Cell.Ring, Is.Not.EqualTo(DartboardRing.OutSide));
                if (mode == HazardPlacementMode.RandomDoubleOrTriple)
                {
                    Assert.That(placement.Cell.Ring,
                        Is.EqualTo(DartboardRing.Double).Or.EqualTo(DartboardRing.Triple));
                }
            }

            Assert.That(selected.Count, Is.EqualTo(expectedCount));
            if (mode == HazardPlacementMode.RandomBoard)
            {
                Assert.That(selected, Does.Contain(new DartboardCellId(0, DartboardRing.InnerBull)));
                Assert.That(selected, Does.Contain(new DartboardCellId(0, DartboardRing.OuterBull)));
            }
        }

        /// <summary>
        ///     カード限定は使用済みを含むカードが存在するマスだけを候補にする
        /// </summary>
        [Test]
        public void Select_RandomCard_IncludesUsedCardsAndExcludesEmptyCells()
        {
            var board = new BoardState();
            var first = new DartboardCellId(4, DartboardRing.Double);
            var second = new DartboardCellId(20, DartboardRing.OuterSingle);
            board.SetCard(first, BoardCard.CreateShield(1));
            board.SetCard(second, BoardCard.CreateShield(2));
            var state = new BattleState(board, new[] { new EnemyModel(8) });
            ThrowEffectResolver.Resolve(
                new DartThrowResult(Vector2.zero, new DartboardHitResult(first, 8)), state);
            var request = new HazardPlacementRequest(HazardPlacementMode.RandomCard, default, new BoardHazard(3));
            var random = new SequentialRandom();

            Assert.That(HazardPlacementResolver.Select(request, board, random)!.Cell, Is.EqualTo(first));
            Assert.That(HazardPlacementResolver.Select(request, board, random)!.Cell, Is.EqualTo(second));
        }

        /// <summary>
        ///     カードがなければ設置を見送る
        /// </summary>
        [Test]
        public void Select_RandomCardWithoutCards_ReturnsNull()
        {
            var request = new HazardPlacementRequest(HazardPlacementMode.RandomCard, default, new BoardHazard(3));

            Assert.That(HazardPlacementResolver.Select(request, new BoardState(), new System.Random(1)), Is.Null);
        }

        /// <summary>
        ///     同じシードなら設置順を再現できる
        /// </summary>
        [Test]
        public void Select_SameSeed_RepeatsPlacementSequence()
        {
            var request = new HazardPlacementRequest(HazardPlacementMode.RandomBoard, default, new BoardHazard(3));
            var board = new BoardState();
            var first = new System.Random(42);
            var second = new System.Random(42);

            for (int i = 0; i < 20; i++)
            {
                Assert.That(HazardPlacementResolver.Select(request, board, first)!.Cell,
                    Is.EqualTo(HazardPlacementResolver.Select(request, board, second)!.Cell));
            }
        }

        /// <summary>
        ///     プレイヤー行動中に候補が増えても敵は予告した場所に設置する
        /// </summary>
        [Test]
        public void EnemyPlacement_KeepsPreviewTargetUntilExecution()
        {
            var board = new BoardState();
            var initialCell = new DartboardCellId(20, DartboardRing.OuterSingle);
            var addedCell = new DartboardCellId(1, DartboardRing.Triple);
            board.SetCard(initialCell, BoardCard.CreateShield(1));
            var request = new HazardPlacementRequest(HazardPlacementMode.RandomCard, default, new BoardHazard(3));
            var enemy = new EnemyModel(8, plannedActions: new IEnemyAction[] { request });
            var state = new BattleState(board, new[] { enemy }, throwsPerTurn: 1);
            var preview = (PlaceHazardEffect)enemy.PlannedActions[0];
            board.SetCard(addedCell, BoardCard.CreateShield(1));

            Assert.That(preview.Cell, Is.EqualTo(initialCell));
            Assert.That(enemy.PlannedActions[0], Is.SameAs(preview));
            state.TryConsumeThrow();
            state.TryResolveEnemyTurn();

            Assert.That(board.GetHazard(initialCell), Is.SameAs(request.Hazard));
            Assert.That(board.GetHazard(addedCell), Is.Null);
            Assert.That(enemy.PlannedActions[0], Is.Not.SameAs(preview));
        }

        /// <summary>
        ///     カード効果の抽選は戦闘開始後に配置されたカードも対象とする
        /// </summary>
        [Test]
        public void CardPlacement_SelectsFromBoardAtActivation()
        {
            var board = new BoardState();
            var state = new BattleState(board, new[] { new EnemyModel(8) });
            var cell = new DartboardCellId(20, DartboardRing.OuterSingle);
            var request = new HazardPlacementRequest(HazardPlacementMode.RandomCard, default, new BoardHazard(3));
            var card = new BoardCard(new IBoardCardEffect[] { request });
            board.SetCard(cell, card);

            ThrowEffectResolver.Resolve(
                new DartThrowResult(Vector2.up, new DartboardHitResult(cell, 20)), state);

            Assert.That(board.GetHazard(cell), Is.SameAs(request.Hazard));
            Assert.That(board.GetCard(cell), Is.SameAs(card));
            Assert.That(state.Player.CurrentHealth, Is.EqualTo(12));
        }

        /// <summary>
        ///     候補の全インデックスを順に選ぶテスト用乱数
        /// </summary>
        private class SequentialRandom : System.Random
        {
            private int _nextIndex;

            /// <summary>
            ///     抽選候補の次のインデックスを返す
            /// </summary>
            public override int Next(int maxValue) => _nextIndex++ % maxValue;
        }
    }
}
