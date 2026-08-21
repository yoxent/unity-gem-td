namespace GemTD.Gameplay.Combat
{
    public static class TargetingKeyLabels
    {
        public static string For(TargetingKey key)
        {
            switch (key)
            {
                case TargetingKey.LeastHpPct: return "Least HP%";
                case TargetingKey.MostHpPct: return "Most HP%";
                case TargetingKey.MostArmor: return "Most Armor";
                case TargetingKey.MostShield: return "Most Shield";
                case TargetingKey.Fastest: return "Fastest";
                case TargetingKey.Slowest: return "Slowest";
                case TargetingKey.Last: return "Last";
                default: return "First";
            }
        }
    }
}
