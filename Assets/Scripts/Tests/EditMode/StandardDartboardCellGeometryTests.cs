using System;
using DartsRoguelike.Core.Model;
using NUnit.Framework;
using UnityEngine;

namespace DartsRoguelike.Tests.EditMode
{
    /// <summary>
    ///     危険表示の領域と実際の着弾判定の一致を検証する
    /// </summary>
    public class StandardDartboardCellGeometryTests
    {
        /// <summary>
        ///     全82マスの内外周の中点が元のマスに命中する
        /// </summary>
        [TestCase(DartboardRing.InnerSingle)]
        [TestCase(DartboardRing.Triple)]
        [TestCase(DartboardRing.OuterSingle)]
        [TestCase(DartboardRing.Double)]
        [TestCase(DartboardRing.InnerBull)]
        [TestCase(DartboardRing.OuterBull)]
        public void CreateStrip_InteriorPoints_MatchHitCalculation(DartboardRing ring)
        {
            bool isBull = ring is DartboardRing.InnerBull or DartboardRing.OuterBull;
            int count = isBull ? 1 : 20;
            for (int number = 1; number <= count; number++)
            {
                var cell = new DartboardCellId(isBull ? 0 : number, ring);
                Vector2[] strip = StandardDartboardCellGeometry.CreateStrip(cell, 12);
                Assert.That(strip.Length, Is.EqualTo(26));
                for (int i = 1; i < 12; i++)
                {
                    Vector2 middle = (strip[i * 2] + strip[i * 2 + 1]) * 0.5f;
                    Assert.That(StandardDartboardHitCalculator.Calculate(middle).Cell, Is.EqualTo(cell),
                        $"{number} {ring} の表示領域が着弾判定と一致しません");
                }
            }
        }

        /// <summary>
        ///     盤外や分割数不足では無効なメッシュを作らない
        /// </summary>
        [Test]
        public void CreateStrip_InvalidInput_Throws()
        {
            Assert.Throws<ArgumentException>(() => StandardDartboardCellGeometry.CreateStrip(
                new DartboardCellId(0, DartboardRing.OutSide), 12));
            Assert.Throws<ArgumentOutOfRangeException>(() => StandardDartboardCellGeometry.CreateStrip(
                new DartboardCellId(20, DartboardRing.Double), 1));
        }
    }
}
