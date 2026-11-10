#nullable enable

using System;
using System.Collections.Generic;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     戦闘中の敵と予告した行動を保持する
    /// </summary>
    public class EnemyModel : BattleCharacterModel
    {
        public EnemyModel(
            int maxHealth, int initialShield = 0, int attackDamage = 0,
            IReadOnlyList<IEnemyAction>? plannedActions = null)
            : base(maxHealth, initialShield)
        {
            if (attackDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackDamage));
            }

            var actions = new List<IEnemyAction>();
            if (plannedActions == null)
            {
                actions.Add(new EnemyAttackAction());
            }
            else
            {
                foreach (IEnemyAction action in plannedActions)
                {
                    actions.Add(action ?? throw new ArgumentException("未設定の敵行動があります", nameof(plannedActions)));
                }
            }

            _actionTemplates = actions.AsReadOnly();
            PlannedActions = _actionTemplates;
            BaseAttackDamage = attackDamage;
        }

        /// <summary> 敵ターンで登録順に実行する予告済み行動 </summary>
        public IReadOnlyList<IEnemyAction> PlannedActions { get; private set; }

        /// <summary> 各攻撃行動の計算に使う敵の基礎攻撃力 </summary>
        public int BaseAttackDamage { get; }

        private readonly IReadOnlyList<IEnemyAction> _actionTemplates;

        /// <summary>
        ///     ランダム配置先を確定して次の敵ターンの予告を作る
        /// </summary>
        internal void PrepareActions(BoardState board, Random random)
        {
            var actions = new List<IEnemyAction>(_actionTemplates.Count);
            foreach (IEnemyAction template in _actionTemplates)
            {
                if (template is HazardPlacementRequest request)
                {
                    PlaceHazardEffect? placement = HazardPlacementResolver.Select(request, board, random);
                    if (placement != null)
                    {
                        actions.Add(placement);
                    }
                }
                else
                {
                    actions.Add(template);
                }
            }

            PlannedActions = actions.AsReadOnly();
        }
    }
}
