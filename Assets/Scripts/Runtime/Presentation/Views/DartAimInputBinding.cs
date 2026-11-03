using System;
using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     入力ビューとダーツの照準表示を結びつけるコンポーネント
    /// </summary>
    public class DartAimInputBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("入力を受け取るビュー")]
        private InputView _inputView;

        [SerializeField, Tooltip("メインカメラ")]
        private Camera _mainCamera;

        [SerializeField, Tooltip("ダーツボードの表面")]
        private DartboardSurface _dartboardSurface;

        [SerializeField, Min(1f), Tooltip("照準円を移動できる範囲")]
        private float _aimLimit;

        private DartAimPresenter _presenter;
        private bool _throwRequested;
        private bool _cancelRequested;

        /// <summary>
        ///     外部からプレゼンターを設定する
        /// </summary>
        /// <param name="presenter"> 設定するプレゼンター </param>
        public void Initialize(DartAimPresenter presenter)
        {
            if (_presenter != null)
            {
                throw new InvalidOperationException("プレゼンターは既に初期化されています");
            }

            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));

            if (isActiveAndEnabled)
            {
                SubsuribeInput();
            }
        }

        private void OnEnable()
        {
            if (_presenter != null)
            {
                SubsuribeInput();
            }
        }

        private void OnDisable()
        {
            if (_presenter == null)
            {
                return;
            }

            UnsubscribeInput();

            _throwRequested = false;
            _cancelRequested = false;
            _presenter.CancelAim();
        }

        private void HandleThrowPressed()
        {
            _throwRequested = true;
        }

        private void HandleCancelRequested()
        {
            _cancelRequested = true;
        }

        private void SubsuribeInput()
        {
            _inputView.OnThrowPressed += HandleThrowPressed;
            _inputView.OnCancelRequested += HandleCancelRequested;
        }

        private void UnsubscribeInput()
        {
            _inputView.OnThrowPressed -= HandleThrowPressed;
            _inputView.OnCancelRequested -= HandleCancelRequested;
        }
    }
}
