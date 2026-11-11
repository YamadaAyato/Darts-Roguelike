using System;
using System.Collections.Generic;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     敵が1ターンで順に実行する行動の組み合わせ
    /// </summary>
    public class EnemyTurn
    {
        public EnemyTurn(IReadOnlyList<IEnemyAction> actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            var copiedActions = new List<IEnemyAction>(actions.Count);
            foreach (IEnemyAction action in actions)
            {
                copiedActions.Add(action ?? throw new ArgumentException("未設定の敵行動があります", nameof(actions)));
            }

            Actions = copiedActions.AsReadOnly();
        }

        /// <summary> 登録順に実行する行動。空の場合は待機 </summary>
        public IReadOnlyList<IEnemyAction> Actions { get; }
    }
}
