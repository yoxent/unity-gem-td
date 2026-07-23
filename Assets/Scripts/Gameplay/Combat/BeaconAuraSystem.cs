using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    public sealed class BeaconAuraSystem
    {
        public void Tick(List<TowerRuntime> towers, EnemyRegistry enemies, float cellSize)
        {
            var size = cellSize > 0f ? cellSize : 1f;
            var half = size * 0.5f;

            if (towers != null)
            {
                for (var i = 0; i < towers.Count; i++)
                {
                    var tower = towers[i];
                    if (tower != null)
                        tower.OutgoingDamageMultiplier = 1f;
                }
            }

            if (enemies != null)
            {
                for (var i = 0; i < enemies.Count; i++)
                {
                    var enemy = enemies.GetAt(i);
                    if (enemy != null && enemy.IsAlive)
                        enemy.MoveSpeedMultiplier = 1f;
                }
            }

            if (towers == null)
                return;

            for (var b = 0; b < towers.Count; b++)
            {
                var beacon = towers[b];
                if (beacon == null || beacon.Def == null || beacon.Def.Kind != TowerKind.Aura)
                    continue;

                var beaconPos = CellToWorld(beacon.Cell, half, size);
                var allyRadius = beacon.Def.AllyAuraRadius;
                var allyMult = beacon.Def.AllyDamageMultiplier;
                var allyRadiusSq = allyRadius * allyRadius;
                var enemyRadius = beacon.Def.EnemyAuraRadius;
                var enemySlow = beacon.Def.EnemySlowMultiplier;
                var enemyRadiusSq = enemyRadius * enemyRadius;

                for (var t = 0; t < towers.Count; t++)
                {
                    var tower = towers[t];
                    if (tower == null)
                        continue;

                    var towerPos = CellToWorld(tower.Cell, half, size);
                    var dx = towerPos.x - beaconPos.x;
                    var dz = towerPos.z - beaconPos.z;
                    if (dx * dx + dz * dz <= allyRadiusSq && allyMult > tower.OutgoingDamageMultiplier)
                        tower.OutgoingDamageMultiplier = allyMult;
                }

                if (enemies == null)
                    continue;

                for (var e = 0; e < enemies.Count; e++)
                {
                    var enemy = enemies.GetAt(e);
                    if (enemy == null || !enemy.IsAlive)
                        continue;

                    var pos = enemy.WorldPosition;
                    var dx = pos.x - beaconPos.x;
                    var dz = pos.z - beaconPos.z;
                    if (dx * dx + dz * dz <= enemyRadiusSq && enemySlow < enemy.MoveSpeedMultiplier)
                        enemy.MoveSpeedMultiplier = enemySlow;
                }
            }
        }

        static Vector3 CellToWorld(Vector2Int cell, float half, float cellSize)
        {
            return new Vector3(cell.x * cellSize + half, 0f, cell.y * cellSize + half);
        }
    }
}
