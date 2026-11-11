using System.Collections.Generic;
using DartsRoguelike.Core.Model;
using NUnit.Framework;

namespace DartsRoguelike.Tests.EditMode
{
    /// <summary>
    ///     敵のターンごとの行動切り替えと予告の確定タイミングを検証する
    /// </summary>
    public class EnemyActionPatternTests
    {
        /// <summary>
        ///     通常攻撃、設置と攻撃、2倍攻撃を実行して先頭へ戻る
        /// </summary>
        [Test]
        public void ResolveEnemyTurn_ThreeTurnPattern_RepeatsInOrder()
        {
            var cell = new DartboardCellId(14, DartboardRing.OuterSingle);
            var enemy = new EnemyModel(8, new[]
            {
                new EnemyTurn(new IEnemyAction[] { new EnemyAttackAction() }),
                new EnemyTurn(new IEnemyAction[]
                {
                    new PlaceHazardEffect(cell, new BoardHazard(3)), new EnemyAttackAction()
                }),
                new EnemyTurn(new IEnemyAction[] { new EnemyAttackAction(2) })
            }, attackDamage: 3);
            var state = new BattleState(new BoardState(), new[] { enemy }, throwsPerTurn: 1,
                player: new BattleCharacterModel(100));

            Assert.That(enemy.ActionTurnIndex, Is.Zero);
            Assert.That(enemy.ActionTurnCount, Is.EqualTo(3));
            Assert.That(state.TryResolveEnemyTurn(), Is.False);
            Assert.That(enemy.ActionTurnIndex, Is.Zero);
            ResolveTurn(state);
            Assert.That(state.Player.CurrentHealth, Is.EqualTo(97));
            Assert.That(enemy.ActionTurnIndex, Is.EqualTo(1));
            Assert.That(enemy.PlannedActions[0], Is.TypeOf<PlaceHazardEffect>());
            Assert.That(state.Board.GetHazard(cell), Is.Null);

            ResolveTurn(state);
            Assert.That(state.Player.CurrentHealth, Is.EqualTo(94));
            Assert.That(state.Board.GetHazard(cell), Is.Not.Null);
            Assert.That(enemy.ActionTurnIndex, Is.EqualTo(2));
            Assert.That(((EnemyAttackAction)enemy.PlannedActions[0]).DamageMultiplier, Is.EqualTo(2));

            ResolveTurn(state);
            Assert.That(state.Player.CurrentHealth, Is.EqualTo(88));
            Assert.That(enemy.ActionTurnIndex, Is.Zero);
            ResolveTurn(state);
            Assert.That(state.Player.CurrentHealth, Is.EqualTo(85));
            Assert.That(enemy.ActionTurnIndex, Is.EqualTo(1));
        }

        /// <summary>
        ///     行動が空のターンは待機し、その次のターンへ進む
        /// </summary>
        [Test]
        public void ResolveEnemyTurn_EmptyTurn_WaitsThenAdvances()
        {
            var enemy = new EnemyModel(8, new[]
            {
                new EnemyTurn(new IEnemyAction[0]),
                new EnemyTurn(new IEnemyAction[] { new EnemyAttackAction(2) })
            }, attackDamage: 2);
            var state = new BattleState(new BoardState(), new[] { enemy }, throwsPerTurn: 1);

            Assert.That(enemy.PlannedActions, Is.Empty);
            ResolveTurn(state);
            Assert.That(state.Player.CurrentHealth, Is.EqualTo(12));
            Assert.That(enemy.ActionTurnIndex, Is.EqualTo(1));
            ResolveTurn(state);
            Assert.That(state.Player.CurrentHealth, Is.EqualTo(8));
            Assert.That(enemy.PlannedActions, Is.Empty);
        }

        /// <summary>
        ///     プレイヤー死亡後は後続の敵を実行せず、次ターンの予告も作らない
        /// </summary>
        [Test]
        public void ResolveEnemyTurn_PlayerDefeated_DoesNotAdvancePattern()
        {
            var cell = new DartboardCellId(1, DartboardRing.Triple);
            var first = new EnemyModel(8, new[]
            {
                new EnemyTurn(new IEnemyAction[] { new EnemyAttackAction() }),
                new EnemyTurn(new IEnemyAction[0])
            }, attackDamage: 12);
            var second = new EnemyModel(8, plannedActions: new IEnemyAction[]
            {
                new PlaceHazardEffect(cell, new BoardHazard(3))
            });
            var state = new BattleState(new BoardState(), new[] { first, second }, throwsPerTurn: 1);

            ResolveTurn(state);

            Assert.That(state.Player.IsAlive, Is.False);
            Assert.That(first.ActionTurnIndex, Is.Zero);
            Assert.That(state.Board.GetHazard(cell), Is.Null);
            Assert.That(state.TryResolveEnemyTurn(), Is.False);
        }

        /// <summary>
        ///     異なる長さのパターンは敵ごとに進み、撃破済みの敵は進まない
        /// </summary>
        [Test]
        public void ResolveEnemyTurn_MultipleEnemies_AdvanceIndependently()
        {
            var wait = new EnemyTurn(new IEnemyAction[0]);
            var first = new EnemyModel(8, new[] { wait, wait });
            var second = new EnemyModel(8, new[] { wait, wait, wait });
            var state = new BattleState(new BoardState(), new[] { first, second }, throwsPerTurn: 1);

            ResolveTurn(state);
            ResolveTurn(state);
            Assert.That(first.ActionTurnIndex, Is.Zero);
            Assert.That(second.ActionTurnIndex, Is.EqualTo(2));
            first.TakeDamage(8);
            ResolveTurn(state);
            ResolveTurn(state);
            Assert.That(first.ActionTurnIndex, Is.Zero);
            Assert.That(second.ActionTurnIndex, Is.EqualTo(1));
        }

        /// <summary>
        ///     ランダム設置は該当ターンの予告時に抽選し、実行までは設置先を保つ
        /// </summary>
        [Test]
        public void ResolveEnemyTurn_RandomPlacement_SelectsWhenTurnIsPreviewed()
        {
            var board = new BoardState();
            var firstCell = new DartboardCellId(14, DartboardRing.OuterSingle);
            var secondCell = new DartboardCellId(1, DartboardRing.Double);
            var request = new HazardPlacementRequest(HazardPlacementMode.RandomCard, default, new BoardHazard(3));
            var enemy = new EnemyModel(8, new[]
            {
                new EnemyTurn(new IEnemyAction[0]),
                new EnemyTurn(new IEnemyAction[] { request })
            });
            var state = new BattleState(board, new[] { enemy }, throwsPerTurn: 1);
            Assert.That(enemy.PlannedActions, Is.Empty);
            board.SetCard(firstCell, BoardCard.CreateShield(1));

            ResolveTurn(state);
            var preview = (PlaceHazardEffect)enemy.PlannedActions[0];
            Assert.That(preview.Cell, Is.EqualTo(firstCell));
            board.SetCard(secondCell, BoardCard.CreateShield(1));
            Assert.That(enemy.PlannedActions[0], Is.SameAs(preview));

            ResolveTurn(state);
            Assert.That(board.GetHazard(firstCell), Is.SameAs(request.Hazard));
            Assert.That(board.GetHazard(secondCell), Is.Null);
            Assert.That(enemy.PlannedActions, Is.Empty);
        }

        /// <summary>
        ///     設定元の一覧を書き換えても戦闘中の行動順は変わらない
        /// </summary>
        [Test]
        public void EnemyModel_CopiesPatternAndTurnActions()
        {
            var actions = new List<IEnemyAction> { new EnemyAttackAction() };
            var pattern = new List<EnemyTurn> { new EnemyTurn(actions) };
            var enemy = new EnemyModel(8, pattern, attackDamage: 3);
            var state = new BattleState(new BoardState(), new[] { enemy }, throwsPerTurn: 1);
            actions.Clear();
            pattern.Clear();

            ResolveTurn(state);

            Assert.That(state.Player.CurrentHealth, Is.EqualTo(9));
            Assert.That(enemy.ActionTurnCount, Is.EqualTo(1));
            Assert.That(enemy.PlannedActions, Has.Count.EqualTo(1));
        }

        /// <summary>
        ///     投擲を消費して敵の予告した1ターンを実行する
        /// </summary>
        private static void ResolveTurn(BattleState state)
        {
            Assert.That(state.TryConsumeThrow(), Is.True);
            Assert.That(state.TryResolveEnemyTurn(), Is.True);
        }
    }
}
