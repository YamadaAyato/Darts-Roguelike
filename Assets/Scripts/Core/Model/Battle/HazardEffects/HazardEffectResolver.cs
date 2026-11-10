namespace DartsRoguelike.Core.Model
{
    /// <summary>
    ///     危険マスに登録された効果を順に解決する
    /// </summary>
    public static class HazardEffectResolver
    {
        /// <summary>
        ///     プレイヤーが死亡するまで危険効果を適用し、実際に失った値を返す
        /// </summary>
        internal static (int HealthLost, int ShieldLost) Resolve(BoardHazard hazard, BattleCharacterModel player)
        {
            int healthLost = 0;
            int shieldLost = 0;
            foreach (IBoardHazardEffect effect in hazard.Effects)
            {
                if (!player.IsAlive)
                {
                    break;
                }

                switch (effect)
                {
                    case HazardDamageEffect damage:
                        (int health, int shield, _) = DamageResolver.Resolve(player, damage.Damage);
                        healthLost += health;
                        shieldLost += shield;
                        break;
                }
            }

            return (healthLost, shieldLost);
        }
    }
}
