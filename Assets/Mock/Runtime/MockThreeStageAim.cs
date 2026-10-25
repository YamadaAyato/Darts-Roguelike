using UnityEngine;

namespace DartsRoguelike.Mock
{
    /// <summary>
    ///     Mock3で使用する、範囲・中心・投擲精度の3段階照準を計算する。
    /// </summary>
    public sealed class MockThreeStageAim
    {
        /// <summary>
        ///     3段階照準を初期化する。
        /// </summary>
        /// <param name="coarseRadius"> 大まかな狙いを示す範囲の半径 </param>
        /// <param name="drawSwayRadius"> 中心点が通常時に揺れる幅 </param>
        /// <param name="tremorSwayRadius"> 震え状態で追加する揺れ幅 </param>
        public MockThreeStageAim(float coarseRadius, float drawSwayRadius, float tremorSwayRadius)
        {
            _coarseRadius = coarseRadius;
            _drawSwayRadius = drawSwayRadius;
            _tremorSwayRadius = tremorSwayRadius;
            Reset();
        }

        /// <summary> 大まかな狙いを示す範囲の中心。 </summary>
        public Vector2 CoarseAim { get; private set; }

        /// <summary> 実際の着弾抽選に使う中心。 </summary>
        public Vector2 Aim { get; private set; }

        /// <summary> 実際の着弾抽選に使う半径。 </summary>
        public float Spread { get; private set; }

        /// <summary> 大まかな狙いを示す範囲の半径。 </summary>
        public float CoarseRadius => _coarseRadius;

        /// <summary> 中心を確定する段階かどうか。 </summary>
        public bool IsDrawing => _step == AimStep.DrawCenter;

        /// <summary> 着弾範囲を確定する段階かどうか。 </summary>
        public bool IsSelectingSpread => _step == AimStep.SelectSpread;

        /// <summary> 1投の操作が始まっているかどうか。 </summary>
        public bool IsInProgress => _step != AimStep.SelectCoarseRange;

        private readonly float _coarseRadius;
        private readonly float _drawSwayRadius;
        private readonly float _tremorSwayRadius;
        private float _stepStartedAt;
        private AimStep _step;

        private const float MIN_SPREAD = 0.045f;
        private const float SPREAD_RANGE = 0.27f;
        private const float TREMOR_SPREAD = 0.04f;

        /// <summary>
        ///     現在の段階を確定し、次の段階へ進める。
        /// </summary>
        /// <returns> 今回の確定で投擲まで完了する場合はtrue。 </returns>
        public bool Advance(
            Vector2 pointerPosition,
            float currentTime,
            bool hasTremor,
            bool hasDrunk,
            bool hasPanic)
        {
            Update(pointerPosition, currentTime, hasTremor, hasDrunk, hasPanic);

            switch (_step)
            {
                case AimStep.SelectCoarseRange:
                    _step = AimStep.DrawCenter;
                    _stepStartedAt = currentTime;
                    Update(pointerPosition, currentTime, hasTremor, hasDrunk, hasPanic);
                    return false;

                case AimStep.DrawCenter:
                    _step = AimStep.SelectSpread;
                    _stepStartedAt = currentTime;
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        ///     現在の入力段階に応じて照準を更新する。
        /// </summary>
        public void Update(
            Vector2 pointerPosition,
            float currentTime,
            bool hasTremor,
            bool hasDrunk,
            bool hasPanic)
        {
            if (_step == AimStep.SelectCoarseRange)
            {
                CoarseAim = pointerPosition;
                Aim = pointerPosition;
                Spread = _coarseRadius;
                return;
            }

            if (_step == AimStep.DrawCenter)
            {
                UpdateDrawCenter(currentTime, hasTremor, hasDrunk);
                return;
            }

            float elapsed = currentTime - _stepStartedAt;
            float speed = hasPanic ? 6.8f : 4.2f;
            float wave = 0.5f + 0.5f * Mathf.Cos(elapsed * speed);
            Spread = MIN_SPREAD + wave * SPREAD_RANGE + (hasTremor ? TREMOR_SPREAD : 0f);
        }

        /// <summary>
        ///     1投の操作を最初の段階へ戻す。
        /// </summary>
        public void Reset()
        {
            _step = AimStep.SelectCoarseRange;
            CoarseAim = Vector2.zero;
            Aim = Vector2.zero;
            Spread = _coarseRadius;
        }

        /// <summary>
        ///     大まかな範囲内で揺れる中心点を更新する。
        /// </summary>
        private void UpdateDrawCenter(float currentTime, bool hasTremor, bool hasDrunk)
        {
            float elapsed = currentTime - _stepStartedAt;
            Vector2 offset = new Vector2(
                Mathf.Sin(elapsed * 1.9f),
                Mathf.Cos(elapsed * 1.9f)) * _drawSwayRadius;
            if (hasTremor)
            {
                offset += new Vector2(
                    Mathf.Sin(currentTime * 23f),
                    Mathf.Cos(currentTime * 29f)) * _tremorSwayRadius;
                offset += new Vector2(
                    Mathf.Sin(currentTime * 7.3f),
                    Mathf.Cos(currentTime * 9.1f)) * (_tremorSwayRadius * 0.5f);
            }

            if (hasDrunk)
            {
                offset += new Vector2(
                    Mathf.Sin(currentTime * 1.7f),
                    Mathf.Cos(currentTime * 1.3f)) * 0.075f;
            }

            Aim = CoarseAim + Vector2.ClampMagnitude(offset, _coarseRadius);
            Spread = MIN_SPREAD;
        }

        private enum AimStep
        {
            SelectCoarseRange,
            DrawCenter,
            SelectSpread
        }
    }
}
