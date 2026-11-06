using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     盤面の1マスに配置された攻撃カードの状態を表示する
    /// </summary>
    public class BoardCardStatusView : MonoBehaviour
    {
        [SerializeField, Tooltip("マスとカードの状態を表示するテキスト")]
        private UnityEngine.UI.Text _statusLabel;

        /// <summary>
        ///     マスの表示名とカードの現在の状態を表示する
        /// </summary>
        /// <param name="attackDamage"> カードなしの場合はnull </param>
        public void Render(string cellLabel, int? attackDamage, bool targetsAll, bool isUsed)
        {
            if (isUsed)
            {
                _statusLabel.text = $"{cellLabel}   USED";
                return;
            }

            if (attackDamage == null)
            {
                _statusLabel.text = $"{cellLabel}   Empty";
                return;
            }

            string scope = targetsAll ? "All" : "Single";
            _statusLabel.text = $"{cellLabel}   {scope} ATK {attackDamage}";
        }
    }
}
