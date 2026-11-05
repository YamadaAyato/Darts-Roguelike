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

            if (throwResult.HitResult.IsOutside)
            {
                return new ThrowResolution(throwResult);
            }

            BoardCard? card = state.Board.GetCard(throwResult.HitResult.Cell);
            if (card == null)
            {
                return new ThrowResolution(throwResult);
            }

            int multiplier = throwResult.HitResult.Ring switch
            {
                DartboardRing.Double => 2,
                DartboardRing.Triple => 3,
                _ => 1
            };

            // 倍率計算で整数があふれても、適用するダメージが負にならないようにする
            int damage = (int)Math.Min((long)card.BaseDamage * multiplier, int.MaxValue);
            if (card.Scope == BoardCard.TargetScope.AllEnemies)
            {
                var hits = new List<EnemyHitResult>();
                foreach (EnemyModel enemy in state.Enemies)
                {
                    if (enemy.IsAlive)
                    {
                        hits.Add(ApplyDamage(enemy, damage));
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

            return new ThrowResolution(
                throwResult, Array.AsReadOnly(new[] { ApplyDamage(target, damage) }));
        }

        /// <summary>
        ///     対象の敵にダメージを適用し、その場で撃破結果を記録する
        /// </summary>
        private static EnemyHitResult ApplyDamage(EnemyModel target, int damage)
        {
            int healthLost = target.TakeDamage(damage);
            return new EnemyHitResult(target, healthLost, !target.IsAlive);
        }
    }
}
