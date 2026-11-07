namespace GemTD.Gameplay.Enemies
{
    public enum EnemyRank
    {
        Normal = 0,
        Elite = 1,
        Commander = 2,
        Boss = 3
    }

    public static class EnemyRankRules
    {
        public static int MaxAffixes(EnemyRank rank)
        {
            switch (rank)
            {
                case EnemyRank.Elite: return 1;
                case EnemyRank.Commander: return 2;
                case EnemyRank.Boss: return 4;
                default: return 0;
            }
        }
    }
}
