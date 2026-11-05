#nullable enable

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     一投の着弾情報と戦闘への適用結果を表す
    /// </summary>
    public readonly struct ThrowResolution
    {
        internal ThrowResolution(
            DartThrowResult throwResult, EnemyModel? target, int enemyHealthLost, bool wasEnemyDefeated)
        {
            ThrowResult = throwResult;
            Target = target;
            EnemyHealthLost = enemyHealthLost;
            WasEnemyDefeated = wasEnemyDefeated;
        }

        /// <summary> 解決に使った着弾結果 </summary>
        public DartThrowResult ThrowResult { get; }

        /// <summary> 攻撃した敵。攻撃が発生しなかった場合はnull </summary>
        public EnemyModel? Target { get; }

        /// <summary> この一投で攻撃が発生したかどうか </summary>
        public bool DidAttack => EnemyHealthLost > 0;

        /// <summary> この一投で実際に減った敵のHP </summary>
        public int EnemyHealthLost { get; }

        /// <summary> この一投で敵を撃破したかどうか </summary>
        public bool WasEnemyDefeated { get; }
    }
}
