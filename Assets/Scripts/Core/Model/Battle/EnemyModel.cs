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
            : this(maxHealth, new[]
            {
                new EnemyTurn(plannedActions ?? new IEnemyAction[] { new EnemyAttackAction() })
            }, initialShield, attackDamage)
        {
        }

        public EnemyModel(
            int maxHealth, IReadOnlyList<EnemyTurn> actionPattern, int initialShield = 0, int attackDamage = 0)
            : base(maxHealth, initialShield)
        {
            if (attackDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackDamage));
            }

            if (actionPattern == null)
            {
                throw new ArgumentNullException(nameof(actionPattern));
            }

            if (actionPattern.Count == 0)
            {
                throw new ArgumentException("行動パターンには1ターン以上指定してください", nameof(actionPattern));
            }

            var turns = new List<EnemyTurn>(actionPattern.Count);
            foreach (EnemyTurn turn in actionPattern)
            {
                turns.Add(turn ?? throw new ArgumentException("未設定のターンがあります", nameof(actionPattern)));
            }

            _actionPattern = turns.AsReadOnly();
            PlannedActions = _actionPattern[0].Actions;
            BaseAttackDamage = attackDamage;
        }

        /// <summary> 敵ターンで登録順に実行する予告済み行動 </summary>
        public IReadOnlyList<IEnemyAction> PlannedActions { get; private set; }

        /// <summary> 各攻撃行動の計算に使う敵の基礎攻撃力 </summary>
        public int BaseAttackDamage { get; }

        /// <summary> 現在予告しているパターン内のターン位置。先頭は0 </summary>
        public int ActionTurnIndex { get; private set; }

        /// <summary> 繰り返す行動パターンのターン数 </summary>
        public int ActionTurnCount => _actionPattern.Count;

        private readonly IReadOnlyList<EnemyTurn> _actionPattern;

        /// <summary>
        ///     次の行動ターンへ進み、末尾の次は先頭へ戻る
        /// </summary>
        internal void AdvanceActionTurn()
        {
            ActionTurnIndex = (ActionTurnIndex + 1) % _actionPattern.Count;
        }

        /// <summary>
        ///     ランダム配置先を確定して次の敵ターンの予告を作る
        /// </summary>
        internal void PrepareActions(BoardState board, Random random)
        {
            IReadOnlyList<IEnemyAction> templates = _actionPattern[ActionTurnIndex].Actions;
            var actions = new List<IEnemyAction>(templates.Count);
            foreach (IEnemyAction template in templates)
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
