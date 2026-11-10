using System;
using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     カードから危険マスを設置する効果の設定
    /// </summary>
    [Serializable]
    public class PlaceHazardCardEffectDefinition : SingleBoardCardEffectDefinition
    {
        [SerializeField, Tooltip("危険効果の設置先とダメージ")]
        private HazardPlacementSettings _placement = new HazardPlacementSettings();

        /// <summary>
        ///     カード用の危険マス設置データを作る
        /// </summary>
        protected override IBoardCardEffect CreateEffect() => _placement.CreateEffect();
    }
}
