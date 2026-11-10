using System;
using System.Collections.Generic;
using DartsRoguelike.Core.Model;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     Inspectorで編集するカード効果の設定
    /// </summary>
    [Serializable]
    public abstract class BoardCardEffectDefinition
    {
        /// <summary>
        ///     この設定を戦闘用の効果一覧へ変換する
        /// </summary>
        internal abstract void AppendEffects(List<IBoardCardEffect> effects);
    }
}
