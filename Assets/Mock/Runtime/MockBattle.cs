using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace DartsRoguelike.Mock
{
    public sealed class MockBattle : MonoBehaviour
    {
        internal TMP_FontAsset SharedFont { get { return font; } }
        internal Material SharedUIMaterial { get { return uiMaterial; } }
        internal int SharedSeed { get { return startingSeed; } }
        public BattleModel Model { get; private set; }
        public bool Aiming { get; private set; }
        public Vector2 Aim { get; private set; }
        public float Spread { get; private set; }
        public bool HasHit { get; private set; }
        public Vector2 LastHit { get; private set; }

        /// <summary> Mock3で中心点を決めている段階かどうか。 </summary>
        public bool IsThreeStageDrawing { get { return _threeStageAim != null && _threeStageAim.IsDrawing; } }

        /// <summary> Mock3で着弾範囲を決めている段階かどうか。 </summary>
        public bool IsThreeStageSelectingSpread { get { return _threeStageAim != null && _threeStageAim.IsSelectingSpread; } }

        /// <summary> Mock3で固定した大まかな狙いの中心。 </summary>
        public Vector2 CoarseAim { get { return _threeStageAim == null ? Vector2.zero : _threeStageAim.CoarseAim; } }

        /// <summary> Mock3で表示する大まかな狙い範囲の半径。 </summary>
        public float CoarseAimRadius { get { return _threeStageAim == null ? 0f : _threeStageAim.CoarseRadius; } }

        /// <summary> Mock3の照準を表示できる状態かどうか。 </summary>
        public bool ShowsThreeStageAim
        {
            get
            {
                return _threeStageAim != null && Model != null && Model.Phase == BattlePhase.Player &&
                    Model.Remaining > 0 && !Model.Used[selected] &&
                    (_threeStageAim.IsInProgress || _isPointerOverBoard);
            }
        }

        [SerializeField] TMP_FontAsset font;
        [SerializeField] Material uiMaterial;
        [SerializeField] int startingSeed = 731;
        [SerializeField, Tooltip("Mock3で固定する大まかな狙い範囲の半径。")] private float _coarseAimRadius = .5f;
        [SerializeField, Tooltip("Mock3で中心点が通常時に揺れる幅。")] private float _drawSwayRadius = .16f;
        [SerializeField, Tooltip("Mock3で震え状態の中心点へ追加する揺れ幅。")] private float _tremorSwayRadius = .06f;
        readonly TextMeshProUGUI[,] cells = new TextMeshProUGUI[20,4];
        readonly TextMeshProUGUI[] hazards = new TextMeshProUGUI[20];
        readonly UnityEngine.UI.Button[] darts = new UnityEngine.UI.Button[3];
        TextMeshProUGUI player, enemy, intent, turn, result, history, hover, status, aimHelp, bull;
        TextMeshProUGUI targetScore, targetProgress, targetReward, targetHint;
        UnityEngine.UI.Image hpFill, enemyFill;
        UnityEngine.UI.Button end;
        DartBoardGraphic board, overlay;
        RectTransform canvasRoot;
        int selected;
        float started;
        System.Random random;
        private MockThreeStageAim _threeStageAim;
        private bool _isPointerOverBoard;

        static readonly Color Ink = new Color(.92f,.94f,.95f);
        static readonly Color Muted = new Color(.63f,.69f,.75f);
        static readonly Color Accent = new Color(.94f,.76f,.39f);
        static readonly string[] DartNames = { "01   牙のダーツ", "02   守りのダーツ", "03   毒のダーツ" };
        static readonly string[] DartEffects = { "命中時：追加ダメージ3", "命中時：防御4を獲得", "命中時：毒2を付与" };
        static readonly string[] CardSymbols = { "攻", "防", "回", "毒", "吸", "金", "" };

        void Awake()
        {
            if (SceneManager.GetActiveScene().name == "MockBattle2")
            {
                gameObject.AddComponent<MockBattle2>();
                enabled = false;
                return;
            }
            if (SceneManager.GetActiveScene().name == "MockBattle3")
                _threeStageAim = new MockThreeStageAim(_coarseAimRadius, _drawSwayRadius, _tremorSwayRadius);
            BuildUI();
            Restart();
        }
        public void Restart()
        {
            Model = new BattleModel(startingSeed);
            random = new System.Random(startingSeed + 1);
            selected=0; Aiming=false; HasHit=false;
            if (_threeStageAim != null)
            {
                _threeStageAim.Reset();
                PrepareMock3FirstTurnIntent();
            }
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
            var t=Text("Label",image.transform,12,8,w-24,h-16,label,16,Ink);
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
            Text("Kicker",canvasRoot,36,22,600,24,"ダーツ × ローグライク　・　戦闘モック",16,Accent);
            Text("Title",canvasRoot,34,51,800,56,"運命のダーツ盤",38,Ink);
            turn=Text("Turn",canvasRoot,1070,40,330,50,"",23,Accent);
            Panel("Rule",canvasRoot,36,115,1368,2,new Color(.23f,.28f,.33f));
            Panel("BoardPanel",canvasRoot,28,134,820,630,new Color(.065f,.086f,.12f));
            Text("BoardCaption",canvasRoot,52,145,720,28,"マス別カード　／　トリプルは左右も発動　／　ダブルは2倍",17,Muted);
            Text("BoardLegend",canvasRoot,52,224,108,180,"攻 攻撃\n防 防御\n回 回復\n毒 毒\n吸 吸収\n金 コイン\n暗色 空",14,Muted);
            var br=Rect("Dartboard",canvasRoot,170,210,540,540);
            br.pivot=new Vector2(.5f,.5f); br.anchoredPosition=new Vector2(440,-480);
            board=br.gameObject.AddComponent<DartBoardGraphic>(); board.Battle=this;board.material=uiMaterial;
            for(int i=0;i<20;i++)
            {
                float a=i*18*Mathf.Deg2Rad;
                float sx=440+Mathf.Sin(a)*292, sy=480-Mathf.Cos(a)*292;
                var n=Text("Number"+i,canvasRoot,sx-22,sy-16,44,32,BattleModel.Numbers[i].ToString(),21,Ink);
                n.alignment=TextAlignmentOptions.Center;
                float[] rings = { .35f, .59f, .78f, .97f };
                int[] sizes = { 13, 12, 15, 12 };
                for(int band=0;band<4;band++)
                {
                    sx=440+Mathf.Sin(a)*270*rings[band];
                    sy=480-Mathf.Cos(a)*270*rings[band];
                    cells[i,band]=Text("Cell"+i+"_"+band,canvasRoot,sx-12,sy-12,24,24,"",sizes[band],Color.white);
                    cells[i,band].alignment=TextAlignmentOptions.Center;
                }
                sx=440+Mathf.Sin(a)*240; sy=480-Mathf.Cos(a)*240;
                hazards[i]=Text("Hazard"+i,canvasRoot,sx-9,sy-10,18,20,"",14,new Color(1,.72f,.68f));
                hazards[i].alignment=TextAlignmentOptions.Center;
            }
            bull=Text("BullCard",canvasRoot,430,470,20,20,"",12,Ink);
            bull.alignment=TextAlignmentOptions.Center;
            var or=Rect("AimOverlay",canvasRoot,170,210,540,540);
            or.pivot=new Vector2(.5f,.5f);or.anchoredPosition=new Vector2(440,-480);
            overlay=or.gameObject.AddComponent<DartBoardGraphic>(); overlay.Battle=this;overlay.material=uiMaterial;overlay.Overlay=true;overlay.raycastTarget=false;

            Panel("EnemyPanel",canvasRoot,870,134,540,210,new Color(.12f,.075f,.095f));
            Text("EnemyName",canvasRoot,894,152,470,42,"闘技場の番人",27,Ink);
            enemy=Text("EnemyHP",canvasRoot,894,195,470,30,"",19,Muted);
            Panel("EnemyBar",canvasRoot,894,235,488,10,new Color(.23f,.16f,.20f));
            enemyFill=Panel("EnemyFill",canvasRoot,894,235,488,10,new Color(.9f,.31f,.35f));
            intent=Text("Intents",canvasRoot,894,258,480,82,"",17,Ink);
            Panel("PlayerPanel",canvasRoot,870,360,540,145,new Color(.07f,.115f,.15f));
            player=Text("Player",canvasRoot,894,378,490,36,"",22,Ink);
            Panel("HPBar",canvasRoot,894,420,488,10,new Color(.14f,.24f,.27f));
            hpFill=Panel("HPFill",canvasRoot,894,420,488,10,new Color(.29f,.74f,.59f));
            status=Text("Statuses",canvasRoot,894,444,485,50,"",16,Accent);
            Panel("TargetPanel",canvasRoot,870,514,540,126,new Color(.16f,.135f,.08f));
            Panel("TargetAccent",canvasRoot,870,514,4,126,Accent);
            targetScore=Text("TargetScore",canvasRoot,894,520,215,34,"",25,Accent);
            targetProgress=Text("TargetProgress",canvasRoot,1110,524,276,30,"",16,Ink);
            targetProgress.alignment=TextAlignmentOptions.Right;
            targetReward=Text("TargetReward",canvasRoot,894,557,490,36,"",19,Ink);
            targetHint=Text("TargetHint",canvasRoot,894,607,490,25,"",14,Muted);
            hover=Text("CardDetails",canvasRoot,888,650,505,54,"",17,Ink);
            history=Text("CombatLog",canvasRoot,888,711,510,53,"",14,Muted);
            aimHelp=Text("AimHelp",canvasRoot,52,791,780,26,"",15,Accent);
            for(int i=0;i<3;i++)
            {
                int index=i;
                darts[i]=Button("Dart"+i,36+i*270,818,255,65,DartNames[i]+"\n"+DartEffects[i],()=>SelectDart(index));
            }
            end=Button("EndTurn",870,769,260,48,"ターン終了",()=>EndTurn());
            Button("Restart",1144,769,260,48,"再戦",Restart);
            result=Text("Result",canvasRoot,878,836,520,45,"",20,Accent);
        }
        public void SelectDart(int index)
        {
            if (Model.Phase != BattlePhase.Player || Model.Used[index] || Aiming) return;
            selected=index; Refresh();
        }
        public void BeginAim()
        {
            if (Model.Phase != BattlePhase.Player || Model.Remaining==0 || Model.Used[selected]) return;
            if (_threeStageAim != null)
            {
                AdvanceThreeStageAim();
                return;
            }
            Aiming=true;started=Time.unscaledTime;UpdateAim();
        }
        public void ReleaseAim()
        {
            if (_threeStageAim != null) return;
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
        void OnApplicationFocus(bool focused) { if(!focused) CancelAim(); }
        void OnDisable() { CancelAim(); }
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
            Spread=.045f+wave*.27f+(Model.Tremor>0?.04f:0);
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
                if(Keyboard.current.escapeKey.wasPressedThisFrame) CancelAim();
            }
            if (_threeStageAim != null)
            {
                UpdateThreeStageAim();
                overlay.SetVerticesDirty();
                aimHelp.text = GetThreeStageAimHelp();
            }
            else if(Aiming) { UpdateAim();overlay.SetVerticesDirty();aimHelp.text="照準中：円が小さいほど高精度　／　Escで中止"; }
            if(Mouse.current!=null)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(board.rectTransform,Mouse.current.position.ReadValue(),null,out local);
                Hit hit=BattleModel.Evaluate(local.x/270,local.y/270);
                if(!hit.Miss)
                {
                    int band=hit.Sector==20?0:(int)hit.Band;
                    Sector current=Model.Board[hit.Sector];
                    CardKind card=current.Cards[band];
                    int factor=hit.Sector<20&&hit.Band==BoardBand.Triple?1:hit.Multiplier;
                    hover.text=hit.Score+"点　"+CardDetail(card,factor);
                    if(hit.Sector<20&&hit.Band==BoardBand.Triple)
                    {
                        int left=(hit.Sector+19)%20, right=(hit.Sector+1)%20;
                        hover.text+="\n左右も発動："+BattleModel.Numbers[left]+CardDetail(Model.Board[left].Cards[1],1)+
                            " / "+BattleModel.Numbers[right]+CardDetail(Model.Board[right].Cards[1],1);
                        string danger="";
                        foreach(int index in new[]{left,hit.Sector,right})
                            if(Model.Board[index].Hazard!=HazardKind.None)
                                danger+=BattleModel.Numbers[index]+HazardName(Model.Board[index].Hazard)+" ";
                        if(danger.Length>0)hover.text+="\n危険："+danger;
                    }
                    else hover.text+="\n"+HazardDescription(current);
                }
                else hover.text="暗いマスは空でも得点は入る。\nトリプルは左右も発動 / ダブルはカード2倍";
            }
        }
        /// <summary>
        ///     Mock3の現在の照準を確定し、投擲まで進んだ場合は戦闘へ着弾を渡す。
        /// </summary>
        private void AdvanceThreeStageAim()
        {
            Vector2 pointerPosition;
            if (!TryGetBoardPointer(out pointerPosition)) return;

            bool shouldThrow = _threeStageAim.Advance(
                pointerPosition,
                Time.unscaledTime,
                Model.Tremor > 0,
                Model.Drunk > 0,
                Model.Panic > 0);
            ApplyThreeStageAim();
            Aiming = _threeStageAim.IsInProgress;

            if (!shouldThrow)
            {
                overlay.SetVerticesDirty();
                return;
            }

            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float distance = Mathf.Sqrt((float)random.NextDouble()) * Spread;
            LastHit = Aim + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            HasHit = true;
            Model.Throw(BattleModel.Evaluate(LastHit.x, LastHit.y), selected);
            for (int i = 0; i < 3; i++)
            {
                if (!Model.Used[i])
                {
                    selected = i;
                    break;
                }
            }

            _threeStageAim.Reset();
            Aiming = false;
            Refresh();
        }

        /// <summary>
        ///     Mock3の照準表示を現在のマウス位置と状態異常から更新する。
        /// </summary>
        private void UpdateThreeStageAim()
        {
            Vector2 pointerPosition;
            if (!TryGetBoardPointer(out pointerPosition)) return;

            _isPointerOverBoard = pointerPosition.sqrMagnitude <= 1f;
            if (!_threeStageAim.IsInProgress && !_isPointerOverBoard) return;

            _threeStageAim.Update(
                pointerPosition,
                Time.unscaledTime,
                Model.Tremor > 0,
                Model.Drunk > 0,
                Model.Panic > 0);
            ApplyThreeStageAim();
        }

        /// <summary>
        ///     3段階照準の計算結果を既存の盤面表示へ反映する。
        /// </summary>
        private void ApplyThreeStageAim()
        {
            Aim = _threeStageAim.Aim;
            Spread = _threeStageAim.Spread;
        }

        /// <summary>
        ///     マウス位置を盤面半径1の正規化座標へ変換する。
        /// </summary>
        private bool TryGetBoardPointer(out Vector2 pointerPosition)
        {
            pointerPosition = Vector2.zero;
            if (Mouse.current == null) return false;

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                board.rectTransform,
                Mouse.current.position.ReadValue(),
                null,
                out local);
            pointerPosition = local / (board.rectTransform.rect.width * .5f);
            return true;
        }

        /// <summary>
        ///     進行中の1投を取り消して最初の照準段階へ戻す。
        /// </summary>
        private void CancelAim()
        {
            Aiming = false;
            _isPointerOverBoard = false;
            if (_threeStageAim != null) _threeStageAim.Reset();
            if (overlay != null) overlay.SetVerticesDirty();
        }

        /// <summary>
        ///     Mock3の現在の入力段階に対応する操作説明を取得する。
        /// </summary>
        private string GetThreeStageAimHelp()
        {
            if (Model.Phase != BattlePhase.Player)
                return "戦闘終了。「再戦」で最初から遊べます。";
            if (Model.Remaining == 0)
                return "残り0本。「ターン終了」で敵が行動します。";
            if (_threeStageAim.IsDrawing)
                return "2 / 3 引く：大きな円の中で揺れる中心を左クリックで確定　／　Escで中止";
            if (_threeStageAim.IsSelectingSpread)
                return "3 / 3 投げる：金色の円が小さい瞬間を左クリックして投擲　／　Escで中止";
            return "1 / 3 狙う：マウスで大きな円を動かし、左クリックで範囲を確定";
        }

        /// <summary>
        ///     Mock3で通常時と震え時を続けて比較できるよう、初手の状態異常を震えへ変更する。
        /// </summary>
        private void PrepareMock3FirstTurnIntent()
        {
            for (int i = 0; i < Model.Intents.Count; i++)
            {
                if (Model.Intents[i].Kind == IntentKind.Panic)
                {
                    Model.Intents[i] = new Intent(IntentKind.Tremor, 2);
                    return;
                }
            }
        }
        static string CardDetail(CardKind card,int multiplier)
        {
            return card==CardKind.Empty?"空マス":
                BattleModel.CardNames[(int)card]+" "+(BattleModel.CardValues[(int)card]*multiplier);
        }
        static string HazardName(HazardKind hazard)
        {
            switch(hazard)
            {
                case HazardKind.Mine:return "地雷";
                case HazardKind.Fire:return "炎";
                case HazardKind.Curse:return "呪い";
                default:return "";
            }
        }
        static string HazardDescription(Sector s)
        {
            switch(s.Hazard)
            {
                case HazardKind.Mine:return "地雷：9ダメージ、1回で消滅";
                case HazardKind.Fire:return "炎：5ダメージ、残り"+s.Turns+"ターン";
                case HazardKind.Curse:return "呪い：命中ごとに4ダメージ、永続";
                default:return "安全なマス　／　命中後にカードを再抽選";
            }
        }
        public void Refresh()
        {
            board.SetVerticesDirty();overlay.SetVerticesDirty();
            for(int i=0;i<20;i++)
            {
                Sector sector=Model.Board[i];
                for(int band=0;band<4;band++)
                    cells[i,band].text=CardSymbols[(int)sector.Cards[band]];
                hazards[i].text=sector.Hazard==HazardKind.None?"":"!";
            }
            bull.text=CardSymbols[(int)Model.Board[20].Cards[0]];
            turn.text="ターン "+Model.Turn+"    /    "+Model.Remaining+"本";
            player.text="自分の体力  "+Model.Hp+" / "+Model.MaxHp+"　防御 "+Model.Shield;
            enemy.text="体力 "+Model.EnemyHp+" / "+Model.EnemyMaxHp+"　毒 "+Model.Poison;
            hpFill.rectTransform.sizeDelta=new Vector2(488f*Model.Hp/Model.MaxHp,10);
            enemyFill.rectTransform.sizeDelta=new Vector2(488f*Model.EnemyHp/Model.EnemyMaxHp,10);
            intent.text="";for(int i=0;i<Model.Intents.Count;i++)intent.text+=(i+1)+". "+Model.Intents[i]+"\n";
            status.text="焦り "+Model.Panic+"　震え "+Model.Tremor+"　酔い "+Model.Drunk+"\nコイン "+Model.Coins+"　累計スコア "+Model.Score;
            targetScore.text="目標 "+Model.TargetScore+"点";
            targetProgress.text="合計 "+Model.TurnScore+"点　"+(Model.TargetAchieved?"達成！":Model.TurnScore>Model.TargetScore?"超過":"あと "+(Model.TargetScore-Model.TurnScore)+"点");
            targetReward.text="達成報酬　"+Model.TargetRewardDescription;
            targetHint.text=Model.TargetAchieved?"発動済み。次のターンに新しい目標":Model.TurnScore>Model.TargetScore?"超過しても通常効果は有効。次のターンに再抽選":"このターンの3本以内にちょうど一致で即発動";
            history.text=string.Join("\n",Model.Log.GetRange(0,Math.Min(3,Model.Log.Count)));
            for(int i=0;i<3;i++)
            {
                darts[i].interactable=Model.Phase==BattlePhase.Player&&!Model.Used[i];
                darts[i].GetComponent<UnityEngine.UI.Image>().color=selected==i?new Color(.37f,.29f,.13f):new Color(.16f,.21f,.27f);
            }
            end.interactable=Model.Phase==BattlePhase.Player;
            result.text=Model.Phase==BattlePhase.Victory?"勝利！　獲得コイン： "+Model.Coins+"枚":Model.Phase==BattlePhase.Defeat?"敗北…　狙い方を変えて再挑戦":"1／2／3：選択　Space：ターン終了";
            aimHelp.text=_threeStageAim!=null?GetThreeStageAimHelp():Model.Phase!=BattlePhase.Player?"戦闘終了。「再戦」で最初から遊べます。":Model.Remaining==0?"残り0本。「ターン終了」で敵が行動します。":"盤面を左クリックで長押しし、円が小さいときに離そう。";
        }
    }
}
