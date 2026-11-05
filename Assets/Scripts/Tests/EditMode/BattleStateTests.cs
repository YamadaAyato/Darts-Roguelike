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
    }
}
