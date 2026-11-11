using System;
using System.Collections.Generic;
using DartsRoguelike.Core.Model;
using GameFoundation.Runtime.Attributers;
using UnityEngine;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     敵の1ターン分の行動を登録順に設定する
    /// </summary>
    [Serializable]
    public class EnemyTurnDefinition
    {
        [SerializeReference, SubclassSelector, Tooltip("このターンに上から順に実行する行動。空の場合は待機")]
        private List<EnemyActionDefinition> _actions = new List<EnemyActionDefinition>
        {
            new AttackEnemyActionDefinition()
        };

        /// <summary>
        ///     設定した行動から戦闘用の1ターンを作る
        /// </summary>
        internal EnemyTurn CreateTurn()
        {
            var actions = new List<IEnemyAction>(_actions.Count);
            foreach (EnemyActionDefinition action in _actions)
            {
                if (action == null)
                {
                    throw new InvalidOperationException("敵のターンに未設定の行動があります");
                }

                actions.Add(action.CreateAction());
            }

            return new EnemyTurn(actions);
        }
    }
}
