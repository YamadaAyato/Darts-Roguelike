using System;
using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     攻撃カード効果の設定
    /// </summary>
    [Serializable]
    public class AttackBoardCardEffectDefinition : SingleBoardCardEffectDefinition
    {
        [SerializeField, Min(1), Tooltip("リング倍率を適用する前のダメージ")]
        private int _baseDamage = 1;
        [SerializeField, Tooltip("攻撃の対象範囲")]
        private AttackTargetScope _scope;

        /// <summary>
        ///     攻撃効果を生成する
        /// </summary>
        protected override IBoardCardEffect CreateEffect() => new AttackCardEffect(_baseDamage, _scope);
    }
}
