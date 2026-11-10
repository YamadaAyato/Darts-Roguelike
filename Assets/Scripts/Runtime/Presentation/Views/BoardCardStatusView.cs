using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     盤面の1マスに配置されたカードの状態を表示する
    /// </summary>
    public class BoardCardStatusView : MonoBehaviour
    {
        [SerializeField, Tooltip("マスとカードの状態を表示するテキスト")]
        private UnityEngine.UI.Text _statusLabel;

        /// <summary>
        ///     マスの表示名とカードの現在の状態を表示する
        /// </summary>
        /// <param name="effectLabel"> カードなしの場合は空文字列 </param>
        public void Render(string cellLabel, string effectLabel, bool isUsed, string hazardLabel = "")
        {
            string hazardSuffix = string.IsNullOrEmpty(hazardLabel) ? string.Empty : $"  !{hazardLabel}";
            if (isUsed)
            {
                _statusLabel.text = $"{cellLabel}   USED{hazardSuffix}";
                return;
            }

            _statusLabel.text = $"{cellLabel}   {(string.IsNullOrEmpty(effectLabel) ? "Empty" : effectLabel)}{hazardSuffix}";
        }
    }
}
