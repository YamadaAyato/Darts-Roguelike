using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     ダーツの照準操作と計算結果の表示を仲介するプレゼンター
    /// </summary>
    public class DartAimPresenter
    {
        public DartAimPresenter(DartAimModel model, IDartAimView view)
        {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            _view = view ?? throw new System.ArgumentNullException(nameof(view));
        }

        /// <summary> 照準中かどうか </summary>
        public bool IsAiming => _model.IsAiming;

        private readonly DartAimModel _model;
        private readonly IDartAimView _view;

        /// <summary>
        ///     照準開始し、照準円を表示する
        /// </summary>
        /// <param name="aimPosition"> 照準を開始する位置 </param>
        public void StartAim(Vector2 aimPosition)
        {
            _model.StartAim(aimPosition);

            RefreshAim();
        }

        /// <summary>
        ///     照準位置と経過時間を更新し、照準円を表示する
        /// </summary>
        /// <param name="aimPosition"> 照準位置 </param>
        /// <param name="deltaTime"> 経過時間 </param>
        public void UpdateAim(Vector2 aimPosition, float deltaTime)
        {
            _model.UpdateAimPosition(aimPosition);
            _model.AdvanceTime(deltaTime);

            RefreshAim();
        }

        /// <summary>
        ///     投擲の着弾点を確定し、照準表示を閉じる
        /// </summary>
        /// <param name="randomAngleRate"> ランダムな角度の割合 </param>
        /// <param name="randomDistanceRate"> ランダムな距離の割合 </param>
        /// <param name="spreadRadiusRate"> 投擲時の照準円の大きさ。最小を0、最大を1とする </param>
        /// <returns> ダーツの投擲結果 </returns>
        public DartThrowResult Release(float randomAngleRate, float randomDistanceRate, out float spreadRadiusRate)
        {
            spreadRadiusRate = _model.SpreadRadiusRate;
            DartThrowCommand command =
                _model.Release(randomAngleRate, randomDistanceRate);

            DartThrowResult result = 
                DartThrowCalculator.Calculate(command);

            _view.HideAim();

            return result;
        }

        /// <summary>
        ///     着弾位置をマーカーで表示する
        /// </summary>
        public void ShowThrowResult(DartThrowResult result)
        {
            _view.ShowThrowResult(result);
        }

        /// <summary>
        ///     投摘をキャンセルし、照準円を非表示にする
        /// </summary>
        public void CancelAim()
        {
            _model.CancelAim();
            _view.HideAim();
        }

        /// <summary>
        ///     現在の照準位置と半径で照準円を表示する
        /// </summary>
        private void RefreshAim()
        {
            _view.ShowAim(_model.AimPosition, _model.SpreadRadius);
        }
    }
}
