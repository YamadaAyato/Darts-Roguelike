using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     入力を受け取るビュー
    /// </summary>
    public class InputView : MonoBehaviour
    {
        /// <summary> 投擲ボタンが押されたときに発火するイベント </summary>
        public event Action OnThrowPressed;

        /// <summary> キャンセルが要求されたときに発火するイベント </summary>
        public event Action OnCancelRequested;

        /// <summary> ポインターの位置を取得する </summary>
        public Vector2 PointPosition => _pointAction.action.ReadValue<Vector2>();

        [SerializeField, Tooltip("ポインターの位置を取得するInputActionReference")]
        private InputActionReference _pointAction;

        [SerializeField, Tooltip("投擲ボタンのInputActionReference")]
        private InputActionReference _throwAction;

        [SerializeField, Tooltip("キャンセルボタンのInputActionReference")]
        private InputActionReference _cancelAction;

        private void OnEnable()
        {
            _throwAction.action.performed += HandleThrowPressed;
            _cancelAction.action.performed += HandleCancelRequested;

            _pointAction.action.Enable();
            _throwAction.action.Enable();
            _cancelAction.action.Enable();
        }

        private void OnDisable()
        {
            _throwAction.action.performed -= HandleThrowPressed;
            _cancelAction.action.performed -= HandleCancelRequested;

            _pointAction.action.Disable();
            _throwAction.action.Disable();
            _cancelAction.action.Disable();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                OnCancelRequested?.Invoke();
            }
        }

        private void HandleThrowPressed(InputAction.CallbackContext context)
        {
            OnThrowPressed?.Invoke();
        }

        private void HandleCancelRequested(InputAction.CallbackContext context)
        {
            OnCancelRequested?.Invoke();
        }
    }
}
