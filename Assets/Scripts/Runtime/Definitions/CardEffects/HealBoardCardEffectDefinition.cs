using System;
using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     プレイヤーのHPを回復するカード効果の設定
    /// </summary>
    [Serializable]
    public class HealBoardCardEffectDefinition : SingleBoardCardEffectDefinition
    {
        [SerializeField, Min(1), Tooltip("リング倍率を適用する前のHP回復量")]
        private int _baseHealing = 1;

        /// <summary>
        ///     HP回復効果を生成する
        /// </summary>
        protected override IBoardCardEffect CreateEffect() => new HealCardEffect(_baseHealing);
    }
}
