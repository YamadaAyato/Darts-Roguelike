using DartsRoguelike.Core.Model;
using NUnit.Framework;

namespace DartsRoguelike.Tests.EditMode
{
    /// <summary>
    ///     複数の敵がいる戦闘の生存・全滅判定を検証する
    /// </summary>
    public class BattleStateTests
    {
        /// <summary>
        ///     各行動が敵の基礎攻撃力に倍率を掛け、それぞれのダメージを切り捨てる
        /// </summary>
        [TestCase(3, 1, 3)]
        [TestCase(3, 2, 6)]
        [TestCase(5, 1.5, 7)]
        public void EnemyAttackAction_UsesBaseAttackDamage(int baseDamage, double multiplier, int expectedDamage)
        {
            var enemy = new EnemyModel(8, attackDamage: baseDamage);
            var action = new EnemyAttackAction(multiplier);

            Assert.That(enemy.BaseAttackDamage, Is.EqualTo(baseDamage));
            Assert.That(EnemyActionResolver.CalculateAttackDamage(enemy, action), Is.EqualTo(expectedDamage));
        }

        /// <summary>
        ///     通常攻撃、設置、2倍攻撃を登録順に実行する
        /// </summary>
        [Test]
        public void TryResolveEnemyTurn_NormalAndDoubleAttack_UsesIndividualActions()
        {
            var cell = new DartboardCellId(14, DartboardRing.OuterSingle);
            var player = new BattleCharacterModel(12, initialShield: 4);
            var enemy = new EnemyModel(8, attackDamage: 3, plannedActions: new IEnemyAction[]
            {
                new EnemyAttackAction(),
                new PlaceHazardEffect(cell, new BoardHazard(2)),
                new EnemyAttackAction(2)
            });
            var state = new BattleState(new BoardState(), new[] { enemy }, throwsPerTurn: 1, player: player);

            Assert.That(state.TryConsumeThrow(), Is.True);
            Assert.That(state.TryResolveEnemyTurn(), Is.True);
            Assert.That(player.CurrentHealth, Is.EqualTo(7));
            Assert.That(player.CurrentShield, Is.Zero);
            Assert.That(state.Board.GetHazard(cell), Is.Not.Null);
        }

        /// <summary>
        ///     予告した順に敵が攻撃と危険マス設置を行い、既存カードを保持する
        /// </summary>
        [Test]
        public void TryResolveEnemyTurn_PlannedPlacement_PreservesCard()
        {
            var cell = new DartboardCellId(14, DartboardRing.OuterSingle);
            var board = new BoardState();
            var card = BoardCard.CreateShield(4);
            board.SetCard(cell, card);
            var enemy = new EnemyModel(8, attackDamage: 3, plannedActions: new IEnemyAction[]
            {
                new EnemyAttackAction(),
                new PlaceHazardEffect(cell, new BoardHazard(4))
            });
            var state = new BattleState(board, new[] { enemy }, throwsPerTurn: 1);

            Assert.That(board.GetHazard(cell), Is.Null);
            Assert.That(state.TryConsumeThrow(), Is.True);
            Assert.That(state.TryResolveEnemyTurn(), Is.True);
            Assert.That(state.Player.CurrentHealth, Is.EqualTo(9));
            Assert.That(((HazardDamageEffect)board.GetHazard(cell).Effects[0]).Damage, Is.EqualTo(4));
            Assert.That(board.GetCard(cell), Is.SameAs(card));
            Assert.That(state.RemainingThrows, Is.EqualTo(1));
        }

        /// <summary>
        ///     攻撃でプレイヤーが倒れた場合は、後続の危険マス設置を実行しない
        /// </summary>
        [Test]
        public void TryResolveEnemyTurn_LethalAttack_SkipsPlacement()
        {
            var cell = new DartboardCellId(14, DartboardRing.OuterSingle);
            var enemy = new EnemyModel(8, attackDamage: 12, plannedActions: new IEnemyAction[]
            {
                new EnemyAttackAction(),
                new PlaceHazardEffect(cell, new BoardHazard(4))
            });
            var state = new BattleState(new BoardState(), new[] { enemy }, throwsPerTurn: 1);

            Assert.That(state.TryConsumeThrow(), Is.True);
            Assert.That(state.TryResolveEnemyTurn(), Is.True);
            Assert.That(state.Player.IsAlive, Is.False);
            Assert.That(state.Board.GetHazard(cell), Is.Null);
        }

        /// <summary>
        ///     撃破した敵を飛ばし、最後の敵の撃破で全滅になることを検証する
        /// </summary>
        [Test]
        public void FindFirstLivingEnemy_TwoEnemies_FollowsDisplayOrder()
        {
            var leftEnemy = new EnemyModel(5);
            var rightEnemy = new EnemyModel(5);
            var state = new BattleState(new BoardState(), new[] { leftEnemy, rightEnemy });

            Assert.That(state.FindFirstLivingEnemy(), Is.SameAs(leftEnemy));

            leftEnemy.TakeDamage(5);
            Assert.That(state.FindFirstLivingEnemy(), Is.SameAs(rightEnemy));
            Assert.That(state.AreAllEnemiesDefeated, Is.False);

            rightEnemy.TakeDamage(5);
            Assert.That(state.FindFirstLivingEnemy(), Is.Null);
            Assert.That(state.AreAllEnemiesDefeated, Is.True);
        }

        /// <summary>
        ///     設定本数を消費すると敵ターンになり、次のプレイヤーターンで補充されることを検証する
        /// </summary>
        [Test]
        public void TryResolveEnemyTurn_AfterLastThrow_RefillsConfiguredCount()
        {
            var enemy = new EnemyModel(5);
            var state = new BattleState(new BoardState(), new[] { enemy }, throwsPerTurn: 2);

            Assert.That(state.TryResolveEnemyTurn(), Is.False);
            Assert.That(state.TryConsumeThrow(), Is.True);
            Assert.That(state.RemainingThrows, Is.EqualTo(1));
            Assert.That(state.TryConsumeThrow(), Is.True);
            Assert.That(state.IsEnemyTurn, Is.True);
            Assert.That(state.TryConsumeThrow(), Is.False);

            Assert.That(state.TryResolveEnemyTurn(), Is.True);
            Assert.That(state.RemainingThrows, Is.EqualTo(2));
            Assert.That(state.IsEnemyTurn, Is.False);
        }

        /// <summary>
        ///     生存敵の攻撃がシールドから適用され、プレイヤーが倒れたら投擲を再開できないことを検証する
        /// </summary>
        [Test]
        public void TryResolveEnemyTurn_AppliesDamageAndPreventsThrowsAfterDefeat()
        {
            var player = new BattleCharacterModel(5, initialShield: 2);
            var defeatedEnemy = new EnemyModel(1, attackDamage: 10);
            var livingEnemy = new EnemyModel(5, attackDamage: 4);
            defeatedEnemy.TakeDamage(1);
            var state = new BattleState(
                new BoardState(), new[] { defeatedEnemy, livingEnemy }, throwsPerTurn: 1, player: player);

            Assert.That(defeatedEnemy.BaseAttackDamage, Is.EqualTo(10));
            Assert.That(livingEnemy.BaseAttackDamage, Is.EqualTo(4));
            Assert.That(state.TryResolveEnemyTurn(), Is.False);
            Assert.That(state.TryConsumeThrow(), Is.True);
            Assert.That(state.TryResolveEnemyTurn(), Is.True);
            Assert.That(player.CurrentShield, Is.Zero);
            Assert.That(player.CurrentHealth, Is.EqualTo(3));
            Assert.That(state.RemainingThrows, Is.EqualTo(1));

            Assert.That(state.TryConsumeThrow(), Is.True);
            Assert.That(state.TryResolveEnemyTurn(), Is.True);
            Assert.That(player.IsAlive, Is.False);
            Assert.That(state.RemainingThrows, Is.Zero);
            Assert.That(state.TryConsumeThrow(), Is.False);
            Assert.That(state.TryResolveEnemyTurn(), Is.False);
        }
    }
}
