using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     ダーツの照準と着弾結果を表示するビューのインターフェース
    /// </summary>
    public interface IDartAimView
    {
        /// <summary>
        ///     指定位置と半径で照準円を表示する
        /// </summary>
        /// <param name="position"> 照準円の中心位置 </param>
        /// <param name="spreadRadius"> 照準円の半径 </param>
        void ShowAim(Vector2 position, float spreadRadius);

        /// <summary>
        ///     照準円を非表示にする
        /// </summary>
        void HideAim();

        /// <summary>
        ///     ダーツの投擲結果を表示する
        /// </summary>
        /// <param name="result"> 投擲結果 </param>
        void ShowThrowResult(DartThrowResult result);
    }
}
