using System;
using System.Collections.Generic;
using DartsRoguelike.Core.Model;

namespace DartsRoguelike.Presentation
{
    /// <summary>
    ///     戦闘中の危険効果と生存敵の設置予告を表示情報へまとめる
    /// </summary>
    public class BoardHazardPresenter
    {
        public BoardHazardPresenter(BattleState state, IBoardHazardView view)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        private readonly BattleState _state;
        private readonly IBoardHazardView _view;

        /// <summary>
        ///     現在の盤面と敵の予告を表示へ反映する
        /// </summary>
        public void Refresh()
        {
            // 設置済みと予告のマスを一つにまとめ、複数の敵が同じマスを選んでも表示を重複させない
            var cells = new HashSet<DartboardCellId>(_state.Board.HazardCells);
            var previews = new HashSet<DartboardCellId>();

            // 設置済みの危険マスは残し、実行可能な敵の予告だけを集める
            if (_state.Player.IsAlive)
            {
                foreach (EnemyModel enemy in _state.Enemies)
                {
                    if (!enemy.IsAlive)
                    {
                        continue;
                    }

                    foreach (IEnemyAction action in enemy.PlannedActions)
                    {
                        if (action is PlaceHazardEffect placement)
                        {
                            previews.Add(placement.Cell);
                            cells.Add(placement.Cell);
                        }
                    }
                }
            }

            // 同じマスに設置済みと予告が重なる場合も、Viewには両方の状態を渡す
            var displays = new List<BoardHazardCellDisplay>(cells.Count);
            foreach (DartboardCellId cell in cells)
            {
                displays.Add(new BoardHazardCellDisplay(
                    cell, _state.Board.GetHazard(cell) != null, previews.Contains(cell)));
            }

            _view.Render(displays);
        }
    }
}
