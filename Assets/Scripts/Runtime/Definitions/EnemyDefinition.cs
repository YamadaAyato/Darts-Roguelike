using System;
using System.Collections.Generic;
using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     敵の能力値と、順番に繰り返すターンごとの行動を設定するアセット
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "Darts Roguelike/Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("敵の最大HP")]
        private int _maxHealth = 8;
        [SerializeField, Min(0), Tooltip("通常攻撃と倍率攻撃の基準となる攻撃力")]
        private int _baseAttackDamage = 3;
        [SerializeField, Tooltip("上から順に実行し、最後の次は先頭へ戻るターン一覧")]
        private List<EnemyTurnDefinition> _turns = new List<EnemyTurnDefinition>
        {
            new EnemyTurnDefinition()
        };

        /// <summary>
        ///     アセットの設定から戦闘用の敵を生成する
        /// </summary>
        public EnemyModel CreateEnemy()
        {
            if (_turns.Count == 0)
            {
                throw new InvalidOperationException($"[{name}] 行動パターンを1ターン以上設定してください");
            }

            var turns = new List<EnemyTurn>(_turns.Count);
            foreach (EnemyTurnDefinition turn in _turns)
            {
                if (turn == null)
                {
                    throw new InvalidOperationException($"[{name}] 未設定のターンがあります");
                }

                turns.Add(turn.CreateTurn());
            }

            return new EnemyModel(_maxHealth, turns, attackDamage: _baseAttackDamage);
        }
    }
}
