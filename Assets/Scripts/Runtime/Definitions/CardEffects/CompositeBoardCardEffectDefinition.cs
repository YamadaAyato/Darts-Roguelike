using System;
using System.Collections.Generic;
using DartsRoguelike.Core.Model;
using GameFoundation.Runtime.Attributers;
using UnityEngine;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     単体効果を登録順に組み合わせる設定
    /// </summary>
    [Serializable]
    public class CompositeBoardCardEffectDefinition : BoardCardEffectDefinition
    {
        [SerializeReference, SubclassSelector, Tooltip("上から順に発動する単体効果")]
        private List<SingleBoardCardEffectDefinition> _effects = new List<SingleBoardCardEffectDefinition>();

        /// <summary>
        ///     子効果を登録順に戦闘用の一覧へ展開する
        /// </summary>
        internal override void AppendEffects(List<IBoardCardEffect> effects)
        {
            foreach (SingleBoardCardEffectDefinition effect in _effects)
            {
                if (effect == null)
                {
                    throw new InvalidOperationException("複数効果に未設定の子効果があります");
                }

                effect.AppendEffects(effects);
            }
        }
    }
}
