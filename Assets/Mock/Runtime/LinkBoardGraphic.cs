using UnityEngine;
using UnityEngine.EventSystems;

namespace DartsRoguelike.Mock
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LinkBoardGraphic : UnityEngine.UI.MaskableGraphic, IPointerDownHandler, IPointerUpHandler
    {
        public MockBattle2 Battle;
        public bool Overlay;

        static readonly Color[] Palette = {
            new Color(.91f,.31f,.29f), new Color(.22f,.59f,.85f), new Color(.27f,.73f,.53f),
            new Color(.65f,.42f,.84f), new Color(.89f,.57f,.29f), new Color(.93f,.78f,.35f)
        };
        static readonly Color Empty = new Color(.095f,.125f,.16f);
        static readonly Color Activated = new Color(1f,.78f,.28f);

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Overlay && eventData.button == PointerEventData.InputButton.Left) Battle.BeginAim();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!Overlay && eventData.button == PointerEventData.InputButton.Left) Battle.ReleaseAim();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            if (Battle == null || Battle.Model == null) return;
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
            if (Overlay) DrawOverlay(vh, radius);
            else DrawBoard(vh, radius);
        }

        void DrawBoard(UnityEngine.UI.VertexHelper vh, float radius)
        {
            for (int i = 0; i < 20; i++)
            {
                Sector sector = Battle.Model.Board[i];
                Wedge(vh, i, .0935f * radius, .56f * radius, CellColor(i, 0, sector.Cards[0], false));
                Wedge(vh, i, .56f * radius, .62f * radius, CellColor(i, 1, sector.Cards[1], true));
                Wedge(vh, i, .62f * radius, .94f * radius, CellColor(i, 2, sector.Cards[2], false));
                Wedge(vh, i, .94f * radius, radius, CellColor(i, 3, sector.Cards[3], true));

                for (int band = 0; band < 4; band++)
                {
                    int key = LinkBattleModel.CellKey(i, (BoardBand)band);
                    if (!Battle.Model.DangerCells.Contains(key)) continue;
                    float[] inner = { .49f, .565f, .86f, .945f };
                    float[] outer = { .55f, .615f, .93f, .995f };
                    Wedge(vh, i, inner[band] * radius, outer[band] * radius, new Color(.95f,.13f,.18f,.92f));
                }
            }
            Color bull = CellColor(20, 0, Battle.Model.Board[20].Cards[0], true);
            Disc(vh, Vector2.zero, radius * .0935f, bull);
            Disc(vh, Vector2.zero, radius * .0375f, Color.Lerp(bull, Color.white, .18f));
        }

        Color CellColor(int sector, int band, CardKind card, bool bright)
        {
            int key = LinkBattleModel.CellKey(sector, (BoardBand)band);
            if (Battle.Model.ActivatedThisTurn.Contains(key)) return Color.Lerp(CardColor(card, bright), Activated, .55f);
            return CardColor(card, bright);
        }

        static Color CardColor(CardKind card, bool bright)
        {
            if (card == CardKind.Empty) return bright ? Empty * 1.25f : Empty;
            return Palette[(int)card] * (bright ? 1f : .68f);
        }

        void DrawOverlay(UnityEngine.UI.VertexHelper vh, float radius)
        {
            for (int i = 0; i < Battle.Model.Segments.Count; i++)
            {
                LinkSegment segment = Battle.Model.Segments[i];
                Color color = segment.Power == 1 ? new Color(.25f,.84f,1f,.9f) : new Color(1f,.72f,.22f,.95f);
                Line(vh, segment.From * radius, segment.To * radius, segment.Power == 1 ? 6f : 9f, color);
            }
            if (Battle.Aiming && Battle.Model.Points.Count > 0)
            {
                Vector2 from = Battle.Model.Points[Battle.Model.Points.Count - 1] * radius;
                Line(vh, from, Battle.Aim * radius, 4f, new Color(1f,1f,1f,.42f));
            }
            for (int i = 0; i < Battle.Model.Points.Count; i++)
            {
                Color color = i == 0 ? new Color(.25f,.84f,1f) : new Color(1f,.78f,.28f);
                Disc(vh, Battle.Model.Points[i] * radius, 9f, color);
                Disc(vh, Battle.Model.Points[i] * radius, 4f, Color.white);
            }
            if (Battle.HasHit)
            {
                Circle(vh, Battle.LastHit * radius, 12f, 2f, new Color(1f,.92f,.55f));
            }
            if (Battle.Aiming)
            {
                Circle(vh, Battle.Aim * radius, Battle.Spread * radius, 3f, new Color(1f,1f,1f,.9f));
                Circle(vh, Battle.Aim * radius, 4f, 2f, Color.white);
            }
        }

        static Vector2 Polar(float degrees, float radius)
        {
            float angle = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
        }

        static void Wedge(UnityEngine.UI.VertexHelper vh, int sector, float inner, float outer, Color color)
        {
            float a0 = sector * 18f - 9f + .6f;
            float a1 = sector * 18f + 9f - .6f;
            Vector2 p0 = Polar(a0, inner), p1 = Polar(a1, inner), p2 = Polar(a1, outer), p3 = Polar(a0, outer);
            int start = vh.currentVertCount;
            AddVertex(vh, p0, color); AddVertex(vh, p1, color); AddVertex(vh, p2, color); AddVertex(vh, p3, color);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        static void Disc(UnityEngine.UI.VertexHelper vh, Vector2 center, float radius, Color color)
        {
            const int count = 32;
            int centerIndex = vh.currentVertCount;
            AddVertex(vh, center, color);
            for (int i = 0; i <= count; i++) AddVertex(vh, center + Polar(i * 360f / count, radius), color);
            for (int i = 0; i < count; i++) vh.AddTriangle(centerIndex, centerIndex + i + 1, centerIndex + i + 2);
        }

        static void Line(UnityEngine.UI.VertexHelper vh, Vector2 from, Vector2 to, float width, Color color)
        {
            Vector2 direction = to - from;
            if (direction.sqrMagnitude < .0001f) return;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * width * .5f;
            int start = vh.currentVertCount;
            AddVertex(vh, from - normal, color); AddVertex(vh, from + normal, color);
            AddVertex(vh, to + normal, color); AddVertex(vh, to - normal, color);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        static void Circle(UnityEngine.UI.VertexHelper vh, Vector2 center, float radius, float width, Color color)
        {
            const int count = 48;
            for (int i = 0; i < count; i++)
            {
                Vector2 a = center + Polar(i * 360f / count, radius);
                Vector2 b = center + Polar((i + 1) * 360f / count, radius);
                Line(vh, a, b, width, color);
            }
        }

        static void AddVertex(UnityEngine.UI.VertexHelper vh, Vector2 position, Color color)
        {
            var vertex = UnityEngine.UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vh.AddVert(vertex);
        }
    }
}
