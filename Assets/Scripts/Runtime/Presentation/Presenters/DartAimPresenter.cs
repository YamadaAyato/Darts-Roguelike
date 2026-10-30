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
        ///     ダーツを投げ、結果を表示する
        /// </summary>
        /// <param name="randomAngleRate"> ランダムな角度の割合 </param>
        /// <param name="randomDistanceRate"> ランダムな距離の割合 </param>
        /// <returns> ダーツの投擲結果 </returns>
        public DartThrowResult Release(float randomAngleRate,float randomDistanceRate)
        {
            DartThrowCommand command = 
                _model.Release(randomAngleRate, randomDistanceRate);

            DartThrowResult result = 
                DartThrowCalculator.Calculate(command);

            _view.HideAim();
            _view.ShowThrowResult(result);

            return result;
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
