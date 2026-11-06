using System.Collections.Generic;
using UnityEngine;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     敵の編成順にステータス表示を並べる
    /// </summary>
    public class EnemyRosterView : MonoBehaviour
    {
        [SerializeField, Tooltip("敵1体分の表示テンプレート")]
        private EnemyStatusView _template;

        private readonly List<EnemyStatusView> _views = new List<EnemyStatusView>();

        /// <summary>
        ///     編成数に合わせて表示を追加し、余った表示を隠す
        /// </summary>
        public void SetCount(int count)
        {
            _template.gameObject.SetActive(false);
            for (int i = _views.Count; i < count; i++)
            {
                EnemyStatusView view = Instantiate(_template, transform);
                view.SetDisplayName($"ENEMY {i + 1}");
                _views.Add(view);
            }

            for (int i = 0; i < _views.Count; i++)
            {
                _views[i].gameObject.SetActive(i < count);
            }
        }

        /// <summary>
        ///     指定位置にいる敵のステータスを更新する
        /// </summary>
        public void Render(int enemyIndex, int currentHealth, int maxHealth, int nextAttackDamage)
        {
            _views[enemyIndex].Render(currentHealth, maxHealth, nextAttackDamage);
        }
    }
}
