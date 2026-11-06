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
        private static readonly DartboardCellId OUTER_20_CELL = new DartboardCellId(20, DartboardRing.OuterSingle);
        private static readonly DartboardCellId OUTER_6_CELL = new DartboardCellId(6, DartboardRing.OuterSingle);

        [Header("参照")]
        [SerializeField] private DartAimInputBinding _inputBinding;
        [SerializeField, Tooltip("敵の編成順に並べたステータス表示")]
        private EnemyStatusView[] _enemyViews;
        [SerializeField] private GameObject _victoryLabel;
        [SerializeField] private UnityEngine.UI.Text _turnLabel;
        [SerializeField] private UnityEngine.UI.Text _singleAttackLabel;
        [SerializeField] private UnityEngine.UI.Text _allAttackLabel;

        [SerializeField, Min(1), Tooltip("プレイヤーターン開始時の投擲本数")]
        private int _throwsPerTurn = 3;

        [SerializeField, Tooltip("カード再抽選の乱数シード")]
        private int _cardDrawSeed = 1;

        private BattleState _state;

        private void Awake()
        {
            var enemies = new[] { new EnemyModel(8, attackDamage: 3), new EnemyModel(16, attackDamage: 4) };
            if (_inputBinding == null || _enemyViews == null || _enemyViews.Length != enemies.Length ||
                System.Array.Exists(_enemyViews, view => view == null) ||
                _victoryLabel == null || _turnLabel == null ||
                _singleAttackLabel == null || _allAttackLabel == null)
            {
                Debug.LogError($"[{nameof(BattleSandbox)}] 投擲入力と戦闘の表示を設定してください", this);
                enabled = false;
                return;
            }

            var singleAttack = new BoardCard(8);
            var allAttack = new BoardCard(4, BoardCard.TargetScope.AllEnemies);
            var deck = new BoardCardDeck(new[] { singleAttack, allAttack, null }, _cardDrawSeed);
            var board = new BoardState(deck);
            board.SetCard(OUTER_20_CELL, singleAttack);
            board.SetCard(OUTER_6_CELL, allAttack);

            _state = new BattleState(board, enemies, _throwsPerTurn, new BattleCharacterModel(12));
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
            for (int i = 0; i < _enemyViews.Length; i++)
            {
                EnemyModel enemy = _state.Enemies[i];
                int nextAttackDamage = _state.Player.IsAlive ? enemy.NextAttackDamage : 0;
                _enemyViews[i].Render(enemy.CurrentHealth, enemy.MaxHealth, nextAttackDamage);
            }
        }

        /// <summary>
        ///     凡例にカードの配置と使用済み状態を反映する
        /// </summary>
        private void RefreshCardLabels()
        {
            _singleAttackLabel.text = FormatCardLabel(OUTER_20_CELL);
            _allAttackLabel.text = FormatCardLabel(OUTER_6_CELL);
        }

        /// <summary>
        ///     マスの番号と現在のカード効果を凡例に表示する
        /// </summary>
        private string FormatCardLabel(DartboardCellId cell)
        {
            string number = $"{cell.Number,2}";
            if (_state.Board.IsCardUsed(cell))
            {
                return $"{number}   USED";
            }

            BoardCard card = _state.Board.GetCard(cell);
            if (card == null)
            {
                return $"{number}   Empty";
            }

            string scope = card.Scope == BoardCard.TargetScope.AllEnemies ? "All" : "Single";
            return $"{number}   {scope} ATK {card.BaseDamage}";
        }
    }
}
