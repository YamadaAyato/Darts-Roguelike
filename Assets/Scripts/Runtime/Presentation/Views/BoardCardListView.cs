using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     指定されたマスのカード表示を凡例に並べる
    /// </summary>
    public class BoardCardListView : MonoBehaviour
    {
        [SerializeField, Tooltip("カードマス1つ分の凡例テンプレート")]
        private BoardCardStatusView _template;

        /// <summary>
        ///     指定された数のカード表示を作成する
        /// </summary>
        public BoardCardStatusView[] CreateRows(int count)
        {
            _template.gameObject.SetActive(false);
            var rows = new BoardCardStatusView[count];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = Instantiate(_template, transform);
                rows[i].gameObject.SetActive(true);
            }

            return rows;
        }
    }
}
