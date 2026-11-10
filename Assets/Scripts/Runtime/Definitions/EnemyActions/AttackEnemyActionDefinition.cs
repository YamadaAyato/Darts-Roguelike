using System;
using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     敵の通常攻撃の設定
    /// </summary>
    [Serializable]
    public class AttackEnemyActionDefinition : EnemyActionDefinition
    {
        [SerializeField, Min(0), Tooltip("基礎攻撃力に掛ける倍率。通常攻撃は1、2倍攻撃は2")]
        private float _damageMultiplier = 1;

        /// <summary>
        ///     予告する攻撃データを作る
        /// </summary>
        internal override IEnemyAction CreateAction() => new EnemyAttackAction(_damageMultiplier);
    }
}
