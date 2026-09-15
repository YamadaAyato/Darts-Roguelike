using UnityEngine;
using UnityEngine.EventSystems;

namespace DartsRoguelike.Mock
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DartBoardGraphic : UnityEngine.UI.MaskableGraphic, IPointerDownHandler, IPointerUpHandler
    {
        public MockBattle Battle;
        public bool Overlay;
        public static readonly Color[] Palette = {
            new Color(.91f,.31f,.29f), new Color(.22f,.59f,.85f), new Color(.27f,.73f,.53f),
            new Color(.65f,.42f,.84f), new Color(.89f,.57f,.29f), new Color(.93f,.78f,.35f)
        };
        public void OnPointerDown(PointerEventData e) { if (!Overlay && e.button == PointerEventData.InputButton.Left) Battle.BeginAim(); }
        public void OnPointerUp(PointerEventData e) { if (!Overlay && e.button == PointerEventData.InputButton.Left) Battle.ReleaseAim(); }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            if (Battle == null || Battle.Model == null) return;
            float radius = rectTransform.rect.width * .5f;
            if (Overlay)
            {
                if (Battle.Aiming)
                {
                    Ring(vh, Battle.Aim * radius, Battle.Spread * radius, 2, new Color(1,.86f,.47f));
                    Ring(vh, Battle.Aim * radius, 5, 1.5f, Color.white);
                }
                if (Battle.HasHit)
                {
                    Vector2 p = Battle.LastHit * radius;
                    Ring(vh,p,7,3,Color.white);
                    Quad(vh,p+new Vector2(-1,-13),p+new Vector2(1,13),Color.white);
                    Quad(vh,p+new Vector2(-13,-1),p+new Vector2(13,1),Color.white);
                }
                return;
            }
            for (int i=0;i<20;i++)
            {
                Color c = Palette[(int)Battle.Model.Board[i].Card];
                Wedge(vh, i, .0935f*radius, .56f*radius, c*.65f);
                Wedge(vh, i, .56f*radius, .62f*radius, c);
                Wedge(vh, i, .62f*radius, .94f*radius, c*.65f);
                Wedge(vh, i, .94f*radius, radius, c);
                if (Battle.Model.Board[i].Hazard != HazardKind.None)
                    Wedge(vh,i,.88f*radius,.915f*radius,new Color(1,.25f,.23f));
            }
            Color bull = Palette[(int)Battle.Model.Board[20].Card];
            Disc(vh,Vector2.zero,radius*.0935f,bull*.8f);
            Disc(vh,Vector2.zero,radius*.0375f,bull);
        }
        static Vector2 Polar(float angle, float radius)
        { float a=angle*Mathf.Deg2Rad; return new Vector2(Mathf.Sin(a),Mathf.Cos(a))*radius; }
        static void Triangle(UnityEngine.UI.VertexHelper vh, Vector2 a,Vector2 b,Vector2 c,Color color)
        {
            color.a=1; int n=vh.currentVertCount;
            vh.AddVert(a,color,Vector2.zero); vh.AddVert(b,color,Vector2.zero); vh.AddVert(c,color,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);
        }
        static void Wedge(UnityEngine.UI.VertexHelper vh,int sector,float inner,float outer,Color color)
        {
            float start=sector*18-8.65f;
            for(int j=0;j<8;j++)
            {
                float a=start+j*17.3f/8, b=start+(j+1)*17.3f/8;
                Vector2 p=Polar(a,inner),q=Polar(a,outer),r=Polar(b,outer),s=Polar(b,inner);
                Triangle(vh,p,q,r,color); Triangle(vh,p,r,s,color);
            }
        }
        static void Disc(UnityEngine.UI.VertexHelper vh,Vector2 center,float radius,Color color)
        { for(int i=0;i<64;i++) Triangle(vh,center,center+Polar(i*360f/64,radius),center+Polar((i+1)*360f/64,radius),color); }
        static void Ring(UnityEngine.UI.VertexHelper vh,Vector2 center,float radius,float width,Color color)
        {
            for(int i=0;i<64;i++)
            {
                float a=i*360f/64,b=(i+1)*360f/64;
                Vector2 p=center+Polar(a,Mathf.Max(0,radius-width)),q=center+Polar(a,radius);
                Vector2 r=center+Polar(b,radius),s=center+Polar(b,Mathf.Max(0,radius-width));
                Triangle(vh,p,q,r,color);Triangle(vh,p,r,s,color);
            }
        }
        static void Quad(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,Color color)
        { Triangle(vh,a,new Vector2(a.x,b.y),b,color);Triangle(vh,a,b,new Vector2(b.x,a.y),color); }
    }
}
