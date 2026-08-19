using System.Collections.Generic;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Run
{
    public sealed class RunStatsTracker
    {
        readonly Dictionary<TowerDefinition, float> _damageByTower = new Dictionary<TowerDefinition, float>(4);
        readonly Dictionary<TowerDefinition, int> _killsByTower = new Dictionary<TowerDefinition, int>(4);
        readonly Dictionary<TowerDefinition, int> _builtByTower = new Dictionary<TowerDefinition, int>(4);
        readonly HashSet<GemId> _socketedGems = new HashSet<GemId>();
        readonly List<RunStatsTowerEntry> _snapshotScratch = new List<RunStatsTowerEntry>(4);

        int _towersBuilt;

        public int TowersBuilt => _towersBuilt;
        public int SkillsCount => _socketedGems.Count;

        public void Reset()
        {
            _damageByTower.Clear();
            _killsByTower.Clear();
            _builtByTower.Clear();
            _socketedGems.Clear();
            _towersBuilt = 0;
        }

        public void RecordTowerPlaced(TowerDefinition def)
        {
            if (def == null)
                return;

            _towersBuilt++;
            if (_builtByTower.TryGetValue(def, out var count))
                _builtByTower[def] = count + 1;
            else
                _builtByTower[def] = 1;
        }

        public void RecordGemSocketed(GemId id)
        {
            if (id == GemId.None)
                return;

            _socketedGems.Add(id);
        }

        public void RecordDamage(TowerDefinition sourceTower, float amount)
        {
            if (sourceTower == null || amount <= 0f)
                return;

            if (_damageByTower.TryGetValue(sourceTower, out var current))
                _damageByTower[sourceTower] = current + amount;
            else
                _damageByTower[sourceTower] = amount;
        }

        public void RecordKill(TowerDefinition sourceTower)
        {
            if (sourceTower == null)
                return;

            if (_killsByTower.TryGetValue(sourceTower, out var count))
                _killsByTower[sourceTower] = count + 1;
            else
                _killsByTower[sourceTower] = 1;
        }

        public RunStatsSnapshot Snapshot(int waveReached, TowerDefinition[] catalogOrder)
        {
            _snapshotScratch.Clear();

            var totalDamage = 0f;
            foreach (var pair in _damageByTower)
                totalDamage += pair.Value;

            var totalKills = 0;
            foreach (var pair in _killsByTower)
                totalKills += pair.Value;

            if (catalogOrder != null)
            {
                for (var i = 0; i < catalogOrder.Length; i++)
                {
                    var tower = catalogOrder[i];
                    if (tower == null)
                        continue;

                    var damage = GetDamage(tower);
                    var kills = GetKills(tower);
                    var built = GetBuilt(tower);
                    var damagePercent = totalDamage > 0f ? damage / totalDamage : 0f;
                    var killPercent = totalKills > 0 ? (float)kills / totalKills : 0f;
                    var builtPercent = _towersBuilt > 0 ? (float)built / _towersBuilt : 0f;

                    _snapshotScratch.Add(new RunStatsTowerEntry(
                        tower,
                        damage,
                        damagePercent,
                        kills,
                        killPercent,
                        built,
                        builtPercent));
                }
            }

            return new RunStatsSnapshot(
                waveReached,
                _socketedGems.Count,
                totalDamage,
                totalKills,
                _towersBuilt,
                _snapshotScratch.ToArray());
        }

        float GetDamage(TowerDefinition tower) =>
            _damageByTower.TryGetValue(tower, out var value) ? value : 0f;

        int GetKills(TowerDefinition tower) =>
            _killsByTower.TryGetValue(tower, out var value) ? value : 0;

        int GetBuilt(TowerDefinition tower) =>
            _builtByTower.TryGetValue(tower, out var value) ? value : 0;
    }
}
