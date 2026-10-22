using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace DartsRoguelike.Mock
{
    public sealed class MockBattle2 : MonoBehaviour
    {
        public LinkBattleModel Model { get; private set; }
        public bool Aiming { get; private set; }
        public Vector2 Aim { get; private set; }
        public float Spread { get; private set; }
        public bool HasHit { get; private set; }
        public Vector2 LastHit { get; private set; }

        TMP_FontAsset font;
        Material uiMaterial;
        int startingSeed;
        RectTransform canvasRoot;
        LinkBoardGraphic board, overlay;
        readonly TextMeshProUGUI[,] cells = new TextMeshProUGUI[20, 4];
        readonly TextMeshProUGUI[] danger = new TextMeshProUGUI[20];
        readonly UnityEngine.UI.Button[] darts = new UnityEngine.UI.Button[3];
        TextMeshProUGUI player, enemy, turn, target, route, hover, history, result, help, bull;
        UnityEngine.UI.Image playerFill, enemyFill;
        UnityEngine.UI.Button end;
        int selected;
        float started;
        System.Random aimRandom;

        static readonly Color Ink = new Color(.92f,.94f,.95f);
        static readonly Color Muted = new Color(.62f,.69f,.75f);
        static readonly Color Cyan = new Color(.25f,.84f,1f);
        static readonly Color Gold = new Color(1f,.76f,.27f);
        static readonly string[] Symbols = { "攻", "防", "回", "毒", "吸", "金", "" };
        static readonly string[] DartNames = { "01 牙のダーツ", "02 守りのダーツ", "03 毒のダーツ" };
        static readonly string[] DartEffects = { "命中：追加攻撃3", "命中：防御4", "命中：毒2" };

        void Awake()
        {
            MockBattle source = GetComponent<MockBattle>();
            if (source == null) throw new InvalidOperationException("MockBattle2 requires the copied MockBattle scene root.");
            font = source.SharedFont;
            uiMaterial = source.SharedUIMaterial;
            startingSeed = source.SharedSeed + 2000;
            BuildUI();
            Restart();
        }

        public void Restart()
        {
            Model = new LinkBattleModel(startingSeed);
            aimRandom = new System.Random(startingSeed + 1);
            selected = 0;
            Aiming = false;
            HasHit = false;
            Refresh();
        }

        RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        UnityEngine.UI.Image Panel(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            var image = Rect(name, parent, x, y, width, height).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.material = uiMaterial;
            image.raycastTarget = false;
            return image;
        }

        TextMeshProUGUI Text(string name, Transform parent, float x, float y, float width, float height, string value, int size, Color color)
        {
            var text = Rect(name, parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        UnityEngine.UI.Button Button(string name, float x, float y, float width, float height, string label, Action action)
        {
            UnityEngine.UI.Image image = Panel(name, canvasRoot, x, y, width, height, new Color(.15f,.20f,.27f));
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            var colors = button.colors;
            colors.highlightedColor = new Color(.32f,.45f,.58f);
            colors.selectedColor = new Color(.38f,.48f,.58f);
            button.colors = colors;
            TextMeshProUGUI text = Text("Label", image.transform, 10, 7, width - 20, height - 14, label, 15, Ink);
            text.alignment = TextAlignmentOptions.Midline;
            return button;
        }

        void BuildUI()
        {
            var canvasObject = new GameObject("MockBattle2Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            canvasRoot = Rect("Frame", canvasObject.transform, 0, 0, 1440, 900);
            canvasRoot.anchorMin = canvasRoot.anchorMax = new Vector2(.5f,.5f);
            canvasRoot.pivot = new Vector2(.5f,.5f);

            if (EventSystem.current == null)
            {
                var eventSystem = new GameObject("MockBattle2EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventSystem.transform.SetParent(transform, false);
                eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            Panel("Background", canvasRoot, 0, 0, 1440, 900, new Color(.027f,.043f,.065f));
            Text("Kicker", canvasRoot, 36, 20, 700, 26, "ダーツ × ローグライク　・　線をつなぐ戦闘モック", 16, Gold);
            Text("Title", canvasRoot, 34, 48, 820, 58, "軌跡を描くダーツ盤", 37, Ink);
            turn = Text("Turn", canvasRoot, 1080, 42, 320, 40, "", 22, Gold);
            Panel("Rule", canvasRoot, 36, 112, 1368, 2, new Color(.23f,.29f,.34f));

            Panel("BoardPanel", canvasRoot, 28, 130, 828, 640, new Color(.045f,.075f,.105f));
            Text("BoardCaption", canvasRoot, 52, 144, 770, 50,
                "1投目で起点　／　2投目から線が通ったマスを発動　／　3投目の線は効果2倍", 16, Muted);
            Text("Legend", canvasRoot, 50, 220, 110, 205,
                "攻 攻撃\n防 防御\n回 回復\n毒 毒\n吸 吸収\n金 コイン\n赤 罠\n暗 空", 14, Muted);

            RectTransform boardRect = Rect("LinkBoard", canvasRoot, 172, 204, 560, 560);
            boardRect.pivot = new Vector2(.5f,.5f);
            boardRect.anchoredPosition = new Vector2(450, -484);
            board = boardRect.gameObject.AddComponent<LinkBoardGraphic>();
            board.Battle = this;
            board.material = uiMaterial;

            for (int sector = 0; sector < 20; sector++)
            {
                float angle = sector * 18f * Mathf.Deg2Rad;
                float x = 450 + Mathf.Sin(angle) * 303f;
                float y = 484 - Mathf.Cos(angle) * 303f;
                TextMeshProUGUI number = Text("Number" + sector, canvasRoot, x - 22, y - 16, 44, 32,
                    BattleModel.Numbers[sector].ToString(), 20, Ink);
                number.alignment = TextAlignmentOptions.Center;

                float[] rings = { .35f, .59f, .78f, .97f };
                int[] sizes = { 13, 11, 14, 11 };
                for (int band = 0; band < 4; band++)
                {
                    x = 450 + Mathf.Sin(angle) * 280f * rings[band];
                    y = 484 - Mathf.Cos(angle) * 280f * rings[band];
                    cells[sector,band] = Text("Cell" + sector + "_" + band, canvasRoot,
                        x - 12, y - 12, 24, 24, "", sizes[band], Color.white);
                    cells[sector,band].alignment = TextAlignmentOptions.Center;
                }
                x = 450 + Mathf.Sin(angle) * 247f;
                y = 484 - Mathf.Cos(angle) * 247f;
                danger[sector] = Text("Danger" + sector, canvasRoot, x - 8, y - 10, 16, 20, "", 14, Color.white);
                danger[sector].alignment = TextAlignmentOptions.Center;
            }
            bull = Text("Bull", canvasRoot, 440, 474, 20, 20, "", 12, Ink);
            bull.alignment = TextAlignmentOptions.Center;

            RectTransform overlayRect = Rect("LinkOverlay", canvasRoot, 172, 204, 560, 560);
            overlayRect.pivot = new Vector2(.5f,.5f);
            overlayRect.anchoredPosition = new Vector2(450, -484);
            overlay = overlayRect.gameObject.AddComponent<LinkBoardGraphic>();
            overlay.Battle = this;
            overlay.Overlay = true;
            overlay.material = uiMaterial;
            overlay.raycastTarget = false;

            Panel("EnemyPanel", canvasRoot, 874, 130, 536, 146, new Color(.115f,.07f,.09f));
            Text("EnemyName", canvasRoot, 896, 148, 470, 34, "線喰らいの番人", 25, Ink);
            enemy = Text("Enemy", canvasRoot, 896, 187, 470, 28, "", 18, Muted);
            Panel("EnemyBar", canvasRoot, 896, 226, 486, 10, new Color(.23f,.15f,.19f));
            enemyFill = Panel("EnemyFill", canvasRoot, 896, 226, 486, 10, new Color(.91f,.28f,.34f));
            Text("EnemyIntent", canvasRoot, 896, 242, 480, 24, "予告：ターン終了時に攻撃。偶数ターンは回復も行う", 14, Muted);

            Panel("PlayerPanel", canvasRoot, 874, 292, 536, 113, new Color(.06f,.11f,.15f));
            player = Text("Player", canvasRoot, 896, 308, 480, 30, "", 20, Ink);
            Panel("PlayerBar", canvasRoot, 896, 348, 486, 10, new Color(.13f,.22f,.25f));
            playerFill = Panel("PlayerFill", canvasRoot, 896, 348, 486, 10, new Color(.28f,.73f,.59f));
            Text("PlayerHint", canvasRoot, 896, 370, 480, 24, "罠を線が通ると7ダメージ。防御で軽減できる", 14, Muted);

            Panel("RoutePanel", canvasRoot, 874, 421, 536, 113, new Color(.055f,.115f,.145f));
            Panel("RouteAccent", canvasRoot, 874, 421, 4, 113, Cyan);
            Text("RouteTitle", canvasRoot, 896, 435, 180, 28, "線の効果", 20, Cyan);
            route = Text("Route", canvasRoot, 896, 468, 480, 54, "", 16, Ink);

            Panel("TargetPanel", canvasRoot, 874, 550, 536, 108, new Color(.15f,.125f,.07f));
            Panel("TargetAccent", canvasRoot, 874, 550, 4, 108, Gold);
            target = Text("Target", canvasRoot, 896, 563, 490, 82, "", 17, Ink);

            hover = Text("Hover", canvasRoot, 890, 673, 500, 52, "", 16, Ink);
            history = Text("History", canvasRoot, 890, 731, 500, 60, "", 14, Muted);
            help = Text("Help", canvasRoot, 52, 789, 790, 24, "", 15, Gold);

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                darts[i] = Button("Dart" + i, 36 + i * 270, 818, 255, 65,
                    DartNames[i] + "\n" + DartEffects[i], () => SelectDart(index));
            }
            end = Button("EndTurn", 874, 805, 258, 48, "ターン終了", EndTurn);
            Button("Restart", 1148, 805, 258, 48, "再戦", Restart);
            result = Text("Result", canvasRoot, 886, 861, 510, 30, "", 18, Gold);
        }

        public void SelectDart(int index)
        {
            if (Aiming || Model.Phase != BattlePhase.Player || Model.Used[index]) return;
            selected = index;
            Refresh();
        }

        public void BeginAim()
        {
            if (Model.Phase != BattlePhase.Player || Model.Remaining == 0 || Model.Used[selected]) return;
            Aiming = true;
            started = Time.unscaledTime;
            UpdateAim();
        }

        public void ReleaseAim()
        {
            if (!Aiming) return;
            UpdateAim();
            Aiming = false;
            float angle = (float)aimRandom.NextDouble() * Mathf.PI * 2f;
            float distance = Mathf.Sqrt((float)aimRandom.NextDouble()) * Spread;
            LastHit = Aim + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            HasHit = true;
            Model.Throw(LastHit, selected);
            for (int i = 0; i < 3; i++) if (!Model.Used[i]) { selected = i; break; }
            Refresh();
        }

        public void EndTurn()
        {
            if (Aiming) return;
            Model.EndTurn();
            selected = 0;
            HasHit = false;
            Refresh();
        }

        void UpdateAim()
        {
            if (Mouse.current == null) { Aiming = false; return; }
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(board.rectTransform,
                Mouse.current.position.ReadValue(), null, out local);
            Aim = local / (board.rectTransform.rect.width * .5f);
            float elapsed = Time.unscaledTime - started;
            float wave = .5f + .5f * Mathf.Cos(elapsed * 4.6f);
            Spread = .055f + wave * .29f;
        }

        void Update()
        {
            if (Model == null) return;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectDart(0);
                if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectDart(1);
                if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectDart(2);
                if (Keyboard.current.spaceKey.wasPressedThisFrame) EndTurn();
                if (Keyboard.current.escapeKey.wasPressedThisFrame) { Aiming = false; overlay.SetVerticesDirty(); }
            }
            if (Aiming)
            {
                UpdateAim();
                overlay.SetVerticesDirty();
                help.text = Model.Points.Count == 0 ? "起点を狙っている。円が小さいときに離そう。" :
                    "白い予告線を見て、通したいマスを選ぼう。";
            }
            UpdateHover();
        }

        void UpdateHover()
        {
            if (Mouse.current == null) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(board.rectTransform,
                Mouse.current.position.ReadValue(), null, out local);
            Hit hit = BattleModel.Evaluate(local.x / 280f, local.y / 280f);
            if (hit.Miss)
            {
                hover.text = "盤面外：得点なし。線の点にもならない。";
                return;
            }
            int band = hit.Sector == 20 ? 0 : (int)hit.Band;
            int key = LinkBattleModel.CellKey(hit.Sector, hit.Sector == 20 ? BoardBand.InnerSingle : hit.Band);
            CardKind card = Model.Board[hit.Sector].Cards[band];
            string name = card == CardKind.Empty ? "空" : BattleModel.CardNames[(int)card] + LinkBattleModel.CardValues[(int)card];
            hover.text = hit.Score + "点　マス効果：" + name +
                (Model.DangerCells.Contains(key) ? "　罠あり" : "") +
                (Model.Points.Count > 0 ? "\nここへつなぐと線" + (Model.Segments.Count + 1) + "・効果x" + (Model.Segments.Count + 1) : "\n1投目は起点になる");
        }

        public void Refresh()
        {
            board.SetVerticesDirty();
            overlay.SetVerticesDirty();
            for (int sector = 0; sector < 20; sector++)
            {
                bool anyDanger = false;
                for (int band = 0; band < 4; band++)
                {
                    int key = LinkBattleModel.CellKey(sector, (BoardBand)band);
                    cells[sector,band].text = Symbols[(int)Model.Board[sector].Cards[band]];
                    if (Model.ActivatedThisTurn.Contains(key)) cells[sector,band].color = Gold;
                    else cells[sector,band].color = Color.white;
                    anyDanger |= Model.DangerCells.Contains(key);
                }
                danger[sector].text = anyDanger ? "!" : "";
            }
            bull.text = Symbols[(int)Model.Board[20].Cards[0]];
            turn.text = "ターン " + Model.Turn + "　／　残り" + Model.Remaining + "本";
            player.text = "自分　HP " + Model.Hp + " / " + Model.MaxHp + "　防御 " + Model.Shield + "　コイン " + Model.Coins;
            enemy.text = "体力 " + Model.EnemyHp + " / " + Model.EnemyMaxHp + "　毒 " + Model.Poison;
            playerFill.rectTransform.sizeDelta = new Vector2(486f * Model.Hp / Model.MaxHp, 10);
            enemyFill.rectTransform.sizeDelta = new Vector2(486f * Model.EnemyHp / Model.EnemyMaxHp, 10);

            if (Model.Segments.Count == 0)
                route.text = Model.Points.Count == 0 ? "まだ点がない。1投目で起点を置く。" : "起点を設置済み。次の投擲で線1（効果x1）。";
            else
            {
                LinkSegment first = Model.Segments[0];
                route.text = "線1：" + first.ActivatedCells + "マス発動 x1";
                if (Model.Segments.Count > 1)
                {
                    LinkSegment second = Model.Segments[1];
                    route.text += "\n線2：" + second.ActivatedCells + "マス発動 x2";
                }
                else route.text += "\n次の線は新しいマスを効果x2で発動";
            }

            string progress = Model.TargetAchieved ? "達成！" : Model.TurnScore > Model.TargetScore ? "超過" : "あと" + (Model.TargetScore - Model.TurnScore) + "点";
            target.text = "得点目標 " + Model.TargetScore + "点　合計" + Model.TurnScore + "点　" + progress +
                "\n報酬：" + Model.TargetRewardDescription + "\n線とは別に、3本の得点がちょうど一致で発動";
            history.text = string.Join("\n", Model.Log.GetRange(0, Math.Min(3, Model.Log.Count)));
            for (int i = 0; i < 3; i++)
            {
                darts[i].interactable = Model.Phase == BattlePhase.Player && !Model.Used[i];
                darts[i].GetComponent<UnityEngine.UI.Image>().color = selected == i ? new Color(.36f,.29f,.12f) : new Color(.15f,.20f,.27f);
            }
            end.interactable = Model.Phase == BattlePhase.Player;
            result.text = Model.Phase == BattlePhase.Victory ? "勝利！ 線の通し方が敵を倒した。" :
                Model.Phase == BattlePhase.Defeat ? "敗北… 「再戦」で線を引き直せる。" : "1／2／3：ダーツ選択　Space：ターン終了";
            help.text = Model.Phase != BattlePhase.Player ? "戦闘終了。再戦で最初から遊べます。" :
                Model.Remaining == 0 ? "3本投げ終えた。ターン終了で盤面効果を再抽選。" :
                Model.Points.Count == 0 ? "盤面を長押しし、円が小さいときに離して起点を置こう。" :
                "直前の点から次の点へ線がつながる。通したいマスを狙おう。";
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && Aiming) { Aiming = false; overlay.SetVerticesDirty(); }
        }

        void OnDisable() { Aiming = false; }
    }
}
