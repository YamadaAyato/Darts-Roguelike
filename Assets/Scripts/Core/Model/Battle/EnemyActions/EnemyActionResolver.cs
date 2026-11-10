using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     敵の予告済み行動を戦闘状態へ適用する
    /// </summary>
    public static class EnemyActionResolver
    {
        /// <summary>
        ///     基礎攻撃力に行動の倍率を掛け、この一撃のダメージを切り捨てで求める
        /// </summary>
        public static int CalculateAttackDamage(EnemyModel enemy, EnemyAttackAction action)
        {
            double damage = enemy.BaseAttackDamage * action.DamageMultiplier;
            return (int)Math.Min(Math.Floor(damage), int.MaxValue);
        }

        /// <summary>
        ///     生存中の敵の行動を登録順に実行し、プレイヤー死亡時は打ち切る
        /// </summary>
        internal static void Resolve(EnemyModel enemy, BattleState state)
        {
            if (!enemy.IsAlive)
            {
                return;
            }

            foreach (IEnemyAction action in enemy.PlannedActions)
            {
                if (!state.Player.IsAlive)
                {
                    break;
                }

                switch (action)
                {
                    case EnemyAttackAction attack:
                        DamageResolver.Resolve(state.Player, CalculateAttackDamage(enemy, attack));
                        break;
                    case PlaceHazardEffect placement:
                        HazardPlacementResolver.Resolve(placement, state.Board);
                        break;
                }
            }
        }
    }
}
