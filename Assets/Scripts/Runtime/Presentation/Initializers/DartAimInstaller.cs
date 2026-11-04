using DartsRoguelike.Core.Model;
using DartsRoguelike.Presentation;
using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     ダーツの照準表示に必要なコンポーネントを結びつけるインストーラー
    /// </summary>
    public class DartAimInstaller : MonoBehaviour
    {
        [SerializeField, Tooltip("入力バインディング")]
        private DartAimInputBinding _inputBinding;

        [SerializeField, Tooltip("ダーツの照準ビュー")]
        private DartAimView _dartAimView;

        [SerializeField, Min(0f), Tooltip("照準円の最小半径、盤面半径が1")]
        private float _minimumSpreadRadius = 0.08f;

        [SerializeField, Min(0f), Tooltip("照準円の最大半径、最小半径以上に設定する")]
        private float _maximumSpreadRadius = 0.32f;

        [SerializeField, Min(0.01f), Tooltip("円が最大から最小を経て最大に戻るまでの秒数")]
        private float _cycleDuration = 1.5f;

        private void Start()
        {
            if(_inputBinding == null || _dartAimView == null)
            {
                Debug.LogError("DartAimInstaller: 必要なコンポーネントが設定されていません");
                return;
            }

            var parameters = new DartAimParameters(
                _minimumSpreadRadius,
                _maximumSpreadRadius,
                _cycleDuration
            );

            var model = new DartAimModel(parameters);
            var presenter = new DartAimPresenter(model, _dartAimView);

            _inputBinding.Initialize(presenter);
        }
    }
}
