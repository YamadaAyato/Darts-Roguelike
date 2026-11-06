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
        [SerializeField] private UnityEngine.UI.Text _leftEnemyHealth;
        [SerializeField] private UnityEngine.UI.Text _rightEnemyHealth;
        [SerializeField] private GameObject _victoryLabel;
        [SerializeField] private UnityEngine.UI.Text _turnLabel;

        [SerializeField, Min(1), Tooltip("プレイヤーターン開始時の投擲本数")]
        private int _throwsPerTurn = 3;

        private BattleState _state;

        private void Awake()
        {
            if (_inputBinding == null || _leftEnemyHealth == null || _rightEnemyHealth == null ||
                _victoryLabel == null || _turnLabel == null)
            {
                Debug.LogError($"[{nameof(BattleSandbox)}] 投擲入力と戦闘の表示を設定してください", this);
                enabled = false;
                return;
            }

            var board = new BoardState();
            board.SetCard(new DartboardCellId(20, DartboardRing.OuterSingle), new BoardCard(8));
            board.SetCard(new DartboardCellId(6, DartboardRing.OuterSingle),
                new BoardCard(4, BoardCard.TargetScope.AllEnemies));

            var enemies = new[] { new EnemyModel(8, attackDamage: 3), new EnemyModel(16, attackDamage: 4) };
            _state = new BattleState(board, enemies, _throwsPerTurn, new BattleCharacterModel(12));
            _victoryLabel.SetActive(false);
            RefreshEnemyHealth();
            RefreshTurnLabel();
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
                RefreshTurnLabel();
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
                RefreshEnemyHealth();
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
        ///     敵の現在HPと最大HPを表示する
        /// </summary>
        private void RefreshEnemyHealth()
        {
            EnemyModel leftEnemy = _state.Enemies[0];
            EnemyModel rightEnemy = _state.Enemies[1];
            _leftEnemyHealth.text = $"HP  {leftEnemy.CurrentHealth} / {leftEnemy.MaxHealth}";
            _rightEnemyHealth.text = $"HP  {rightEnemy.CurrentHealth} / {rightEnemy.MaxHealth}";
        }
    }
}
