using System;
using System.Collections.Generic;
using DartsRoguelike.Core.Model;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     単独でも複数効果の子としても使える効果設定
    /// </summary>
    [Serializable]
    public abstract class SingleBoardCardEffectDefinition : BoardCardEffectDefinition
    {
        /// <summary>
        ///     単体効果を戦闘用の一覧へ追加する
        /// </summary>
        internal override void AppendEffects(List<IBoardCardEffect> effects)
        {
            effects.Add(CreateEffect());
        }

        /// <summary>
        ///     単体効果の戦闘用データを生成する
        /// </summary>
        protected abstract IBoardCardEffect CreateEffect();
    }
}
