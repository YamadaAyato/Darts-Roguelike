using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace DartsRoguelike.Mock
{
    public sealed class MockBattle : MonoBehaviour
    {
        public BattleModel Model { get; private set; }
        public bool Aiming { get; private set; }
        public Vector2 Aim { get; private set; }
        public float Spread { get; private set; }
        public bool HasHit { get; private set; }
        public Vector2 LastHit { get; private set; }
        [SerializeField] TMP_FontAsset font;
        [SerializeField] Material uiMaterial;
        [SerializeField] int startingSeed = 731;
        readonly TextMeshProUGUI[] sectors = new TextMeshProUGUI[20];
        readonly UnityEngine.UI.Button[] darts = new UnityEngine.UI.Button[3];
        TextMeshProUGUI player, enemy, intent, turn, result, history, hover, status, aimHelp, bull;
        UnityEngine.UI.Image hpFill, enemyFill;
        UnityEngine.UI.Button end;
        DartBoardGraphic board, overlay;
        RectTransform canvasRoot;
        int selected;
        float started;
        System.Random random;

        static readonly Color Ink = new Color(.92f,.94f,.95f);
        static readonly Color Muted = new Color(.63f,.69f,.75f);
        static readonly Color Accent = new Color(.94f,.76f,.39f);
        static readonly string[] DartNames = { "01   FANG", "02   AEGIS", "03   VENOM" };
        static readonly string[] DartEffects = { "+3 damage on hit", "+4 block on hit", "+2 poison on hit" };

        void Awake()
        {
            BuildUI();
            Restart();
        }
        public void Restart()
        {
            Model = new BattleModel(startingSeed);
            random = new System.Random(startingSeed + 1);
            selected=0; Aiming=false; HasHit=false;
            Refresh();
        }
        RectTransform Rect(string name, Transform parent, float x,float y,float w,float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent,false);
            rt.anchorMin=rt.anchorMax=new Vector2(0,1);
            rt.pivot=new Vector2(0,1);
            rt.anchoredPosition=new Vector2(x,-y); rt.sizeDelta=new Vector2(w,h);
            return rt;
        }
        UnityEngine.UI.Image Panel(string name,Transform parent,float x,float y,float w,float h,Color color)
        {
            var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color=color; image.material=uiMaterial; image.raycastTarget=false; return image;
        }
        TextMeshProUGUI Text(string name,Transform parent,float x,float y,float w,float h,string value,int size,Color color)
        {
            var text=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();
            text.font=font; text.text=value; text.fontSize=size; text.color=color;
            text.raycastTarget=false; text.textWrappingMode=TextWrappingModes.Normal;
            return text;
        }
        UnityEngine.UI.Button Button(string name,float x,float y,float w,float h,string label,Action click)
        {
            var image=Panel(name,canvasRoot,x,y,w,h,new Color(.16f,.21f,.27f));
            image.raycastTarget=true;
            var b=image.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic=image; b.onClick.AddListener(()=>click());
            var colors=b.colors; colors.highlightedColor=new Color(.8f,.87f,1); colors.selectedColor=Color.white; b.colors=colors;
            var t=Text("Label",image.transform,12,8,w-24,h-16,label,18,Ink);
            t.alignment=TextAlignmentOptions.Midline;
            return b;
        }
        void BuildUI()
        {
            var canvasGo=new GameObject("MockCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.transform.SetParent(transform,false);
            canvasGo.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1440,900);
            scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            canvasRoot=Rect("Frame",canvasGo.transform,0,0,1440,900);
            canvasRoot.anchorMin=canvasRoot.anchorMax=new Vector2(.5f,.5f);
            canvasRoot.pivot=new Vector2(.5f,.5f);
            if (EventSystem.current == null)
            {
                var es=new GameObject("MockEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                es.transform.SetParent(transform,false);
                es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            Panel("Background",canvasRoot,0,0,1440,900,new Color(.035f,.05f,.075f));
            Text("Kicker",canvasRoot,36,22,600,24,"DARTS / ROGUELIKE     •     COMBAT LAB",16,Accent);
            Text("Title",canvasRoot,34,51,800,56,"THE LOADED BOARD",38,Ink);
            turn=Text("Turn",canvasRoot,1070,40,330,50,"",23,Accent);
            Panel("Rule",canvasRoot,36,115,1368,2,new Color(.23f,.28f,.33f));
            Panel("BoardPanel",canvasRoot,28,134,820,630,new Color(.065f,.086f,.12f));
            Text("BoardCaption",canvasRoot,52,149,720,28,"HOLD TO FOCUS   /   RELEASE TO THROW",17,Muted);
            var br=Rect("Dartboard",canvasRoot,170,210,540,540);
            br.pivot=new Vector2(.5f,.5f); br.anchoredPosition=new Vector2(440,-480);
            board=br.gameObject.AddComponent<DartBoardGraphic>(); board.Battle=this;board.material=uiMaterial;
            for(int i=0;i<20;i++)
            {
                float a=i*18*Mathf.Deg2Rad;
                float sx=440+Mathf.Sin(a)*292, sy=480-Mathf.Cos(a)*292;
                var n=Text("Number"+i,canvasRoot,sx-22,sy-16,44,32,BattleModel.Numbers[i].ToString(),21,Ink);
                n.alignment=TextAlignmentOptions.Center;
                sx=440+Mathf.Sin(a)*205; sy=480-Mathf.Cos(a)*205;
                sectors[i]=Text("Card"+i,canvasRoot,sx-36,sy-22,72,50,"",12,Color.white);
                sectors[i].alignment=TextAlignmentOptions.Center;
            }
            bull=Text("BullCard",canvasRoot,354,444,172,22,"",12,Ink);
            bull.alignment=TextAlignmentOptions.Center;
            var or=Rect("AimOverlay",canvasRoot,170,210,540,540);
            or.pivot=new Vector2(.5f,.5f);or.anchoredPosition=new Vector2(440,-480);
            overlay=or.gameObject.AddComponent<DartBoardGraphic>(); overlay.Battle=this;overlay.material=uiMaterial;overlay.Overlay=true;overlay.raycastTarget=false;

            Panel("EnemyPanel",canvasRoot,870,134,540,210,new Color(.12f,.075f,.095f));
            Text("EnemyName",canvasRoot,894,152,470,35,"THE PIT WARDEN",27,Ink);
            enemy=Text("EnemyHP",canvasRoot,894,195,470,30,"",19,Muted);
            Panel("EnemyBar",canvasRoot,894,235,488,10,new Color(.23f,.16f,.20f));
            enemyFill=Panel("EnemyFill",canvasRoot,894,235,488,10,new Color(.9f,.31f,.35f));
            intent=Text("Intents",canvasRoot,894,258,480,82,"",17,Ink);
            Panel("PlayerPanel",canvasRoot,870,360,540,145,new Color(.07f,.115f,.15f));
            player=Text("Player",canvasRoot,894,378,490,30,"",22,Ink);
            Panel("HPBar",canvasRoot,894,420,488,10,new Color(.14f,.24f,.27f));
            hpFill=Panel("HPFill",canvasRoot,894,420,488,10,new Color(.29f,.74f,.59f));
            status=Text("Statuses",canvasRoot,894,444,485,50,"",16,Accent);
            hover=Text("CardDetails",canvasRoot,888,521,505,64,"",18,Ink);
            history=Text("CombatLog",canvasRoot,888,602,510,138,"",15,Muted);
            aimHelp=Text("AimHelp",canvasRoot,52,791,780,22,"",17,Accent);
            for(int i=0;i<3;i++)
            {
                int index=i;
                darts[i]=Button("Dart"+i,36+i*270,818,255,65,DartNames[i]+"\n"+DartEffects[i],()=>SelectDart(index));
            }
            end=Button("EndTurn",870,769,260,48,"END TURN",()=>EndTurn());
            Button("Restart",1144,769,260,48,"RESTART",Restart);
            result=Text("Result",canvasRoot,878,836,520,45,"",23,Accent);
        }
        public void SelectDart(int index)
        {
            if (Model.Phase != BattlePhase.Player || Model.Used[index] || Aiming) return;
            selected=index; Refresh();
        }
        public void BeginAim()
        {
            if (Model.Phase != BattlePhase.Player || Model.Remaining==0 || Model.Used[selected]) return;
            Aiming=true;started=Time.unscaledTime;UpdateAim();
        }
        public void ReleaseAim()
        {
            if(!Aiming) return;
            UpdateAim();
            Aiming=false;
            float angle=(float)random.NextDouble()*Mathf.PI*2;
            float distance=Mathf.Sqrt((float)random.NextDouble())*Spread;
            LastHit=Aim+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*distance;
            HasHit=true;
            Model.Throw(BattleModel.Evaluate(LastHit.x,LastHit.y),selected);
            for(int i=0;i<3;i++) if(!Model.Used[i]) { selected=i; break; }
            Refresh();
        }
        public void EndTurn()
        {
            if(Aiming) return;
            Model.EndTurn(); selected=0;HasHit=false;Refresh();
        }
        void OnApplicationFocus(bool focused) { if(!focused) { Aiming=false;if(overlay!=null)overlay.SetVerticesDirty(); } }
        void OnDisable() { Aiming=false; }
        void UpdateAim()
        {
            if(Mouse.current==null) { Aiming=false;return; }
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(board.rectTransform,Mouse.current.position.ReadValue(),null,out local);
            Aim=local/(board.rectTransform.rect.width*.5f);
            float elapsed=Time.unscaledTime-started;
            if(Model.Tremor>0) Aim+=new Vector2(Mathf.Sin(Time.unscaledTime*23),Mathf.Cos(Time.unscaledTime*29))*.027f;
            if(Model.Drunk>0) Aim+=new Vector2(Mathf.Sin(Time.unscaledTime*1.7f),Mathf.Cos(Time.unscaledTime*1.3f))*.075f;
            float wave=.5f+.5f*Mathf.Cos(elapsed*(Model.Panic>0?6.8f:4.2f));
            Spread=.012f+wave*.18f+(Model.Tremor>0?.015f:0);
        }
        void Update()
        {
            if(Model==null) return;
            if(Keyboard.current!=null)
            {
                if(Keyboard.current.digit1Key.wasPressedThisFrame)SelectDart(0);
                if(Keyboard.current.digit2Key.wasPressedThisFrame)SelectDart(1);
                if(Keyboard.current.digit3Key.wasPressedThisFrame)SelectDart(2);
                if(Keyboard.current.spaceKey.wasPressedThisFrame)EndTurn();
                if(Keyboard.current.escapeKey.wasPressedThisFrame) { Aiming=false;overlay.SetVerticesDirty(); }
            }
            if(Aiming) { UpdateAim();overlay.SetVerticesDirty();aimHelp.text="FOCUS  /  smaller circle = tighter spread   •   ESC cancels"; }
            if(Mouse.current!=null)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(board.rectTransform,Mouse.current.position.ReadValue(),null,out local);
                Hit hit=BattleModel.Evaluate(local.x/270,local.y/270);
                if(!hit.Miss)
                {
                    Sector s=Model.Board[hit.Sector];
                    hover.text=BattleModel.CardNames[(int)s.Card]+"  "+BattleModel.CardValues[(int)s.Card]+" x"+hit.Multiplier+
                        "   /   "+hit.Score+" points\n"+HazardDescription(s);
                }
                else hover.text="Outer ring x2  /  thin middle ring x3\nBull: 25 / 50 points; card x1 / x2";
            }
        }
        static string HazardDescription(Sector s)
        {
            switch(s.Hazard)
            {
                case HazardKind.Mine:return "MINE: 9 damage, consumed on hit";
                case HazardKind.Fire:return "FIRE: 5 damage, "+s.Turns+" turns remaining";
                case HazardKind.Curse:return "CURSE: 4 damage on every hit, permanent";
                default:return "Safe sector  /  hit rerolls this card";
            }
        }
        public void Refresh()
        {
            board.SetVerticesDirty();overlay.SetVerticesDirty();
            for(int i=0;i<20;i++)
            {
                Sector s=Model.Board[i];
                string hazard=s.Hazard==HazardKind.None?"":s.Hazard==HazardKind.Mine?"\n! MINE":s.Hazard==HazardKind.Fire?"\n! FIRE "+s.Turns:"\n! CURSE";
                sectors[i].text=BattleModel.CardNames[(int)s.Card]+"\n"+BattleModel.CardValues[(int)s.Card]+hazard;
            }
            bull.text=BattleModel.CardNames[(int)Model.Board[20].Card];
            turn.text="TURN "+Model.Turn+"    /    "+Model.Remaining+" DARTS";
            player.text="YOU  "+Model.Hp+" / "+Model.MaxHp+"     BLOCK "+Model.Shield;
            enemy.text="HP "+Model.EnemyHp+" / "+Model.EnemyMaxHp+"     POISON "+Model.Poison;
            hpFill.rectTransform.sizeDelta=new Vector2(488f*Model.Hp/Model.MaxHp,10);
            enemyFill.rectTransform.sizeDelta=new Vector2(488f*Model.EnemyHp/Model.EnemyMaxHp,10);
            intent.text="";for(int i=0;i<Model.Intents.Count;i++)intent.text+=(i+1)+". "+Model.Intents[i]+"\n";
            status.text="PANIC "+Model.Panic+"   TREMOR "+Model.Tremor+"   DRUNK "+Model.Drunk+"\nCOINS "+Model.Coins+"    SCORE "+Model.Score;
            history.text=string.Join("\n",Model.Log);
            for(int i=0;i<3;i++)
            {
                darts[i].interactable=Model.Phase==BattlePhase.Player&&!Model.Used[i];
                darts[i].GetComponent<UnityEngine.UI.Image>().color=selected==i?new Color(.37f,.29f,.13f):new Color(.16f,.21f,.27f);
            }
            end.interactable=Model.Phase==BattlePhase.Player;
            result.text=Model.Phase==BattlePhase.Victory?"VICTORY  /  "+Model.Coins+" coins":Model.Phase==BattlePhase.Defeat?"DEFEAT  /  try a different approach":"1 / 2 / 3: select   SPACE: end turn";
            aimHelp.text=Model.Phase!=BattlePhase.Player?"Battle finished. RESTART to play again.":Model.Remaining==0?"No darts left. END TURN to resolve the enemy's intent.":"Hold left mouse on board; release when the circle is small.";
        }
    }
}
