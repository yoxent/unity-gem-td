using System.Collections.Generic;

namespace GemTD.Gameplay.Enemies
{
    public sealed class EnemyRegistry
    {
        readonly List<EnemyRuntime> _enemies = new List<EnemyRuntime>();

        public int Count => _enemies.Count;

        public void Register(EnemyRuntime enemy)
        {
            if (enemy == null)
                return;

            _enemies.Add(enemy);
        }

        public void Unregister(EnemyRuntime enemy)
        {
            if (enemy == null)
                return;

            _enemies.Remove(enemy);
        }

        public EnemyRuntime GetAt(int i) => _enemies[i];

        public void CopyAlive(List<EnemyRuntime> into)
        {
            into.Clear();
            for (var i = 0; i < _enemies.Count; i++)
            {
                var enemy = _enemies[i];
                if (enemy != null && enemy.IsAlive)
                    into.Add(enemy);
            }
        }
    }
}
