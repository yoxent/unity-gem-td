using System;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.Run
{
    public sealed class EnemySpawnerGate
    {
        readonly Action<EnemyDefinition> _spawn;
        readonly Func<int> _liveCount;

        public EnemySpawnerGate(Action<EnemyDefinition> spawn, Func<int> liveCount)
        {
            _spawn = spawn ?? throw new ArgumentNullException(nameof(spawn));
            _liveCount = liveCount ?? throw new ArgumentNullException(nameof(liveCount));
        }

        public void Spawn(EnemyDefinition enemy) => _spawn(enemy);

        public int LiveEnemyCount => _liveCount();
    }
}
