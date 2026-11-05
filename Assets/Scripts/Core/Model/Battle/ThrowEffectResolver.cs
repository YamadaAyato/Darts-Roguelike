#nullable enable

using System;

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
        public static ThrowResolution Resolve(DartThrowResult throwResult, BattleState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (throwResult.HitResult.IsOutside)
            {
                return new ThrowResolution(throwResult, null, 0, false);
            }

            EnemyModel? target = state.FindFirstLivingEnemy();
            BoardCard? card = state.Board.GetCard(throwResult.HitResult.Cell);
            if (target == null || card == null)
            {
                return new ThrowResolution(throwResult, null, 0, false);
            }

            int multiplier = throwResult.HitResult.Ring switch
            {
                DartboardRing.Double => 2,
                DartboardRing.Triple => 3,
                _ => 1
            };

            // 倍率計算で整数があふれても、適用するダメージが負にならないようにする
            int damage = (int)Math.Min((long)card.BaseDamage * multiplier, int.MaxValue);
            int healthLost = target.TakeDamage(damage);
            return new ThrowResolution(throwResult, target, healthLost, !target.IsAlive);
        }
    }
}
