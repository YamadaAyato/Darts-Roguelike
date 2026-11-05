#nullable enable

using System;
using System.Collections.Generic;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     1戦闘分の盤面と敵の状態を保持する
    /// </summary>
    public class BattleState
    {
        public BattleState(BoardState board, IReadOnlyList<EnemyModel> enemies)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));

            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            if (enemies.Count == 0)
            {
                throw new ArgumentException("敵を1体以上指定してください", nameof(enemies));
            }

            // 編成元の一覧が変更されても戦闘開始時の並び順を維持する
            var battleEnemies = new List<EnemyModel>(enemies.Count);
            foreach (EnemyModel enemy in enemies)
            {
                if (enemy == null || battleEnemies.Contains(enemy))
                {
                    throw new ArgumentException(
                        "敵には個別のインスタンスを指定してください", nameof(enemies));
                }

                battleEnemies.Add(enemy);
            }

            Enemies = battleEnemies.AsReadOnly();
        }

        /// <summary> 戦闘中の盤面 </summary>
        public BoardState Board { get; }

        /// <summary> 左からの表示順に並んだ戦闘中の敵 </summary>
        public IReadOnlyList<EnemyModel> Enemies { get; }

        /// <summary> 戦闘中の敵が全滅しているかどうか </summary>
        public bool AreAllEnemiesDefeated => FindFirstLivingEnemy() == null;

        /// <summary>
        ///     左からの位置を指定して生存敵を探す
        /// </summary>
        /// <param name="enemyIndex"> 左端を0とする敵の位置 </param>
        /// <returns> 指定先が範囲外または撃破済みの場合はnull </returns>
        public EnemyModel? FindLivingEnemy(int enemyIndex)
        {
            if (enemyIndex < 0 || enemyIndex >= Enemies.Count)
            {
                return null;
            }

            EnemyModel enemy = Enemies[enemyIndex];
            return enemy.IsAlive ? enemy : null;
        }

        /// <summary>
        ///     左端の生存敵を探す
        /// </summary>
        /// <returns> 生存敵がいない場合はnull </returns>
        public EnemyModel? FindFirstLivingEnemy()
        {
            foreach (EnemyModel enemy in Enemies)
            {
                if (enemy.IsAlive)
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}
