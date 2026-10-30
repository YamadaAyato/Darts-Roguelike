using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     3D盤面上に照準円と着弾位置を表示する
    /// </summary>
    public class DartAimView : MonoBehaviour, IDartAimView
    {
        [SerializeField, Tooltip("盤面の座標変換を担当するコンポーネント")]
        private DartboardSurface _surface;

        [SerializeField, Tooltip("照準円を描画するLineRenderer")]
        private LineRenderer _aimCircle;

        [SerializeField, Tooltip("着弾位置を表示する3Dマーカー")]
        private Transform _landingMarker;

        [SerializeField, Range(16, 128), Tooltip("照準円を構成する頂点数")]
        private int _circleSegments = 64;

        [SerializeField, Min(0f)]
        [Tooltip("盤面との重なりを避ける表示位置のずらし量、ワールド単位")]
        private float _surfaceOffset = 0.01f;

        private Vector3[] _circlePoints;

        /// <summary>
        ///     正規化座標の照準円を3D盤面上に表示する
        /// </summary>
        /// <param name="position"> 照準中心の正規化座標 </param>
        /// <param name="spreadRadius"> 正規化された拡散半径 </param>
        public void ShowAim(Vector2 position, float spreadRadius)
        {
            for (int i = 0; i < _circlePoints.Length; i++)
            {
                float angle =
                    i * (Mathf.PI * 2f / _circlePoints.Length);

                Vector2 direction =
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                Vector2 boardPosition =
                    position + direction * spreadRadius;

                _circlePoints[i] = GetDisplayPosition(boardPosition);
            }

            _aimCircle.SetPositions(_circlePoints);
            _aimCircle.enabled = true;
        }

        /// <summary>
        ///     照準円を非表示にする
        /// </summary>
        public void HideAim()
        {
            _aimCircle.enabled = false;
        }

        /// <summary>
        ///     確定した着弾位置へ3Dマーカーを表示する
        /// </summary>
        public void ShowThrowResult(DartThrowResult result)
        {
            _landingMarker.position =
                GetDisplayPosition(result.LandingPosition);
            _landingMarker.gameObject.SetActive(true);
        }

        private void Awake()
        {
            _circlePoints = new Vector3[_circleSegments];
            _aimCircle.positionCount = _circlePoints.Length;

            HideAim();
            _landingMarker.gameObject.SetActive(false);
        }

        /// <summary>
        ///     盤面の少し手前にある表示用ワールド座標を取得する
        /// </summary>
        private Vector3 GetDisplayPosition(Vector2 boardPosition)
        {
            return _surface.BoardToWorld(boardPosition) -
                _surface.transform.forward * _surfaceOffset;
        }
    }
}
