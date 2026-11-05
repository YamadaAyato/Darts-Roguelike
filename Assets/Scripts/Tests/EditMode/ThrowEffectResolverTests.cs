using DartsRoguelike.Core.Model;
using NUnit.Framework;
using UnityEngine;

namespace DartsRoguelike.Tests.EditMode
{
    /// <summary>
    ///     一投の攻撃対象と戦闘への適用結果を検証する
    /// </summary>
    public class ThrowEffectResolverTests
    {
        /// <summary>
        ///     単体攻撃が左から順に生存敵へ適用されることを検証する
        /// </summary>
        [Test]
        public void Resolve_TwoEnemies_AttacksFirstLivingEnemy()
        {
            var leftEnemy = new EnemyModel(5);
            var rightEnemy = new EnemyModel(8);
            var board = new BoardState();
            var cell = new DartboardCellId(20, DartboardRing.Double);
            board.SetCard(cell, new BoardCard(4));

            var state = new BattleState(board, new[] { leftEnemy, rightEnemy });
            var hit = new DartboardHitResult(cell, 40);
            var throwResult = new DartThrowResult(Vector2.up, hit);

            ThrowResolution first = ThrowEffectResolver.Resolve(throwResult, state);
            Assert.That(first.Target, Is.SameAs(leftEnemy));
            Assert.That(first.EnemyHealthLost, Is.EqualTo(5));
            Assert.That(first.WasEnemyDefeated, Is.True);
            Assert.That(rightEnemy.CurrentHealth, Is.EqualTo(8));
            Assert.That(state.AreAllEnemiesDefeated, Is.False);

            ThrowResolution second = ThrowEffectResolver.Resolve(throwResult, state);
            Assert.That(second.Target, Is.SameAs(rightEnemy));
            Assert.That(second.EnemyHealthLost, Is.EqualTo(8));
            Assert.That(state.AreAllEnemiesDefeated, Is.True);

            ThrowResolution afterVictory = ThrowEffectResolver.Resolve(throwResult, state);
            Assert.That(afterVictory.Target, Is.Null);
            Assert.That(afterVictory.DidAttack, Is.False);
            Assert.That(afterVictory.WasEnemyDefeated, Is.False);
        }
    }
}
