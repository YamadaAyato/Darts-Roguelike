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

            Assert.That(defeatedEnemy.NextAttackDamage, Is.Zero);
            Assert.That(livingEnemy.NextAttackDamage, Is.EqualTo(4));
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
