#nullable enable

using System;
using System.Collections.Generic;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     着弾したマスのカード効果を戦闘状態へ適用する
    /// </summary>
    public static class ThrowEffectResolver
    {
        /// <summary>
        ///     一投のカード効果を解決し、戦闘状態の変化を返す
        /// </summary>
        /// <param name="preferredEnemyIndex"> 優先して狙う敵の位置。左端は0、指定なしはnull </param>
        public static ThrowResolution Resolve(
            DartThrowResult throwResult, BattleState state, int? preferredEnemyIndex = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (throwResult.HitResult.IsOutside || !state.Player.IsAlive || state.AreAllEnemiesDefeated)
            {
                return new ThrowResolution(throwResult);
            }

            return ResolveBoardCard(throwResult, state, preferredEnemyIndex);
        }

        /// <summary>
        ///     盤面カードの段階で着弾マスの攻撃を解決する
        /// </summary>
        private static ThrowResolution ResolveBoardCard(
            DartThrowResult throwResult, BattleState state, int? preferredEnemyIndex)
        {
            BoardCard? card = state.Board.UseCard(throwResult.HitResult.Cell);
            if (card == null)
            {
                return new ThrowResolution(throwResult);
            }

            DartboardRing ring = throwResult.HitResult.Ring;
            if (card.Scope == BoardCard.TargetScope.AllEnemies)
            {
                var hits = new List<EnemyHitResult>();
                foreach (EnemyModel enemy in state.Enemies)
                {
                    if (enemy.IsAlive)
                    {
                        hits.Add(DamageResolver.Resolve(enemy, card.BaseDamage, ring));
                    }
                }

                return new ThrowResolution(throwResult, hits.AsReadOnly());
            }

            EnemyModel? target = preferredEnemyIndex is int enemyIndex
                ? state.FindLivingEnemy(enemyIndex)
                : null;

            // 狙っている敵がいない場合は、左端の生存敵を狙う
            target ??= state.FindFirstLivingEnemy();
            if (target == null)
            {
                return new ThrowResolution(throwResult);
            }

            EnemyHitResult hit = DamageResolver.Resolve(target, card.BaseDamage, ring);
            return new ThrowResolution(throwResult, Array.AsReadOnly(new[] { hit }));
        }
    }
}
