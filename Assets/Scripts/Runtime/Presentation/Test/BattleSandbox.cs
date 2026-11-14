using System.Collections.Generic;
using DartsRoguelike.Core.Model;
using DartsRoguelike.Definitions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     開発用シーンで投擲を戦闘処理と敵HPの表示につなぐ
    /// </summary>
    public class BattleSandbox : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private DartAimInputBinding _inputBinding;
        [SerializeField] private EnemyRosterView _enemyRosterView;
        [SerializeField] private GameObject _victoryLabel;
        [SerializeField] private UnityEngine.UI.Text _turnLabel;
        [SerializeField] private BoardCardListView _boardCardListView;
        [SerializeField] private BoardHazardView _boardHazardView;
        [SerializeField] private GameObject _rewardPanel;
        [SerializeField] private UnityEngine.UI.Text _rewardLabel;
        [SerializeField] private GameObject _landingMarker;

        [SerializeField, Min(1), Tooltip("プレイヤーターン開始時の投擲本数")]
        private int _throwsPerTurn = 3;

        [SerializeField, Tooltip("カード再抽選の乱数シード")]
        private int _cardDrawSeed = 1;

        [SerializeField, Tooltip("危険マスの配置抽選の乱数シード")]
        private int _hazardDrawSeed = 1;

        [SerializeField, Tooltip("ステージ順に並べた敵の編成")]
        private StageSetup[] _stages =
        {
            new StageSetup(new[] { new EnemySetup(12, 2) }),
            new StageSetup(new[] { new EnemySetup(8, 3), new EnemySetup(16, 2) }),
            new StageSetup(new[] { new EnemySetup(36, 3) })
        };

        [SerializeField, Tooltip("初期カードの配置と効果")]
        private CardSetup[] _cardPlacements =
        {
            new CardSetup(20, DartboardRing.OuterSingle, 8, AttackTargetScope.SingleEnemy),
            new CardSetup(6, DartboardRing.OuterSingle, 4, AttackTargetScope.AllEnemies),
            new CardSetup(11, DartboardRing.OuterSingle, 4, AttackTargetScope.SingleEnemy, SandboxCardEffectKind.Shield)
        };

        private BattleState _state;
        private DartboardCellId[] _cardCells;
        private BoardCardStatusView[] _cardViews;
        private BoardHazardPresenter _hazardPresenter;
        private int _stageIndex;
        private bool _waitingToStart = true;
        private bool _awaitingReward;
        private BoardCard[] _rewardChoices;
        private readonly List<BoardCard> _earnedCards = new List<BoardCard>();

        private void Awake()
        {
            if (_inputBinding == null || _enemyRosterView == null ||
                _stages == null || _stages.Length == 0 || _rewardPanel == null || _rewardLabel == null ||
                _victoryLabel == null || _turnLabel == null ||
                _boardCardListView == null || _cardPlacements == null || _boardHazardView == null)
            {
                Debug.LogError($"[{nameof(BattleSandbox)}] 投擲入力、戦闘の表示、敵の編成を設定してください", this);
                enabled = false;
                return;
            }

            foreach (StageSetup stage in _stages)
            {
                if (stage.Enemies == null || stage.Enemies.Length == 0)
                {
                    Debug.LogError($"[{nameof(BattleSandbox)}] 各ステージに敵を1体以上設定してください", this);
                    enabled = false;
                    return;
                }
            }

            _inputBinding.enabled = false;
            _turnLabel.enabled = false;
            _victoryLabel.SetActive(false);
            _rewardLabel.text = "DARTS ROGUELIKE\n\nLeft click to aim, left click again to throw\n" +
                "Right click to cancel aim\nEnter after your throws to resolve the enemy turn\n" +
                "Press 1-3 to choose a card after a win\n\nPress Enter to start";
            _rewardPanel.SetActive(true);
        }

        private void OnEnable()
        {
            _inputBinding.OnThrowResolved += HandleThrowResolved;
        }

        private void OnDisable()
        {
            if (_inputBinding != null)
            {
                _inputBinding.OnThrowResolved -= HandleThrowResolved;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (_waitingToStart)
            {
                if (keyboard?.enterKey.wasPressedThisFrame == true)
                {
                    _waitingToStart = false;
                    StartBattle(new BattleCharacterModel(12, maxShield: 12));
                }

                return;
            }

            if (_awaitingReward)
            {
                if (keyboard != null)
                {
                    HandleRewardInput(keyboard);
                }

                return;
            }

            if (keyboard?.enterKey.wasPressedThisFrame == true && _state.TryResolveEnemyTurn())
            {
                _inputBinding.enabled = _state.Player.IsAlive;
                RefreshEnemyStatus();
                RefreshTurnLabel();
                RefreshCardLabels();
            }
        }

        private void HandleThrowResolved(DartThrowResult result)
        {
            if (!_state.TryConsumeThrow())
            {
                return;
            }

            ThrowResolution resolution = ThrowEffectResolver.Resolve(result, _state);
            if (resolution.DidAttack)
            {
                RefreshEnemyStatus();
            }

            if (!_state.Player.IsAlive)
            {
                _inputBinding.enabled = false;
                RefreshEnemyStatus();
            }
            else if (_state.AreAllEnemiesDefeated)
            {
                _inputBinding.enabled = false;
                ShowVictory();
            }
            else if (_state.IsEnemyTurn)
            {
                _inputBinding.enabled = false;
            }

            RefreshTurnLabel();
            RefreshCardLabels();
        }

        /// <summary>
        ///     報酬選択中に数字キーからカードを確定する
        /// </summary>
        private void HandleRewardInput(Keyboard keyboard)
        {
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            {
                ChooseReward(0);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            {
                ChooseReward(1);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            {
                ChooseReward(2);
            }
        }

        /// <summary>
        ///     次のステージがある場合は報酬を表示し、最後の勝利では終了表示を出す
        /// </summary>
        private void ShowVictory()
        {
            _awaitingReward = _stageIndex + 1 < _stages.Length;
            if (!_awaitingReward)
            {
                _victoryLabel.SetActive(true);
                return;
            }

            // 過去に同じ種類を選んでいても、獲得した別のカードとしてデッキで扱う
            _rewardChoices = new[]
            {
                new BoardCard(12),
                BoardCard.CreateShield(6),
                new BoardCard(new IBoardCardEffect[] { new HealCardEffect(6) })
            };
            _rewardLabel.text = $"STAGE {_stageIndex + 1} CLEAR\nPress 1-3 to choose a card\n\n" +
                $"1   {DescribeCard(_rewardChoices[0])}\n" +
                $"2   {DescribeCard(_rewardChoices[1])}\n" +
                $"3   {DescribeCard(_rewardChoices[2])}";
            _rewardPanel.SetActive(true);
        }

        /// <summary>
        ///     選択したカードを獲得済みカードに加え、現在のHPで次のステージへ進む
        /// </summary>
        private void ChooseReward(int choiceIndex)
        {
            if (!_awaitingReward)
            {
                return;
            }

            BattleCharacterModel previousPlayer = _state.Player;
            var player = new BattleCharacterModel(previousPlayer.MaxHealth, maxShield: previousPlayer.MaxShield);
            player.TakeDamage(previousPlayer.MaxHealth - previousPlayer.CurrentHealth);
            _earnedCards.Add(_rewardChoices[choiceIndex]);
            _stageIndex++;
            StartBattle(player);
        }

        /// <summary>
        ///     戦闘用の状態を作り、盤面と表示を新しい戦闘に切り替える
        /// </summary>
        private void StartBattle(BattleCharacterModel player)
        {
            BoardState board = CreateBoard();
            EnemySetup[] formation = _stages[_stageIndex].Enemies;
            var enemies = new EnemyModel[formation.Length];
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i] = formation[i].CreateEnemy();
            }

            _state = new BattleState(board, enemies, _throwsPerTurn, player, hazardSeed: _hazardDrawSeed);
            _hazardPresenter = new BoardHazardPresenter(_state, _boardHazardView);
            _enemyRosterView.SetCount(enemies.Length);

            if (_cardViews != null)
            {
                foreach (BoardCardStatusView view in _cardViews)
                {
                    view.gameObject.SetActive(false);
                    Destroy(view.gameObject);
                }
            }

            _cardViews = _boardCardListView.CreateRows(_cardCells.Length);
            _awaitingReward = false;
            _victoryLabel.SetActive(false);
            _rewardPanel.SetActive(false);

            _inputBinding.ClearFlightVisual();
            if (_landingMarker != null)
            {
                _landingMarker.SetActive(false);
            }

            _inputBinding.enabled = true;
            RefreshEnemyStatus();
            RefreshTurnLabel();
            RefreshCardLabels();
        }

        /// <summary>
        ///     プレイヤーHPと現在のターンを表示する
        /// </summary>
        private void RefreshTurnLabel()
        {
            _turnLabel.enabled = true;
            string health = $"HP {_state.Player.CurrentHealth}/{_state.Player.MaxHealth}  SH {_state.Player.CurrentShield}";
            string stage = $"STAGE {_stageIndex + 1}/{_stages.Length}";
            if (!_state.Player.IsAlive)
            {
                _turnLabel.text = $"DEFEAT  |  {health}";
                return;
            }

            _turnLabel.text = _state.IsEnemyTurn
                ? $"{stage}  |  {health}  |  ENEMY: ENTER"
                : _state.AreAllEnemiesDefeated
                    ? $"{stage} CLEAR  |  {health}"
                    : $"{stage}  |  {health}  |  DARTS {_state.RemainingThrows}";
        }

        /// <summary>
        ///     敵のHPと次の攻撃予告を表示する
        /// </summary>
        private void RefreshEnemyStatus()
        {
            for (int i = 0; i < _state.Enemies.Count; i++)
            {
                EnemyModel enemy = _state.Enemies[i];
                _enemyRosterView.Render(i, enemy.CurrentHealth, enemy.MaxHealth,
                    enemy.BaseAttackDamage, DescribeEnemyIntent(enemy));
            }
        }

        /// <summary>
        ///     危険マスの盤面表示と、カードの配置や使用済み状態を示す凡例を更新する
        /// </summary>
        private void RefreshCardLabels()
        {
            _hazardPresenter.Refresh();
            for (int i = 0; i < _cardCells.Length; i++)
            {
                RefreshCardLabel(_cardCells[i], _cardViews[i]);
            }
        }

        /// <summary>
        ///     マスの番号とカードの状態を表示用のデータとして渡す
        /// </summary>
        private void RefreshCardLabel(DartboardCellId cell, BoardCardStatusView view)
        {
            BoardCard card = _state.Board.GetCard(cell);
            view.Render(FormatCellLabel(cell), DescribeCard(card), _state.Board.IsCardUsed(cell),
                DescribeHazard(_state.Board.GetHazard(cell)));
        }

        /// <summary>
        ///     凡例に表示するカード効果の要約を作る
        /// </summary>
        private static string DescribeCard(BoardCard card)
        {
            if (card == null)
            {
                return string.Empty;
            }

            if (card.Effects.Count > 1)
            {
                return $"Combo x{card.Effects.Count}";
            }

            return card.Effects[0] switch
            {
                AttackCardEffect attack =>
                    $"{(attack.Scope == AttackTargetScope.AllEnemies ? "All" : "Single")} ATK {attack.BaseDamage}",
                ShieldCardEffect shield => $"Shield {shield.BaseShield}",
                HealCardEffect heal => $"Heal {heal.BaseHealing}",
                PlaceHazardEffect => "Place hazard",
                HazardPlacementRequest => "Place hazard",
                _ => "Effect"
            };
        }

        /// <summary>
        ///     確定した行動一覧から敵の予告表示を作る
        /// </summary>
        private string DescribeEnemyIntent(EnemyModel enemy)
        {
            if (!_state.Player.IsAlive || !enemy.IsAlive)
            {
                return string.Empty;
            }

            var parts = new System.Collections.Generic.List<string>();
            foreach (IEnemyAction action in enemy.PlannedActions)
            {
                switch (action)
                {
                    case EnemyAttackAction attack:
                        int damage = EnemyActionResolver.CalculateAttackDamage(enemy, attack);
                        if (damage > 0)
                        {
                            parts.Add(attack.DamageMultiplier == 1
                                ? $"ATK {damage}"
                                : $"ATK x{attack.DamageMultiplier:0.##} ({damage})");
                        }
                        break;
                    case PlaceHazardEffect placement:
                        parts.Add($"MINE {FormatCellLabel(placement.Cell)} ({DescribeHazard(placement.Hazard)})");
                        break;
                }
            }

            return parts.Count == 0 ? "WAIT" : string.Join(" + ", parts);
        }

        /// <summary>
        ///     危険効果のダメージまたは効果数を表示用に要約する
        /// </summary>
        private static string DescribeHazard(BoardHazard hazard)
        {
            if (hazard == null)
            {
                return string.Empty;
            }

            return hazard.Effects.Count == 1 && hazard.Effects[0] is HazardDamageEffect damage
                ? damage.Damage.ToString()
                : $"x{hazard.Effects.Count}";
        }

        /// <summary>
        ///     Inspectorの配置から盤面を作り、各カードと空マスを再抽選候補にする
        /// </summary>
        private BoardState CreateBoard()
        {
            int cardCount = _cardPlacements.Length + _earnedCards.Count;
            var cells = new List<DartboardCellId>(cardCount);
            var candidates = new List<BoardCard>(cardCount + 1);
            var uniqueCells = new HashSet<DartboardCellId>();
            foreach (CardSetup setup in _cardPlacements)
            {
                DartboardCellId cell = setup.CreateCell();
                if (!uniqueCells.Add(cell))
                {
                    throw new System.InvalidOperationException($"[{nameof(BattleSandbox)}] カードの配置先が重複しています");
                }

                cells.Add(cell);
                candidates.Add(setup.CreateCard());
            }

            // 前のステージまでに獲得したカードをすべて抽選候補に加える
            candidates.AddRange(_earnedCards);
            // 報酬をすぐ試せるよう、空いているアウターシングルに獲得順で置く
            for (int number = 1; number <= 20 && cells.Count < cardCount; number++)
            {
                var cell = new DartboardCellId(number, DartboardRing.OuterSingle);
                if (uniqueCells.Add(cell))
                {
                    cells.Add(cell);
                }
            }

            // nullは通常マスの再抽選候補を表す
            candidates.Add(null);
            _cardCells = cells.ToArray();
            var board = new BoardState(new BoardCardDeck(candidates, _cardDrawSeed + _stageIndex));
            for (int i = 0; i < _cardCells.Length; i++)
            {
                board.SetCard(_cardCells[i], candidates[i]);
            }

            return board;
        }

        /// <summary>
        ///     番号が同じでもリングの違いが分かるマス名を作る
        /// </summary>
        private static string FormatCellLabel(DartboardCellId cell)
        {
            return cell.Ring switch
            {
                DartboardRing.InnerBull => "BULL",
                DartboardRing.OuterBull => "OUT BULL",
                DartboardRing.InnerSingle => $"{cell.Number,2} IS",
                DartboardRing.Double => $"{cell.Number,2} D",
                DartboardRing.Triple => $"{cell.Number,2} T",
                _ => $"{cell.Number,2} OS"
            };
        }

        [System.Serializable]
        private struct CardSetup
        {
            public CardSetup(
                int number, DartboardRing ring, int amount, AttackTargetScope scope,
                SandboxCardEffectKind effect = SandboxCardEffectKind.Attack)
            {
                _number = number;
                _ring = ring;
                _amount = amount;
                _scope = scope;
                _effect = effect;
                _definition = null;
            }

            [SerializeField, Range(0, 20), Tooltip("マスの番号。ブルは0")]
            private int _number;
            [SerializeField, Tooltip("マスのリング")]
            private DartboardRing _ring;
            [UnityEngine.Serialization.FormerlySerializedAs("_attackDamage")]
            [SerializeField, Min(1), Tooltip("カードの基礎効果量")]
            private int _amount;
            [SerializeField, Tooltip("カードの効果")]
            private SandboxCardEffectKind _effect;
            [SerializeField, Tooltip("攻撃カードの対象範囲")]
            private AttackTargetScope _scope;
            [SerializeField, Tooltip("設定するとインラインの効果より優先するカードアセット")]
            private BoardCardDefinition _definition;

            /// <summary>
            ///     設定した配置先のマスを作る
            /// </summary>
            public DartboardCellId CreateCell() => new DartboardCellId(_number, _ring);

            /// <summary>
            ///     設定した効果のカードを作る
            /// </summary>
            public BoardCard CreateCard()
            {
                if (_definition != null)
                {
                    return _definition.CreateCard();
                }

                return _effect == SandboxCardEffectKind.Shield
                    ? BoardCard.CreateShield(_amount)
                    : new BoardCard(_amount, _scope);
            }
        }

        [System.Serializable]
        private struct StageSetup
        {
            public StageSetup(EnemySetup[] enemies)
            {
                _enemies = enemies;
            }

            /// <summary> ステージに出現する敵の編成 </summary>
            public EnemySetup[] Enemies => _enemies;

            [SerializeField, Tooltip("左から表示する順に並べた敵")]
            private EnemySetup[] _enemies;
        }

        [System.Serializable]
        private struct EnemySetup
        {
            public EnemySetup(int maxHealth, int attackDamage)
            {
                _maxHealth = maxHealth;
                _attackDamage = attackDamage;
                _definition = null;
            }

            /// <summary> 敵の最大HP </summary>
            public int MaxHealth => _maxHealth;

            /// <summary> 敵の通常攻撃力 </summary>
            public int AttackDamage => _attackDamage;

            [SerializeField, Min(1), Tooltip("敵の最大HP")]
            private int _maxHealth;

            [SerializeField, Min(0), Tooltip("敵の通常攻撃力")]
            private int _attackDamage;

            [SerializeField, Tooltip("設定するとインライン設定より優先する敵アセット")]
            private EnemyDefinition _definition;

            /// <summary>
            ///     敵アセット、または開発用インライン設定から敵を生成する
            /// </summary>
            public EnemyModel CreateEnemy()
            {
                return _definition != null
                    ? _definition.CreateEnemy()
                    : new EnemyModel(_maxHealth, attackDamage: _attackDamage);
            }
        }
    }
}
