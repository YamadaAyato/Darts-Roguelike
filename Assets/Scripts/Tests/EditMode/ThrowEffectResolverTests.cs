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
        ///     リング倍率、最大HPによる上限、同じマスの再発動防止を検証する
        /// </summary>
        [TestCase(DartboardRing.OuterSingle, 4, 4)]
        [TestCase(DartboardRing.Double, 8, 8)]
        [TestCase(DartboardRing.Triple, 11, 11)]
        [TestCase(DartboardRing.Triple, 3, 3)]
        [TestCase(DartboardRing.OuterSingle, 0, 0)]
        public void Resolve_HealCard_ReportsActualRecovery(DartboardRing ring, int missingHealth, int expectedRecovery)
        {
            var player = new BattleCharacterModel(12, initialShield: 2);
            player.TakeDamage(missingHealth);
            var board = new BoardState();
            var cell = new DartboardCellId(14, ring);
            board.SetCard(cell, new BoardCard(new IBoardCardEffect[] { new HealCardEffect(4) }));
            var state = new BattleState(board, new[] { new EnemyModel(20) }, player: player);
            var hit = new DartThrowResult(Vector2.left, new DartboardHitResult(cell, 14));

            ThrowResolution resolution = ThrowEffectResolver.Resolve(hit, state);

            Assert.That(resolution.HealthRecovered, Is.EqualTo(expectedRecovery));
            Assert.That(resolution.DidAttack, Is.False);
            Assert.That(player.CurrentHealth, Is.EqualTo(12 - missingHealth + expectedRecovery));
            Assert.That(player.CurrentShield, Is.EqualTo(2));
            Assert.That(board.IsCardUsed(cell), Is.True);
            Assert.That(ThrowEffectResolver.Resolve(hit, state).HealthRecovered, Is.Zero);
        }

        /// <summary>
        ///     複数効果を登録順に解決し、カード全体を一度だけ使うことを検証する
        /// </summary>
        [Test]
        public void Resolve_CompositeCard_AppliesEffectsInOrderOnce()
        {
            var left = new EnemyModel(3);
            var right = new EnemyModel(10);
            var board = new BoardState();
            var cell = new DartboardCellId(11, DartboardRing.Double);
            board.SetCard(cell, new BoardCard(new IBoardCardEffect[]
            {
                new AttackCardEffect(2),
                new ShieldCardEffect(2),
                new AttackCardEffect(1, AttackTargetScope.AllEnemies)
            }));
            var state = new BattleState(board, new[] { left, right });
            var result = new DartThrowResult(Vector2.left, new DartboardHitResult(cell, 22));

            ThrowResolution resolution = ThrowEffectResolver.Resolve(result, state, preferredEnemyIndex: 0);

            Assert.That(resolution.EnemyHits, Has.Count.EqualTo(2));
            Assert.That(resolution.EnemyHits[0].Target, Is.SameAs(left));
            Assert.That(resolution.EnemyHits[0].WasDefeated, Is.True);
            Assert.That(resolution.EnemyHits[1].Target, Is.SameAs(right));
            Assert.That(right.CurrentHealth, Is.EqualTo(8));
            Assert.That(resolution.ShieldGained, Is.EqualTo(4));
            Assert.That(state.Player.CurrentShield, Is.EqualTo(4));
            Assert.That(board.IsCardUsed(cell), Is.True);

            ThrowResolution repeated = ThrowEffectResolver.Resolve(result, state);
            Assert.That(repeated.EnemyHits, Is.Empty);
            Assert.That(repeated.ShieldGained, Is.Zero);
        }

        /// <summary>
        ///     防御カードのリング倍率と、敵ターン後のHP・シールド残量を検証する
        /// </summary>
        [TestCase(DartboardRing.OuterSingle, 4, 9, 0)]
        [TestCase(DartboardRing.Double, 8, 12, 1)]
        [TestCase(DartboardRing.Triple, 10, 12, 3)]
        public void Resolve_ShieldCard_AppliesRingMultiplierAndKeepsRemainingShield(
            DartboardRing ring, int expectedGained, int expectedHealth, int expectedShield)
        {
            var board = new BoardState();
            var cell = new DartboardCellId(11, ring);
            board.SetCard(cell, BoardCard.CreateShield(4));
            var player = new BattleCharacterModel(12, maxShield: 10);
            var enemy = new EnemyModel(20, attackDamage: 7);
            var state = new BattleState(board, new[] { enemy }, throwsPerTurn: 1, player: player);
            var result = new DartThrowResult(Vector2.left, new DartboardHitResult(cell, 11));

            Assert.That(state.TryConsumeThrow(), Is.True);
            ThrowResolution resolution = ThrowEffectResolver.Resolve(result, state);
            Assert.That(resolution.ShieldGained, Is.EqualTo(expectedGained));
            Assert.That(resolution.DidAttack, Is.False);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(20));
            Assert.That(board.IsCardUsed(cell), Is.True);
            Assert.That(ThrowEffectResolver.Resolve(result, state).ShieldGained, Is.Zero);
            Assert.That(state.TryResolveEnemyTurn(), Is.True);
            Assert.That(player.CurrentHealth, Is.EqualTo(expectedHealth));
            Assert.That(player.CurrentShield, Is.EqualTo(expectedShield));
        }

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
            var secondCell = new DartboardCellId(6, DartboardRing.Double);
            board.SetCard(cell, new BoardCard(4));
            board.SetCard(secondCell, new BoardCard(4));

            var state = new BattleState(board, new[] { leftEnemy, rightEnemy });
            var hit = new DartboardHitResult(cell, 40);
            var throwResult = new DartThrowResult(Vector2.up, hit);

            ThrowResolution first = ThrowEffectResolver.Resolve(throwResult, state);
            Assert.That(first.EnemyHits, Has.Count.EqualTo(1));
            Assert.That(first.EnemyHits[0].Target, Is.SameAs(leftEnemy));
            Assert.That(first.EnemyHits[0].HealthLost, Is.EqualTo(5));
            Assert.That(first.EnemyHits[0].WasDefeated, Is.True);
            Assert.That(rightEnemy.CurrentHealth, Is.EqualTo(8));
            Assert.That(state.AreAllEnemiesDefeated, Is.False);

            var secondThrow = new DartThrowResult(Vector2.right, new DartboardHitResult(secondCell, 12));
            ThrowResolution second = ThrowEffectResolver.Resolve(secondThrow, state);
            Assert.That(second.EnemyHits, Has.Count.EqualTo(1));
            Assert.That(second.EnemyHits[0].Target, Is.SameAs(rightEnemy));
            Assert.That(second.EnemyHits[0].HealthLost, Is.EqualTo(8));
            Assert.That(state.AreAllEnemiesDefeated, Is.True);

            ThrowResolution afterVictory = ThrowEffectResolver.Resolve(throwResult, state);
            Assert.That(afterVictory.EnemyHits, Is.Empty);
            Assert.That(afterVictory.DidAttack, Is.False);
        }

        /// <summary>
        ///     同じカードマスの再発動は次のプレイヤーターンまで待つことを検証する
        /// </summary>
        [Test]
        public void Resolve_UsedCard_ActivatesAgainOnlyOnNextPlayerTurn()
        {
            var enemy = new EnemyModel(20);
            var board = new BoardState();
            var cell = new DartboardCellId(20, DartboardRing.OuterSingle);
            board.SetCard(cell, new BoardCard(4));
            var state = new BattleState(board, new[] { enemy }, throwsPerTurn: 2);
            var hit = new DartboardHitResult(cell, 20);
            var throwResult = new DartThrowResult(Vector2.up, hit);

            Assert.That(state.TryConsumeThrow(), Is.True);
            Assert.That(ThrowEffectResolver.Resolve(throwResult, state).DidAttack, Is.True);
            Assert.That(board.IsCardUsed(cell), Is.True);

            Assert.That(state.TryConsumeThrow(), Is.True);
            Assert.That(ThrowEffectResolver.Resolve(throwResult, state).DidAttack, Is.False);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(16));
            Assert.That(board.IsCardUsed(cell), Is.True);

            Assert.That(state.TryResolveEnemyTurn(), Is.True);
            Assert.That(board.IsCardUsed(cell), Is.False);
            Assert.That(state.TryConsumeThrow(), Is.True);
            Assert.That(ThrowEffectResolver.Resolve(throwResult, state).DidAttack, Is.True);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(12));
        }

        /// <summary>
        ///     次のターン開始時に使用済みマスだけが別のカードまたは通常マスになることを検証する
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public void Resolve_UsedCard_RerollsOnlyTheUsedCell(bool drawCard)
        {
            var usedCard = new BoardCard(4);
            BoardCard replacement = drawCard ? new BoardCard(2) : null;
            var deck = new BoardCardDeck(new[] { usedCard, replacement }, seed: 1);
            var board = new BoardState(deck);
            var usedCell = new DartboardCellId(20, DartboardRing.OuterSingle);
            var unusedCell = new DartboardCellId(6, DartboardRing.OuterSingle);
            board.SetCard(usedCell, usedCard);
            board.SetCard(unusedCell, usedCard);
            var state = new BattleState(board, new[] { new EnemyModel(20) }, throwsPerTurn: 1);
            var hit = new DartboardHitResult(usedCell, 20);
            var throwResult = new DartThrowResult(Vector2.up, hit);

            Assert.That(state.TryConsumeThrow(), Is.True);
            Assert.That(ThrowEffectResolver.Resolve(throwResult, state).DidAttack, Is.True);
            Assert.That(board.GetCard(usedCell), Is.SameAs(usedCard));
            Assert.That(state.TryResolveEnemyTurn(), Is.True);
            Assert.That(board.IsCardUsed(usedCell), Is.False);
            Assert.That(board.GetCard(usedCell), Is.EqualTo(replacement));
            Assert.That(board.GetCard(unusedCell), Is.SameAs(usedCard));
        }

        /// <summary>
        ///     指定先が生存中なら優先し、撃破済みや範囲外なら左端の生存敵を狙うことを検証する
        /// </summary>
        [TestCase(1, true, 1)]
        [TestCase(1, false, 0)]
        [TestCase(-1, true, 0)]
        [TestCase(2, true, 0)]
        public void Resolve_PreferredEnemyIndex_AttacksExpectedEnemy(
            int preferredEnemyIndex, bool isRightEnemyAlive, int expectedEnemyIndex)
        {
            var leftEnemy = new EnemyModel(10);
            var rightEnemy = new EnemyModel(10);
            if (!isRightEnemyAlive)
            {
                rightEnemy.TakeDamage(10);
            }

            var board = new BoardState();
            var cell = new DartboardCellId(20, DartboardRing.Double);
            board.SetCard(cell, new BoardCard(2));

            var state = new BattleState(board, new[] { leftEnemy, rightEnemy });
            var hit = new DartboardHitResult(cell, 40);
            var throwResult = new DartThrowResult(Vector2.up, hit);

            ThrowResolution result = ThrowEffectResolver.Resolve(throwResult, state, preferredEnemyIndex);

            Assert.That(result.EnemyHits, Has.Count.EqualTo(1));
            Assert.That(result.EnemyHits[0].Target, Is.SameAs(state.Enemies[expectedEnemyIndex]));
            Assert.That(result.EnemyHits[0].HealthLost, Is.EqualTo(4));
            Assert.That(leftEnemy.CurrentHealth, Is.EqualTo(expectedEnemyIndex == 0 ? 6 : 10));
        }

        /// <summary>
        ///     全体攻撃が優先指定にかかわらず生存敵に当たり、敵ごとの結果を返すことを検証する
        /// </summary>
        [Test]
        public void Resolve_AllEnemies_AttacksEveryLivingEnemy()
        {
            var leftEnemy = new EnemyModel(3);
            var defeatedEnemy = new EnemyModel(1);
            var rightEnemy = new EnemyModel(10);
            defeatedEnemy.TakeDamage(1);

            var board = new BoardState();
            var cell = new DartboardCellId(20, DartboardRing.Double);
            board.SetCard(cell, new BoardCard(2, AttackTargetScope.AllEnemies));

            var state = new BattleState(board, new[] { leftEnemy, defeatedEnemy, rightEnemy });
            var hit = new DartboardHitResult(cell, 40);
            var throwResult = new DartThrowResult(Vector2.up, hit);

            ThrowResolution result = ThrowEffectResolver.Resolve(throwResult, state, 2);

            Assert.That(result.DidAttack, Is.True);
            Assert.That(result.EnemyHits, Has.Count.EqualTo(2));
            Assert.That(result.EnemyHits[0].Target, Is.SameAs(leftEnemy));
            Assert.That(result.EnemyHits[0].HealthLost, Is.EqualTo(3));
            Assert.That(result.EnemyHits[0].WasDefeated, Is.True);
            Assert.That(result.EnemyHits[1].Target, Is.SameAs(rightEnemy));
            Assert.That(result.EnemyHits[1].HealthLost, Is.EqualTo(4));
            Assert.That(result.EnemyHits[1].WasDefeated, Is.False);
            Assert.That(rightEnemy.CurrentHealth, Is.EqualTo(6));
            Assert.That(state.AreAllEnemiesDefeated, Is.False);
        }

        /// <summary>
        ///     シールドを先に減らし、オーバーキル分を除いた有効ダメージを記録することを検証する
        /// </summary>
        [TestCase(5, 0, 4, false)]
        [TestCase(1, 2, 1, true)]
        public void Resolve_ShieldedEnemy_RecordsEffectiveDamage(
            int initialShield, int expectedHealthLost, int expectedShieldLost, bool expectedDefeat)
        {
            var enemy = new EnemyModel(2, initialShield);
            var board = new BoardState();
            var cell = new DartboardCellId(20, DartboardRing.Double);
            board.SetCard(cell, new BoardCard(2));

            var state = new BattleState(board, new[] { enemy });
            var hit = new DartboardHitResult(cell, 40);
            var throwResult = new DartThrowResult(Vector2.up, hit);

            ThrowResolution result = ThrowEffectResolver.Resolve(throwResult, state);

            Assert.That(result.DidAttack, Is.True);
            Assert.That(result.EnemyHits, Has.Count.EqualTo(1));
            EnemyHitResult enemyHit = result.EnemyHits[0];
            Assert.That(enemyHit.HealthLost, Is.EqualTo(expectedHealthLost));
            Assert.That(enemyHit.ShieldLost, Is.EqualTo(expectedShieldLost));
            Assert.That(enemyHit.EffectiveDamage, Is.EqualTo(expectedHealthLost + expectedShieldLost));
            Assert.That(enemyHit.WasDefeated, Is.EqualTo(expectedDefeat));
            Assert.That(enemy.CurrentHealth, Is.EqualTo(2 - expectedHealthLost));
            Assert.That(enemy.CurrentShield, Is.EqualTo(initialShield - expectedShieldLost));
        }
    }
}
