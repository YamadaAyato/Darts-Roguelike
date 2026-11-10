using System;
using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     シールドを付与するカード効果の設定
    /// </summary>
    [Serializable]
    public class ShieldBoardCardEffectDefinition : SingleBoardCardEffectDefinition
    {
        [SerializeField, Min(1), Tooltip("リング倍率を適用する前のシールド量")]
        private int _baseShield = 1;

        /// <summary>
        ///     シールド効果を生成する
        /// </summary>
        protected override IBoardCardEffect CreateEffect() => new ShieldCardEffect(_baseShield);
    }
}
