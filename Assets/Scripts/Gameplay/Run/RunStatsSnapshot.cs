using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Run
{
    public readonly struct RunStatsTowerEntry
    {
        public TowerDefinition Tower { get; }
        public float Damage { get; }
        public float DamagePercent { get; }
        public int Kills { get; }
        public float KillPercent { get; }
        public int Built { get; }
        public float BuiltPercent { get; }

        public RunStatsTowerEntry(
            TowerDefinition tower,
            float damage,
            float damagePercent,
            int kills,
            float killPercent,
            int built,
            float builtPercent)
        {
            Tower = tower;
            Damage = damage;
            DamagePercent = damagePercent;
            Kills = kills;
            KillPercent = killPercent;
            Built = built;
            BuiltPercent = builtPercent;
        }
    }

    public readonly struct RunStatsSnapshot
    {
        public int WaveReached { get; }
        public int SkillsCount { get; }
        public float TotalDamage { get; }
        public int TotalKills { get; }
        public int TotalBuilt { get; }
        public RunStatsTowerEntry[] TowersByType { get; }

        public RunStatsSnapshot(
            int waveReached,
            int skillsCount,
            float totalDamage,
            int totalKills,
            int totalBuilt,
            RunStatsTowerEntry[] towersByType)
        {
            WaveReached = waveReached;
            SkillsCount = skillsCount;
            TotalDamage = totalDamage;
            TotalKills = totalKills;
            TotalBuilt = totalBuilt;
            TowersByType = towersByType ?? System.Array.Empty<RunStatsTowerEntry>();
        }
    }
}
