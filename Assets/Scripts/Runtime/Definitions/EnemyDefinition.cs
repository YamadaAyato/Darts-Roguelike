using System;
using System.Collections.Generic;
using DartsRoguelike.Core.Model;
using GameFoundation.Runtime.Attributers;
using UnityEngine;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     敵の最大HPと、順に実行する行動を設定するアセット
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "Darts Roguelike/Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("敵の最大HP")]
        private int _maxHealth = 8;
        [SerializeField, Min(0), Tooltip("通常攻撃と倍率攻撃の基準となる攻撃力")]
        private int _baseAttackDamage = 3;
        [SerializeReference, SubclassSelector, Tooltip("敵ターンで上から順に実行する行動")]
        private List<EnemyActionDefinition> _actions = new List<EnemyActionDefinition>();

        /// <summary>
        ///     アセットの設定から戦闘用の敵を生成する
        /// </summary>
        public EnemyModel CreateEnemy()
        {
            var actions = new List<IEnemyAction>(_actions.Count);
            foreach (EnemyActionDefinition action in _actions)
            {
                if (action == null)
                {
                    throw new InvalidOperationException($"[{name}] 未設定の敵行動があります");
                }

                actions.Add(action.CreateAction());
            }

            return new EnemyModel(_maxHealth, attackDamage: _baseAttackDamage, plannedActions: actions);
        }
    }
}
