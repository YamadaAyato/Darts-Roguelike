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
            DartThrowResult throwResult, IReadOnlyList<EnemyHitResult>? enemyHits = null)
        {
            ThrowResult = throwResult;
            _enemyHits = enemyHits;
        }

        /// <summary> 解決に使った着弾結果 </summary>
        public DartThrowResult ThrowResult { get; }

        /// <summary> この一投で攻撃が発生したかどうか </summary>
        public bool DidAttack => EnemyHits.Count > 0;

        /// <summary> 左からの表示順に並んだ敵ごとの攻撃結果 </summary>
        public IReadOnlyList<EnemyHitResult> EnemyHits => _enemyHits ?? Array.Empty<EnemyHitResult>();

        private readonly IReadOnlyList<EnemyHitResult>? _enemyHits;
    }
}
