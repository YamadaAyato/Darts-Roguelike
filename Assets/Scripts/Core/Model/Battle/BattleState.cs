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
        public BattleState(
            BoardState board, IReadOnlyList<EnemyModel> enemies, int throwsPerTurn = 3,
            BattleCharacterModel? player = null, int hazardSeed = 1)
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

            if (throwsPerTurn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(throwsPerTurn));
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
            Player = player ?? new BattleCharacterModel(12);
            _throwsPerTurn = throwsPerTurn;
            RemainingThrows = throwsPerTurn;
            _hazardRandom = new Random(hazardSeed);
            PrepareEnemyActions();
        }

        /// <summary> 戦闘中の盤面 </summary>
        public BoardState Board { get; }

        /// <summary> 戦闘中のプレイヤー </summary>
        public BattleCharacterModel Player { get; }

        /// <summary> 左からの表示順に並んだ戦闘中の敵 </summary>
        public IReadOnlyList<EnemyModel> Enemies { get; }

        /// <summary> 戦闘中の敵が全滅しているかどうか </summary>
        public bool AreAllEnemiesDefeated => FindFirstLivingEnemy() == null;

        /// <summary> プレイヤーターンの残り投擲数 </summary>
        public int RemainingThrows { get; private set; }

        /// <summary> 生存敵がいて、プレイヤーが投擲を終えたかどうか </summary>
        public bool IsEnemyTurn => RemainingThrows == 0 && Player.IsAlive && !AreAllEnemiesDefeated;

        private readonly int _throwsPerTurn;
        private readonly Random _hazardRandom;

        /// <summary>
        ///     戦闘が継続中で残り本数があれば、確定した投擲を1本消費する
        /// </summary>
        public bool TryConsumeThrow()
        {
            if (RemainingThrows == 0 || !Player.IsAlive || AreAllEnemiesDefeated)
            {
                return false;
            }

            RemainingThrows--;
            return true;
        }

        /// <summary>
        ///     生存敵が予告した行動を実行し、プレイヤーが生存していれば次のターンへ進む
        /// </summary>
        public bool TryResolveEnemyTurn()
        {
            if (!IsEnemyTurn)
            {
                return false;
            }

            foreach (EnemyModel enemy in Enemies)
            {
                EnemyActionResolver.Resolve(enemy, this);
                if (!Player.IsAlive)
                {
                    break;
                }
            }

            if (Player.IsAlive)
            {
                foreach (EnemyModel enemy in Enemies)
                {
                    if (enemy.IsAlive)
                    {
                        enemy.AdvanceActionTurn();
                    }
                }

                RemainingThrows = _throwsPerTurn;
                Board.RefreshUsedCards();
                PrepareEnemyActions();
            }

            return true;
        }

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

        /// <summary>
        ///     戦闘用の乱数で危険マスの配置先を確定する
        /// </summary>
        internal PlaceHazardEffect? SelectHazardPlacement(HazardPlacementRequest request)
        {
            return HazardPlacementResolver.Select(request, Board, _hazardRandom);
        }

        /// <summary>
        ///     生存敵の次の行動をプレイヤーターン開始時に確定する
        /// </summary>
        private void PrepareEnemyActions()
        {
            foreach (EnemyModel enemy in Enemies)
            {
                if (enemy.IsAlive)
                {
                    enemy.PrepareActions(Board, _hazardRandom);
                }
            }
        }
    }
}
