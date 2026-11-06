using DartsRoguelike.Core.Model;
using UnityEngine;

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

        private BattleState _state;

        private void Awake()
        {
            if (_inputBinding == null || _leftEnemyHealth == null || _rightEnemyHealth == null ||
                _victoryLabel == null)
            {
                Debug.LogError($"[{nameof(BattleSandbox)}] 投擲入力、敵HP表示、勝利表示を設定してください", this);
                enabled = false;
                return;
            }

            var board = new BoardState();
            board.SetCard(new DartboardCellId(20, DartboardRing.OuterSingle), new BoardCard(8));
            board.SetCard(new DartboardCellId(6, DartboardRing.OuterSingle),
                new BoardCard(4, BoardCard.TargetScope.AllEnemies));

            _state = new BattleState(board, new[] { new EnemyModel(8), new EnemyModel(16) });
            _victoryLabel.SetActive(false);
            RefreshEnemyHealth();
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

        private void HandleThrowResolved(DartThrowResult result)
        {
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
