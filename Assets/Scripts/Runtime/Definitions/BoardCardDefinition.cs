using System;
using System.Collections.Generic;
using DartsRoguelike.Core.Model;
using GameFoundation.Runtime.Attributers;
using UnityEngine;

namespace DartsRoguelike.Definitions
{
    /// <summary>
    ///     盤面カードの効果を編集するための設定アセット
    /// </summary>
    [CreateAssetMenu(fileName = "BoardCard", menuName = "Darts Roguelike/Board Card")]
    public class BoardCardDefinition : ScriptableObject
    {
        [SerializeReference, SubclassSelector, Tooltip("カードの効果設定")]
        private BoardCardEffectDefinition _effect = new AttackBoardCardEffectDefinition();

        /// <summary>
        ///     カードアセットの設定から、戦闘で使うカードを生成する
        /// </summary>
        public BoardCard CreateCard()
        {
            if (_effect == null)
            {
                throw new InvalidOperationException($"[{name}] カード効果を設定してください");
            }

            var effects = new List<IBoardCardEffect>();
            _effect.AppendEffects(effects);
            return new BoardCard(effects);
        }
    }
}
