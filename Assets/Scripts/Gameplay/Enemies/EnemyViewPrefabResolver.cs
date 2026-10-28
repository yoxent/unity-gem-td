namespace GemTD.Gameplay.Enemies
{
    public static class EnemyViewPrefabResolver
    {
        public static EnemyView Resolve(EnemyDefinition def, EnemyView fallback)
        {
            if (def != null && def.ViewPrefab != null)
                return def.ViewPrefab;
            return fallback;
        }
    }
}
