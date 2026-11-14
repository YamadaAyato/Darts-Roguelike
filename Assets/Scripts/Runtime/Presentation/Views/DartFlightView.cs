using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     確定した着弾点へ3Dダーツを飛ばし、到着を入力側へ伝える
    /// </summary>
    public class DartFlightView : MonoBehaviour
    {
        [SerializeField, Tooltip("盤面の座標変換")]
        private DartboardSurface _surface;
        [SerializeField, Tooltip("投擲時のダーツの先端位置")]
        private Transform _releasePoint;
        [SerializeField, Tooltip("先端を原点、ローカルZ軸の正方向を進行方向とするモデル")]
        private Transform _dart;
        [SerializeField, Min(0.01f), Tooltip("投擲から着弾までの秒数")]
        private float _duration = 0.32f;
        [SerializeField, Min(0.01f), Tooltip("照準円が最小のときに直線軌道から持ち上げる高さ")]
        private float _minimumArcHeight = 0.06f;
        [UnityEngine.Serialization.FormerlySerializedAs("_arcHeight")]
        [SerializeField, Min(0.01f), Tooltip("照準円が最大のときに直線軌道から持ち上げる高さ")]
        private float _maximumArcHeight = 0.32f;

        private Vector3 _start;
        private Vector3 _target;
        private float _currentArcHeight;
        private float _elapsed;
        private bool _missed;

        /// <summary>
        ///     着弾点と投擲時の照準円の大きさから軌道を決めてモデルを表示する
        /// </summary>
        public void Play(DartThrowResult result, float spreadRadiusRate)
        {
            _start = _releasePoint.position;
            _target = _surface.BoardToWorld(result.LandingPosition);
            float minimum = Mathf.Max(0.01f, _minimumArcHeight);
            float maximum = Mathf.Max(minimum, _maximumArcHeight);
            _currentArcHeight = Mathf.Lerp(minimum, maximum, spreadRadiusRate);
            _elapsed = 0f;
            _missed = result.HitResult.IsOutside;
            SetPose(0f);
            _dart.gameObject.SetActive(true);
        }

        /// <summary>
        ///     飛行を進め、ダーツの先端が確定した着弾点に達したフレームでtrueを返す
        /// </summary>
        public bool Advance(float deltaTime)
        {
            _elapsed += deltaTime;
            float progress = Mathf.Clamp01(_elapsed / _duration);
            SetPose(progress);
            if (progress < 1f)
            {
                return false;
            }

            if (_missed)
            {
                Clear();
            }

            return true;
        }

        /// <summary>
        ///     盤面に残っているダーツや中断した飛行を非表示にする
        /// </summary>
        public void Clear()
        {
            _dart.gameObject.SetActive(false);
        }

        /// <summary>
        ///     弧の高さを加え、ダーツの先端を軌道の進行方向へ向ける
        /// </summary>
        private void SetPose(float progress)
        {
            Vector3 up = _surface.transform.up;
            Vector3 arc = up * (4f * _currentArcHeight);
            Vector3 position = Vector3.Lerp(_start, _target, progress) + arc * (progress * (1f - progress));
            // 高さの式を微分し、飛行中と着弾時で向きが連続するようにする
            Vector3 direction = _target - _start + arc * (1f - 2f * progress);
            _dart.SetPositionAndRotation(position, Quaternion.LookRotation(direction, up));
        }
    }
}
