using DartsRoguelike.Core.Model;
using DartsRoguelike.Presentation;
using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     ダーツの照準と着弾結果を表示するビュー
    /// </summary>
    public class DartAimView : MonoBehaviour, IDartAimView
    {
        [SerializeField, Tooltip("得点エリアの外周に一致する盤面の表示領域")]
        private RectTransform _boardRect;

        [SerializeField, Tooltip("盤面の直下に表示する照準円")]
        private RectTransform _aimCircle;

        [SerializeField, Tooltip("着弾位置を示すマーカー")]
        private RectTransform _landingMarker;

        /// <inheritdoc/>
        public void ShowAim(Vector2 position, float spreadRadius)
        {
            float boardRadius = GetBoardRadius();
            float diameter = boardRadius * spreadRadius * 2f;

            _aimCircle.anchoredPosition = position * boardRadius;
            _aimCircle.sizeDelta = new Vector2(diameter, diameter);
            _aimCircle.gameObject.SetActive(true);
        }

        /// <inheritdoc/>
        public void HideAim()
        {
            _aimCircle.gameObject.SetActive(false);
        }

        /// <inheritdoc/>
        public void ShowThrowResult(DartThrowResult result)
        {
            _landingMarker.anchoredPosition =
                result.LandingPosition * GetBoardRadius();
            _landingMarker.gameObject.SetActive(true);
        }

        private void Awake()
        {
            HideAim();
            _landingMarker.gameObject.SetActive(false);
        }

        /// <summary>
        ///     盤面の表示領域から盤面の半径を取得する
        /// </summary>
        /// <returns> 盤面の半径 </returns>
        private float GetBoardRadius()
        {
            Rect boardRect = _boardRect.rect;

            return Mathf.Min(boardRect.width, boardRect.height) * 0.5f;
        }
    }
}
