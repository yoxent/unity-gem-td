namespace GemTD.Gameplay.Enemies
{
    /// <summary>Fill and visibility math for world-space enemy HP / shield bars.</summary>
    public static class EnemyHealthBarMath
    {
        const float DamagedEpsilon = 0.0001f;

        public static bool ShouldShow(float hp, float maxHealth, float shieldHp, float shieldMax, bool isBoss)
        {
            if (isBoss)
                return true;
            if (hp + DamagedEpsilon < maxHealth)
                return true;
            if (shieldHp + DamagedEpsilon < shieldMax)
                return true;
            return false;
        }

        public static void ComputeFills(
            float hp,
            float maxHealth,
            float shieldHp,
            out float hpFill,
            out float shieldFill)
        {
            hpFill = 0f;
            shieldFill = 0f;
            if (maxHealth <= 0f)
                return;

            hpFill = Clamp01(hp / maxHealth);
            // Shield overlays the same HP bar, in HP units, so 20 shield on 20 HP covers the full bar.
            shieldFill = Clamp01(shieldHp / maxHealth);
        }

        static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }
    }
}
