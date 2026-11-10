#nullable enable

using System;
using System.Collections.Generic;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     発動元に共通する危険マスの設置処理
    /// </summary>
    public static class HazardPlacementResolver
    {
        /// <summary>
        ///     現在の盤面から候補を選び、配置先を確定する
        /// </summary>
        /// <returns> 条件に合うマスがない場合はnull </returns>
        public static PlaceHazardEffect? Select(
            HazardPlacementRequest request, BoardState board, Random random)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (request.Mode == HazardPlacementMode.Fixed)
            {
                return new PlaceHazardEffect(request.FixedCell, request.Hazard);
            }

            var candidates = new List<DartboardCellId>();
            // 列挙順を固定し、同じシードで同じ設置先を再現できるようにする
            for (int number = 1; number <= 20; number++)
            {
                AddCandidate(new DartboardCellId(number, DartboardRing.InnerSingle), request.Mode, board, candidates);
                AddCandidate(new DartboardCellId(number, DartboardRing.Triple), request.Mode, board, candidates);
                AddCandidate(new DartboardCellId(number, DartboardRing.OuterSingle), request.Mode, board, candidates);
                AddCandidate(new DartboardCellId(number, DartboardRing.Double), request.Mode, board, candidates);
            }

            AddCandidate(new DartboardCellId(0, DartboardRing.OuterBull), request.Mode, board, candidates);
            AddCandidate(new DartboardCellId(0, DartboardRing.InnerBull), request.Mode, board, candidates);
            return candidates.Count == 0
                ? null
                : new PlaceHazardEffect(candidates[random.Next(candidates.Count)], request.Hazard);
        }

        /// <summary>
        ///     カード層を保持したまま危険効果を配置する
        /// </summary>
        public static void Resolve(PlaceHazardEffect placement, BoardState board)
        {
            if (placement == null)
            {
                throw new ArgumentNullException(nameof(placement));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            board.SetHazard(placement.Cell, placement.Hazard);
        }

        /// <summary>
        ///     配置条件を満たすマスを抽選候補へ追加する
        /// </summary>
        private static void AddCandidate(
            DartboardCellId cell, HazardPlacementMode mode, BoardState board, List<DartboardCellId> candidates)
        {
            if (mode == HazardPlacementMode.RandomDoubleOrTriple &&
                cell.Ring != DartboardRing.Double && cell.Ring != DartboardRing.Triple)
            {
                return;
            }

            if (mode == HazardPlacementMode.RandomCard && board.GetCard(cell) == null)
            {
                return;
            }

            candidates.Add(cell);
        }
    }
}
