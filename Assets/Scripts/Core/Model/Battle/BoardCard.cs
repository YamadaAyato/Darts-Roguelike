using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     盤面に配置する攻撃カードの効果量を表す
    /// </summary>
    public class BoardCard
    {
        public BoardCard(int baseDamage, TargetScope scope = TargetScope.SingleEnemy)
        {
            if (baseDamage <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseDamage), baseDamage, "基本ダメージは1以上にしてください");
            }

            BaseDamage = baseDamage;
            Scope = scope;
        }

        /// <summary> リング倍率を適用する前の基本ダメージ </summary>
        public int BaseDamage { get; }

        /// <summary> 攻撃が届く範囲 </summary>
        public TargetScope Scope { get; }

        /// <summary>
        ///     攻撃カードの対象範囲
        /// </summary>
        public enum TargetScope
        {
            SingleEnemy,
            AllEnemies
        }
    }
}
