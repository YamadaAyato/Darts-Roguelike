using System.Collections.Generic;
using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     盤面上の危険表示をマスごとに再利用する
    /// </summary>
    public class BoardHazardView : MonoBehaviour, IBoardHazardView
    {
        [Header("参照")]
        [SerializeField] private DartboardSurface _surface;
        [SerializeField] private BoardHazardCellView _cellTemplate;

        [SerializeField, Range(4, 64), Tooltip("扇形の円弧の分割数。ブルは4倍の分割数")]
        private int _arcSegments = 12;
        [SerializeField, Min(0f), Tooltip("盤面の手前へずらす量。盤面のローカル座標単位")]
        private float _surfaceOffset = 0.004f;

        // 予告が消えたマスも保持し、再表示時にPrefabとメッシュを使い回す
        private readonly Dictionary<DartboardCellId, BoardHazardCellView> _cells =
            new Dictionary<DartboardCellId, BoardHazardCellView>();

        // Viewを一時的に無効化しても、再有効化時に直前の表示を復元できるようにする
        private readonly HashSet<DartboardCellId> _visibleCells = new HashSet<DartboardCellId>();

        /// <summary>
        ///     最新の表示一覧を受け取り、一覧から外れたマスを非表示にする
        /// </summary>
        public void Render(IReadOnlyList<BoardHazardCellDisplay> cells)
        {
            // 敵の撃破などで一覧から外れた予告が残らないよう、一度すべての表示を隠す
            _visibleCells.Clear();
            foreach (BoardHazardCellView view in _cells.Values)
            {
                view.gameObject.SetActive(false);
            }

            foreach (BoardHazardCellDisplay cell in cells)
            {
                if (!cell.HasHazard && !cell.HasPreview)
                {
                    continue;
                }

                if (!_cells.TryGetValue(cell.Cell, out BoardHazardCellView view))
                {
                    // 盤面の子にすると、ローカル座標で作った表示が盤面の移動や回転に追従する
                    view = Instantiate(_cellTemplate, _surface.transform);
                    view.name = $"Hazard {cell.Cell.Number} {cell.Cell.Ring}";
                    view.transform.localPosition = Vector3.back * _surfaceOffset;
                    view.transform.localRotation = Quaternion.identity;
                    view.transform.localScale = Vector3.one;

                    // ブルは一周分の円を描くため、分割数を増やして角を目立ちにくくする
                    bool isBull = cell.Cell.Ring is DartboardRing.InnerBull or DartboardRing.OuterBull;
                    view.Initialize(cell.Cell, _surface.Radius, isBull ? _arcSegments * 4 : _arcSegments);
                    _cells.Add(cell.Cell, view);
                }

                view.Render(cell.HasHazard, cell.HasPreview);
                _visibleCells.Add(cell.Cell);
                view.gameObject.SetActive(isActiveAndEnabled);
            }
        }

        private void OnEnable()
        {
            foreach (KeyValuePair<DartboardCellId, BoardHazardCellView> cell in _cells)
            {
                cell.Value.gameObject.SetActive(_visibleCells.Contains(cell.Key));
            }
        }

        private void OnDisable()
        {
            foreach (BoardHazardCellView view in _cells.Values)
            {
                if (view != null)
                {
                    view.gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (BoardHazardCellView view in _cells.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
        }
    }
}
