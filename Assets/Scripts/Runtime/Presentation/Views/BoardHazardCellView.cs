using DartsRoguelike.Core.Model;
using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     1マスの危険表示を塗りと枠で描画する
    /// </summary>
    public class BoardHazardCellView : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private MeshFilter _fill;
        [SerializeField] private MeshRenderer _fillRenderer;
        [SerializeField] private LineRenderer _outline;
        [SerializeField] private LineRenderer _innerOutline;

        [SerializeField, ColorUsage(true), Tooltip("設置済み危険マスの枠色")]
        private Color _hazardColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField, ColorUsage(true), Tooltip("設置予告の枠色")]
        private Color _previewColor = new Color(1f, 0.8f, 0.15f);

        private Mesh _mesh;
        private bool _hasInnerOutline;

        /// <summary>
        ///     マスの形と盤面サイズに合わせて表示用のメッシュと枠を作る
        /// </summary>
        public void Initialize(DartboardCellId cell, float radius, int arcSegments)
        {
            // 着弾判定と同じマス形状を使い、正規化座標を盤面のローカル座標へ変換する
            Vector2[] strip = StandardDartboardCellGeometry.CreateStrip(cell, arcSegments);
            var vertices = new Vector3[strip.Length];
            var triangles = new int[arcSegments * 6];
            for (int i = 0; i < strip.Length; i++)
            {
                vertices[i] = new Vector3(strip[i].x * radius, strip[i].y * radius, 0f);
            }

            // 頂点は内周・外周のペアで並ぶため、隣り合う2ペアを三角形2枚でつないで塗りを作る
            for (int i = 0; i < arcSegments; i++)
            {
                int vertex = i * 2;
                int triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = vertex + 3;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 3;
                triangles[triangle + 5] = vertex + 2;
            }

            _mesh = new Mesh { name = $"Hazard {cell.Number} {cell.Ring}" };
            _mesh.vertices = vertices;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
            _fill.sharedMesh = _mesh;
            InitializeOutline(cell, vertices, arcSegments);
        }

        /// <summary>
        ///     設置済みの塗りと予告の枠を同じマスに重ねて表示する
        /// </summary>
        public void Render(bool hasHazard, bool hasPreview)
        {
            // 塗りは設置済みを表し、枠は予告がある間だけ予告色を優先する
            _fillRenderer.enabled = hasHazard;
            Color color = hasPreview ? _previewColor : _hazardColor;
            _outline.startColor = color;
            _outline.endColor = color;
            _innerOutline.startColor = color;
            _innerOutline.endColor = color;
            _outline.enabled = hasHazard || hasPreview;
            _innerOutline.enabled = _hasInnerOutline && (hasHazard || hasPreview);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        /// <summary>
        ///     扇形は閉じた枠、ブルは継ぎ目のない円で輪郭を作る
        /// </summary>
        private void InitializeOutline(DartboardCellId cell, Vector3[] vertices, int arcSegments)
        {
            bool isBull = cell.Ring is DartboardRing.InnerBull or DartboardRing.OuterBull;
            _hasInnerOutline = cell.Ring == DartboardRing.OuterBull;
            if (isBull)
            {
                // LineRendererが始点と終点を閉じるため、円弧末尾の重複した頂点を除く
                var outer = new Vector3[arcSegments];
                var inner = new Vector3[arcSegments];
                for (int i = 0; i < arcSegments; i++)
                {
                    inner[i] = vertices[i * 2];
                    outer[i] = vertices[i * 2 + 1];
                }

                _outline.positionCount = outer.Length;
                _outline.SetPositions(outer);
                _innerOutline.positionCount = inner.Length;
                _innerOutline.SetPositions(inner);
                return;
            }

            // 外周を一方向、内周を逆方向に並べると、両端の接続も含めて扇形を一周できる
            var outline = new Vector3[vertices.Length];
            for (int i = 0; i <= arcSegments; i++)
            {
                outline[i] = vertices[i * 2 + 1];
                outline[outline.Length - 1 - i] = vertices[i * 2];
            }

            _outline.positionCount = outline.Length;
            _outline.SetPositions(outline);
        }
    }
}
