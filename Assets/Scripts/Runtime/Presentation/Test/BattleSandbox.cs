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

        [SerializeField, Min(1), Tooltip("プレイヤーターン開始時の投擲本数")]
        private int _throwsPerTurn = 3;

        [SerializeField, Tooltip("カード再抽選の乱数シード")]
        private int _cardDrawSeed = 1;

        [SerializeField, Tooltip("危険マスの配置抽選の乱数シード")]
        private int _hazardDrawSeed = 1;

        [SerializeField, Tooltip("開発シーンに出現する敵の編成")]
        private EnemySetup[] _enemyFormation = { new EnemySetup(8, 3), new EnemySetup(16, 4) };

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

        private void Awake()
        {
            if (_inputBinding == null || _enemyRosterView == null ||
                _enemyFormation == null || _enemyFormation.Length == 0 ||
                _victoryLabel == null || _turnLabel == null ||
                _boardCardListView == null || _cardPlacements == null)
            {
                Debug.LogError($"[{nameof(BattleSandbox)}] 投擲入力、戦闘の表示、敵の編成を設定してください", this);
                enabled = false;
                return;
            }

            BoardState board = CreateBoard();

            var enemies = new EnemyModel[_enemyFormation.Length];
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemySetup setup = _enemyFormation[i];
                enemies[i] = setup.CreateEnemy();
            }

            _state = new BattleState(board, enemies, _throwsPerTurn,
                new BattleCharacterModel(12, maxShield: 12), hazardSeed: _hazardDrawSeed);
            _enemyRosterView.SetCount(enemies.Length);
            _cardViews = _boardCardListView.CreateRows(_cardCells.Length);
            _victoryLabel.SetActive(false);
            RefreshEnemyStatus();
            RefreshTurnLabel();
            RefreshCardLabels();
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
            if (Keyboard.current?.enterKey.wasPressedThisFrame == true && _state.TryResolveEnemyTurn())
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
                _victoryLabel.SetActive(true);
            }
            else if (_state.IsEnemyTurn)
            {
                _inputBinding.enabled = false;
            }

            RefreshTurnLabel();
            RefreshCardLabels();
        }

        /// <summary>
        ///     プレイヤーHPと現在のターンを表示する
        /// </summary>
        private void RefreshTurnLabel()
        {
            _turnLabel.enabled = !_state.AreAllEnemiesDefeated;
            string health = $"HP {_state.Player.CurrentHealth}/{_state.Player.MaxHealth}  SH {_state.Player.CurrentShield}";
            if (!_state.Player.IsAlive)
            {
                _turnLabel.text = $"DEFEAT  |  {health}";
                return;
            }

            _turnLabel.text = _state.IsEnemyTurn
                ? $"{health}  |  ENEMY: ENTER"
                : $"{health}  |  DARTS {_state.RemainingThrows}";
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
        ///     凡例にカードの配置と使用済み状態を反映する
        /// </summary>
        private void RefreshCardLabels()
        {
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

            return string.Join(" + ", parts);
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
            _cardCells = new DartboardCellId[_cardPlacements.Length];
            // 末尾のnullを通常マスの再抽選候補にする
            var candidates = new BoardCard[_cardPlacements.Length + 1];
            var uniqueCells = new System.Collections.Generic.HashSet<DartboardCellId>();
            for (int i = 0; i < _cardPlacements.Length; i++)
            {
                CardSetup setup = _cardPlacements[i];
                DartboardCellId cell = setup.CreateCell();
                if (!uniqueCells.Add(cell))
                {
                    throw new System.InvalidOperationException($"[{nameof(BattleSandbox)}] カードの配置先が重複しています");
                }

                _cardCells[i] = cell;
                candidates[i] = setup.CreateCard();
            }

            var board = new BoardState(new BoardCardDeck(candidates, _cardDrawSeed));
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
