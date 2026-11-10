#nullable enable

using System;
using System.Collections.Generic;

namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     着弾したマスの危険効果とカードを戦闘状態へ適用する
    /// </summary>
    public static class ThrowEffectResolver
    {
        /// <summary>
        ///     一投の危険効果とカードを順に解決し、戦闘状態の変化を返す
        /// </summary>
        /// <param name="preferredEnemyIndex"> 優先して狙う敵の位置。左端は0、指定なしはnull </param>
        public static ThrowResolution Resolve(
            DartThrowResult throwResult, BattleState state, int? preferredEnemyIndex = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (throwResult.HitResult.IsOutside || !state.Player.IsAlive || state.AreAllEnemiesDefeated)
            {
                return new ThrowResolution(throwResult);
            }

            int hazardHealthLost = 0;
            int hazardShieldLost = 0;
            BoardHazard? hazard = state.Board.GetHazard(throwResult.HitResult.Cell);
            if (hazard != null)
            {
                (hazardHealthLost, hazardShieldLost) = HazardEffectResolver.Resolve(hazard, state.Player);
                if (!state.Player.IsAlive)
                {
                    return new ThrowResolution(throwResult,
                        hazardHealthLost: hazardHealthLost, hazardShieldLost: hazardShieldLost);
                }
            }

            return ResolveBoardCard(throwResult, state, preferredEnemyIndex, hazardHealthLost, hazardShieldLost);
        }

        /// <summary>
        ///     盤面カードの段階で着弾マスの効果を解決する
        /// </summary>
        private static ThrowResolution ResolveBoardCard(
            DartThrowResult throwResult, BattleState state, int? preferredEnemyIndex,
            int hazardHealthLost, int hazardShieldLost)
        {
            BoardCard? card = state.Board.UseCard(throwResult.HitResult.Cell);
            if (card == null)
            {
                return new ThrowResolution(throwResult,
                    hazardHealthLost: hazardHealthLost, hazardShieldLost: hazardShieldLost);
            }

            DartboardRing ring = throwResult.HitResult.Ring;
            var hits = new List<EnemyHitResult>();
            int shieldGained = 0;
            int healthRecovered = 0;
            foreach (IBoardCardEffect effect in card.Effects)
            {
                switch (effect)
                {
                    case AttackCardEffect attack:
                        ResolveAttack(attack, ring, state, preferredEnemyIndex, hits);
                        break;
                    case ShieldCardEffect shield:
                        int amount = DartboardEffectMultiplier.Apply(shield.BaseShield, ring);
                        shieldGained += state.Player.GainShield(amount);
                        break;
                    case HealCardEffect heal:
                        int healing = DartboardEffectMultiplier.Apply(heal.BaseHealing, ring);
                        healthRecovered += state.Player.Heal(healing);
                        break;
                    case PlaceHazardEffect placement:
                        HazardPlacementResolver.Resolve(placement, state.Board);
                        break;
                    case HazardPlacementRequest request:
                        PlaceHazardEffect? selectedPlacement = state.SelectHazardPlacement(request);
                        if (selectedPlacement != null)
                        {
                            HazardPlacementResolver.Resolve(selectedPlacement, state.Board);
                        }

                        break;
                }
            }

            return new ThrowResolution(throwResult, hits.AsReadOnly(), shieldGained, healthRecovered,
                hazardHealthLost, hazardShieldLost);
        }

        /// <summary>
        ///     攻撃効果の対象を決め、結果を発動順に追加する
        /// </summary>
        private static void ResolveAttack(
            AttackCardEffect attack, DartboardRing ring, BattleState state,
            int? preferredEnemyIndex, List<EnemyHitResult> hits)
        {
            if (attack.Scope == AttackTargetScope.AllEnemies)
            {
                foreach (EnemyModel enemy in state.Enemies)
                {
                    if (enemy.IsAlive)
                    {
                        hits.Add(DamageResolver.Resolve(enemy, attack.BaseDamage, ring));
                    }
                }

                return;
            }

            EnemyModel? target = preferredEnemyIndex is int enemyIndex
                ? state.FindLivingEnemy(enemyIndex)
                : null;

            // 狙っている敵がいない場合は、左端の生存敵を狙う
            target ??= state.FindFirstLivingEnemy();
            if (target != null)
            {
                hits.Add(DamageResolver.Resolve(target, attack.BaseDamage, ring));
            }
        }
    }
}
