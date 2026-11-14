using System;
using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     入力ビューとダーツの照準表示を結びつけるコンポーネント
    /// </summary>
    public class DartAimInputBinding : MonoBehaviour
    {
        /// <summary> 着弾したときの投擲結果 </summary>
        public event Action<DartThrowResult> OnThrowResolved;

        [SerializeField, Tooltip("入力を受け取るビュー")]
        private InputView _inputView;

        [SerializeField, Tooltip("メインカメラ")]
        private Camera _mainCamera;

        [SerializeField, Tooltip("ダーツボードの表面")]
        private DartboardSurface _dartboardSurface;

        [SerializeField, Min(1f), Tooltip("照準円を移動できる範囲")]
        private float _aimLimit;

        [SerializeField, Tooltip("設定するとダーツが盤面に着くまで効果解決を待つ")]
        private DartFlightView _flightView;

        private DartAimPresenter _presenter;
        private DartThrowResult? _pendingThrow;
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

        /// <summary>
        ///     前の戦闘で表示したダーツを消す
        /// </summary>
        public void ClearFlightVisual()
        {
            _flightView?.Clear();
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
            if (_pendingThrow.HasValue)
            {
                _pendingThrow = null;
                ClearFlightVisual();
            }

            _presenter.CancelAim();
        }

        private void LateUpdate()
        {
            if (_presenter == null)
            {
                return;
            }

            bool throwRequested = _throwRequested;
            bool cancelRequested = _cancelRequested;
            _throwRequested = false;
            _cancelRequested = false;

            // 飛行中の入力は使わず、到着した投擲だけを一度通知する
            if (_pendingThrow is DartThrowResult pendingThrow)
            {
                if (_flightView.Advance(Time.deltaTime))
                {
                    _pendingThrow = null;
                    OnThrowResolved?.Invoke(pendingThrow);
                }

                return;
            }

            // クリックとキャンセルの両方が同時に発生した場合はキャンセルを優先する
            if (cancelRequested || !_inputView.isActiveAndEnabled)
            {
                if (_presenter.IsAiming)
                {
                    _presenter.CancelAim();
                }

                return;
            }

            if (!_presenter.IsAiming && !throwRequested)
            {
                return;
            }

            // ポインターの位置を取得する
            Ray ray = _mainCamera.ScreenPointToRay(_inputView.PointPosition);
            if (!_dartboardSurface.TryRaytoBoard(ray, out Vector2 boardPosition))
            {
                if (_presenter.IsAiming)
                {
                    _presenter.CancelAim();
                }

                return;
            }

            if (!_presenter.IsAiming)
            {
                if (boardPosition.sqrMagnitude <= _aimLimit * _aimLimit)
                {
                    _presenter.StartAim(boardPosition);
                }

                return;
            }

            // 2回目のクリックでも細心の位置と円の大きさを反映してから投げる
            _presenter.UpdateAim(
                Vector2.ClampMagnitude(boardPosition, _aimLimit), Time.deltaTime);

            if (throwRequested)
            {
                DartThrowResult result = _presenter.Release(
                    UnityEngine.Random.value,
                    UnityEngine.Random.value,
                    out float spreadRadiusRate);
                if (_flightView == null)
                {
                    _presenter.ShowThrowResult(result);
                    OnThrowResolved?.Invoke(result);
                    return;
                }

                _flightView.Play(result, spreadRadiusRate);
                _pendingThrow = result;
            }
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
