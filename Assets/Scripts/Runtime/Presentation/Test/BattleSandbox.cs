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
        [SerializeField] private EnemyRosterView _enemyRosterView;
        [SerializeField] private GameObject _victoryLabel;
        [SerializeField] private UnityEngine.UI.Text _turnLabel;
        [SerializeField] private BoardCardStatusView _outer20View;
        [SerializeField] private BoardCardStatusView _outer6View;

        [SerializeField, Min(1), Tooltip("プレイヤーターン開始時の投擲本数")]
        private int _throwsPerTurn = 3;

        [SerializeField, Tooltip("カード再抽選の乱数シード")]
        private int _cardDrawSeed = 1;

        [SerializeField, Tooltip("開発シーンに出現する敵の編成")]
        private EnemySetup[] _enemyFormation = { new EnemySetup(8, 3), new EnemySetup(16, 4) };

        private BattleState _state;

        private void Awake()
        {
            if (_inputBinding == null || _enemyRosterView == null ||
                _enemyFormation == null || _enemyFormation.Length == 0 ||
                _victoryLabel == null || _turnLabel == null ||
                _outer20View == null || _outer6View == null)
            {
                Debug.LogError($"[{nameof(BattleSandbox)}] 投擲入力、戦闘の表示、敵の編成を設定してください", this);
                enabled = false;
                return;
            }

            var singleAttack = new BoardCard(8);
            var allAttack = new BoardCard(4, BoardCard.TargetScope.AllEnemies);
            var deck = new BoardCardDeck(new[] { singleAttack, allAttack, null }, _cardDrawSeed);
            var board = new BoardState(deck);
            board.SetCard(OUTER_20_CELL, singleAttack);
            board.SetCard(OUTER_6_CELL, allAttack);

            var enemies = new EnemyModel[_enemyFormation.Length];
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemySetup setup = _enemyFormation[i];
                enemies[i] = new EnemyModel(setup.MaxHealth, attackDamage: setup.AttackDamage);
            }

            _state = new BattleState(board, enemies, _throwsPerTurn, new BattleCharacterModel(12));
            _enemyRosterView.SetCount(enemies.Length);
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
            RefreshCardLabel(OUTER_20_CELL, _outer20View);
            RefreshCardLabel(OUTER_6_CELL, _outer6View);
        }

        /// <summary>
        ///     マスの番号とカードの状態を表示用のデータとして渡す
        /// </summary>
        private void RefreshCardLabel(DartboardCellId cell, BoardCardStatusView view)
        {
            BoardCard card = _state.Board.GetCard(cell);
            view.Render(
                $"{cell.Number,2}",
                card?.BaseDamage,
                card?.Scope == BoardCard.TargetScope.AllEnemies,
                _state.Board.IsCardUsed(cell));
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
