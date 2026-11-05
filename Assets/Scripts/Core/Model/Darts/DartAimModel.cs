using UnityEngine;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     ダーツの照準状態を管理するモデルクラス
    /// </summary>
    public class DartAimModel
    {
        public DartAimModel(DartAimParameters parameters)
        {
            // 初期状態では、経過時間は0であり、スプレッド半径は最大値に設定する
            SpreadRadius =
                DartAimCalculator.CalculateSpreadRadius(parameters, 0f);
            _parameters = parameters;
        }

        /// <summary> 照準中かどうか </summary>
        public bool IsAiming { get; private set; }

        /// <summary> 照準位置 </summary>
        public Vector2 AimPosition { get; private set; }

        /// <summary> 照準開始からの経過時間 </summary>
        public float ElapsedTime { get; private set; }

        /// <summary> 現在のスプレッド半径 </summary>
        public float SpreadRadius { get; private set; }

        private readonly DartAimParameters _parameters;

        /// <summary>
        ///     指定位置で照準を開始します
        /// </summary>
        /// <param name="aimPosition"> 照準を開始する位置 </param>
        public void StartAim(Vector2 aimPosition)
        {
            if (IsAiming)
            {
                throw new System.InvalidOperationException("照準はすでに開始されています。");
            }

            ValidatePosition(aimPosition);

            AimPosition = aimPosition;
            ElapsedTime = 0f;
            SpreadRadius =
                DartAimCalculator.CalculateSpreadRadius(_parameters, 0f);
            IsAiming = true;
        }

        /// <summary>
        ///     照準中に照準位置を更新します
        /// </summary>
        /// <param name="aimPosition"> 更新する照準位置 </param>
        public void UpdateAimPosition(Vector2 aimPosition)
        {
            EnsureAiming();
            ValidatePosition(aimPosition);

            AimPosition = aimPosition;
        }

        /// <summary>
        ///     照準時間を進め、スプレッド半径を更新します
        /// </summary>
        /// <param name="deltaTime"> 経過時間の増分 </param>
        public void AdvanceTime(float deltaTime)
        {
            EnsureAiming();

            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                throw new System.ArgumentException("経過時間の増分は有効な数値でなければなりません。");
            }

            float nextElapsedTime = ElapsedTime + deltaTime;

            // 経過時間に基づいて、次のスプレッド半径を計算する
            float nextSpreadRadius =
                DartAimCalculator.CalculateSpreadRadius(_parameters, nextElapsedTime);

            ElapsedTime = nextElapsedTime;
            SpreadRadius = nextSpreadRadius;
        }

        /// <summary>
        ///     現在の照準から投摘コマンドを作成し、照準状態を終了します
        /// </summary>
        /// <param name="randomAngleRate"> ランダムな角度の割合 </param>
        /// <param name="randomDistanceRate"> ランダムな距離の割合 </param>
        /// <returns> 作成されたダーツ投擲コマンド </returns>
        public DartThrowCommand Release(float randomAngleRate, float randomDistanceRate)
        {
            EnsureAiming();

            // ダーツ投擲コマンドを作成する
            // 値が不正ならば、DartThrowCommandのコンストラクタで例外がスローされる
            DartThrowCommand command =
                new DartThrowCommand(AimPosition, SpreadRadius, randomAngleRate, randomDistanceRate);

            IsAiming = false;
            return command;
        }

        /// <summary>
        ///     投擲せずに照準状態を終了します
        /// </summary>
        public void CancelAim()
        {
            IsAiming = false;
            AimPosition = Vector2.zero;
            ElapsedTime = 0f;
            SpreadRadius =
                DartAimCalculator.CalculateSpreadRadius(_parameters, 0f);
        }

        /// <summary>
        ///     照準中であることを検証する
        /// </summary>
        private void EnsureAiming()
        {
            if (!IsAiming)
            {
                throw new System.InvalidOperationException("照準は開始してください");
            }
        }

        /// <summary>
        ///     照準座標が有効な数値であることを検証する
        /// </summary>
        /// <param name="aimPosition"></param>
        private static void ValidatePosition(Vector2 aimPosition)
        {
            if (float.IsNaN(aimPosition.x) || float.IsInfinity(aimPosition.x) ||
               float.IsNaN(aimPosition.y) || float.IsInfinity(aimPosition.y))
            {
                throw new System.ArgumentException(nameof(aimPosition) + "は有効な数値でなければなりません。");
            }
        }
    }
}
