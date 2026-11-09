using DartsRoguelike.Core.Model;
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

        [SerializeField, Tooltip("開発シーンに出現する敵の編成")]
        private EnemySetup[] _enemyFormation = { new EnemySetup(8, 3), new EnemySetup(16, 4) };

        [SerializeField, Tooltip("初期カードの配置と効果")]
        private CardSetup[] _cardPlacements =
        {
            new CardSetup(20, DartboardRing.OuterSingle, 8, BoardCard.TargetScope.SingleEnemy),
            new CardSetup(6, DartboardRing.OuterSingle, 4, BoardCard.TargetScope.AllEnemies)
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
                enemies[i] = new EnemyModel(setup.MaxHealth, attackDamage: setup.AttackDamage);
            }

            _state = new BattleState(board, enemies, _throwsPerTurn, new BattleCharacterModel(12));
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

            if (_state.AreAllEnemiesDefeated)
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
            string health = $"HP {_state.Player.CurrentHealth}/{_state.Player.MaxHealth}";
            if (!_state.Player.IsAlive)
            {
                _turnLabel.text = $"DEFEAT  |  {health}";
                return;
            }

            _turnLabel.text = _state.IsEnemyTurn
                ? $"{health}  |  ENEMY: ENTER"
                : $"{health}  |  PLAYER: {_state.RemainingThrows} DARTS";
        }

        /// <summary>
        ///     敵のHPと次の攻撃予告を表示する
        /// </summary>
        private void RefreshEnemyStatus()
        {
            for (int i = 0; i < _state.Enemies.Count; i++)
            {
                EnemyModel enemy = _state.Enemies[i];
                int nextAttackDamage = _state.Player.IsAlive ? enemy.NextAttackDamage : 0;
                _enemyRosterView.Render(i, enemy.CurrentHealth, enemy.MaxHealth, nextAttackDamage);
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
            view.Render(
                FormatCellLabel(cell),
                card?.BaseDamage,
                card?.Scope == BoardCard.TargetScope.AllEnemies,
                _state.Board.IsCardUsed(cell));
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
            public CardSetup(int number, DartboardRing ring, int attackDamage, BoardCard.TargetScope scope)
            {
                _number = number;
                _ring = ring;
                _attackDamage = attackDamage;
                _scope = scope;
            }

            [SerializeField, Range(0, 20), Tooltip("マスの番号。ブルは0")]
            private int _number;
            [SerializeField, Tooltip("マスのリング")]
            private DartboardRing _ring;
            [SerializeField, Min(1), Tooltip("カードの基礎ダメージ")]
            private int _attackDamage;
            [SerializeField, Tooltip("攻撃の対象範囲")]
            private BoardCard.TargetScope _scope;

            /// <summary>
            ///     設定した配置先のマスを作る
            /// </summary>
            public DartboardCellId CreateCell() => new DartboardCellId(_number, _ring);

            /// <summary>
            ///     設定した攻撃カードを作る
            /// </summary>
            public BoardCard CreateCard() => new BoardCard(_attackDamage, _scope);
        }

        [System.Serializable]
        private struct EnemySetup
        {
            public EnemySetup(int maxHealth, int attackDamage)
            {
                _maxHealth = maxHealth;
                _attackDamage = attackDamage;
            }

            /// <summary> 敵の最大HP </summary>
            public int MaxHealth => _maxHealth;

            /// <summary> 敵の通常攻撃力 </summary>
            public int AttackDamage => _attackDamage;

            [SerializeField, Min(1), Tooltip("敵の最大HP")]
            private int _maxHealth;

            [SerializeField, Min(0), Tooltip("敵の通常攻撃力")]
            private int _attackDamage;
        }
    }
}
