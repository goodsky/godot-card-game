using System;

public static class CombatHelper
{
    public static bool IsAttacking(CardInfo? attacker, CardInfo? defender)
    {
        return CardDamage(attacker, defender) > 0;
    }

    public static bool IsBlocked(CardInfo? attacker, CardInfo? defender)
    {
        if (attacker == null) throw new InvalidOperationException("Null attacker during IsBlocked check.");
        if (defender == null) return false;

        CardInfo attackerInfo = attacker.Value;
        CardInfo defenderInfo = defender.Value;
        if (attackerInfo.Abilities.Contains(CardAbilities.Agile) &&
                !defenderInfo.Abilities.Contains(CardAbilities.Agile) &&
                !defenderInfo.Abilities.Contains(CardAbilities.Guard))
        {
            return false; // agile attacker flies over defenders
        }

        return true;
    }

    public static int CardDamage(CardInfo? attacker, CardInfo? defender)
    {
        if (attacker == null) return 0;

        CardInfo attackerInfo = attacker.Value;
        if (attackerInfo.Abilities.Contains(CardAbilities.Poisoned) &&
            defender != null)
        {
            return defender.Value.Health; // poison removes all health
        }

        return attackerInfo.Attack;
    }
}