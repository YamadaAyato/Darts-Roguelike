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
        ///     HP、基礎攻撃力、次の行動予告を表示する
        /// </summary>
        public void Render(int currentHealth, int maxHealth, int baseAttackDamage, string intent)
        {
            string health = $"HP {currentHealth}/{maxHealth}  BASE {baseAttackDamage}";
            _statusLabel.text = string.IsNullOrEmpty(intent) ? health : $"{health}\n{intent}";
        }
    }
}
