using System;
using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     発動元に共通する危険マスの設置設定
    /// </summary>
    [Serializable]
    public class HazardPlacementSettings
    {
        [SerializeField, Tooltip("固定、盤面全体、ダブル・トリプル、カード上から配置先を選ぶ")]
        private HazardPlacementMode _mode;
        [SerializeField, Range(0, 20), Tooltip("固定指定時の配置先の番号。ブルは0")]
        private int _number = 20;
        [SerializeField, Tooltip("固定指定時の配置先のリング")]
        private DartboardRing _ring = DartboardRing.OuterSingle;
        [SerializeField, Min(1), Tooltip("危険マスに命中したときの固定ダメージ")]
        private int _damage = 3;

        /// <summary>
        ///     配置先の選び方と危険効果を保持する設定データを作る
        /// </summary>
        public HazardPlacementRequest CreateEffect()
        {
            DartboardCellId fixedCell = _mode == HazardPlacementMode.Fixed
                ? new DartboardCellId(_number, _ring)
                : default;
            return new HazardPlacementRequest(_mode, fixedCell, new BoardHazard(_damage));
        }
    }
}
