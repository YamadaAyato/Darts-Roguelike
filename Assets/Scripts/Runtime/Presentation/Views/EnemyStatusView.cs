using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     敵1体のHPと攻撃予告を表示する
    /// </summary>
    public class EnemyStatusView : MonoBehaviour
    {
        [SerializeField, Tooltip("敵の表示名を表示するテキスト")]
        private UnityEngine.UI.Text _nameLabel;

        [SerializeField, Tooltip("HPと攻撃予告を表示するテキスト")]
        private UnityEngine.UI.Text _statusLabel;

        /// <summary>
        ///     敵の表示名を設定する
        /// </summary>
        public void SetDisplayName(string displayName)
        {
            _nameLabel.text = displayName;
        }

        /// <summary>
        ///     HPと次の攻撃予定を表示する
        /// </summary>
        /// <param name="nextAttackDamage"> 0の場合は攻撃予告を表示しない </param>
        public void Render(int currentHealth, int maxHealth, int nextAttackDamage)
        {
            string health = $"HP {currentHealth}/{maxHealth}";
            _statusLabel.text = nextAttackDamage > 0 ? $"{health}  |  ATK {nextAttackDamage}" : health;
        }
    }
}
