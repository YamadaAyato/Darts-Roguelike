using System;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     盤面に配置する攻撃カードの効果量を表す
    /// </summary>
    public class BoardCard
    {
        public BoardCard(int baseDamage)
        {
            if (baseDamage <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseDamage), baseDamage, "基本ダメージは1以上にしてください");
            }

            BaseDamage = baseDamage;
        }

        /// <summary> リング倍率を適用する前の基本ダメージ </summary>
        public int BaseDamage { get; }
    }
}
