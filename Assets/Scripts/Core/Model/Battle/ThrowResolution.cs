#nullable enable

using System;
using System.Collections.Generic;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     一投の着弾情報と戦闘への適用結果を表す
    /// </summary>
    public readonly struct ThrowResolution
    {
        internal ThrowResolution(
            DartThrowResult throwResult, IReadOnlyList<EnemyHitResult>? enemyHits = null,
            int shieldGained = 0, int healthRecovered = 0,
            int hazardHealthLost = 0, int hazardShieldLost = 0)
        {
            ThrowResult = throwResult;
            ShieldGained = shieldGained;
            HealthRecovered = healthRecovered;
            HazardHealthLost = hazardHealthLost;
            HazardShieldLost = hazardShieldLost;
            _enemyHits = enemyHits;
        }

        /// <summary> 解決に使った着弾結果 </summary>
        public DartThrowResult ThrowResult { get; }

        /// <summary> この一投で攻撃が発生したかどうか </summary>
        public bool DidAttack => EnemyHits.Count > 0;

        /// <summary> 効果の発動順に並んだ攻撃結果。全体攻撃は左からの表示順 </summary>
        public IReadOnlyList<EnemyHitResult> EnemyHits => _enemyHits ?? Array.Empty<EnemyHitResult>();

        /// <summary> この一投でプレイヤーが実際に獲得したシールド </summary>
        public int ShieldGained { get; }

        /// <summary> この一投でプレイヤーが実際に回復したHP </summary>
        public int HealthRecovered { get; }

        /// <summary> 危険効果でプレイヤーが失ったHP </summary>
        public int HazardHealthLost { get; }

        /// <summary> 危険効果でプレイヤーが失ったシールド </summary>
        public int HazardShieldLost { get; }

        private readonly IReadOnlyList<EnemyHitResult>? _enemyHits;
    }
}
