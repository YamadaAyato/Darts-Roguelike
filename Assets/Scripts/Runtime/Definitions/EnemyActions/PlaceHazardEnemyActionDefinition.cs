using System;
using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     敵が危険マスを設置する行動の設定
    /// </summary>
    [Serializable]
    public class PlaceHazardEnemyActionDefinition : EnemyActionDefinition
    {
        [SerializeField, Tooltip("危険効果の設置先とダメージ")]
        private HazardPlacementSettings _placement = new HazardPlacementSettings();

        /// <summary>
        ///     予告する危険マス設置データを作る
        /// </summary>
        internal override IEnemyAction CreateAction() => _placement.CreateEffect();
    }
}
