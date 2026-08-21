using System;
using System.Collections.Generic;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Domain combat tick: cooldown → gem pipeline → targeting mode → projectiles (LMP/Chain/Hydra).
    /// </summary>
    public sealed class CombatDirector
    {
        readonly float _cellSize;
        readonly float _projectileSpeed;
        readonly TargetSelector _selector = new TargetSelector();
        readonly List<ProjectileRuntime> _projectiles = new List<ProjectileRuntime>(32);
        readonly List<ProjectileRuntime> _spawnBuffer = new List<ProjectileRuntime>(16);
        readonly Action<TowerDefinition, float> _recordDamage;

        public IReadOnlyList<ProjectileRuntime> Projectiles => _projectiles;

        public CombatDirector(float cellSize = 1f, float projectileSpeed = 20f, Action<TowerDefinition, float> recordDamage = null)
        {
            _cellSize = cellSize > 0f ? cellSize : 1f;
            _projectileSpeed = projectileSpeed > 0f ? projectileSpeed : 20f;
            _recordDamage = recordDamage;
        }

        /// <summary>Despawn all in-flight bolts (wave end / leave combat). Views pool via sync.</summary>
        public void ClearProjectiles()
        {
            for (var i = 0; i < _projectiles.Count; i++)
                _projectiles[i].Deactivate();
            _projectiles.Clear();
            _spawnBuffer.Clear();
        }

        public void Tick(
            float dt,
            List<TowerRuntime> towers,
            EnemyRegistry enemies,
            GemModifierPipeline pipeline,
            StatusRuntime statuses = null)
        {
            if (enemies == null || pipeline == null)
                return;

            var living = ListPool<EnemyRuntime>.Get();
            enemies.CopyAlive(living);

            for (var i = _projectiles.Count - 1; i >= 0; i--)
            {
                if (!_projectiles[i].Tick(dt, living))
                    _projectiles.RemoveAt(i);
            }

            MergeSpawnBuffer();

            // Refresh after projectile kills so tower targeting sees current alive set.
            enemies.CopyAlive(living);

            if (towers != null)
            {
                for (var t = 0; t < towers.Count; t++)
                {
                    var tower = towers[t];
                    if (tower == null || tower.Def == null)
                        continue;

                    if (tower.Def.Kind == TowerKind.Aura)
                        continue;

                    tower.Cooldown -= dt;
                    if (tower.Cooldown > 0f)
                        continue;

                    var modifiers = ListPool<IAttackModifier>.Get();
                    var spec = pipeline.Resolve(tower, modifiers);
                    ListPool<IAttackModifier>.Release(modifiers);

                    var towerPos = CellToWorld(tower.Cell);
                    var rangeMul = spec.RangeMultiplier > 0.01f ? spec.RangeMultiplier : 1f;
                    var range = tower.Def.Range * rangeMul;
                    if (!_selector.TrySelect(tower.Targeting, towerPos, range, living, out var primary))
                        continue;

                    var fireRate = spec.FireRateMultiplier > 0.01f ? spec.FireRateMultiplier : 0.01f;
                    tower.Cooldown = tower.Def.AttackInterval / fireRate;
                    var damage = spec.Damage * tower.OutgoingDamageMultiplier;
                    var speedMul = spec.ProjectileSpeedMultiplier > 0.01f ? spec.ProjectileSpeedMultiplier : 1f;
                    var speed = _projectileSpeed * speedMul;
                    var volleys = spec.EchoVolleyCount >= 2 ? spec.EchoVolleyCount : 1;
                    var echoFactor = volleys > 1 ? spec.EchoDamageFactor : 1f;
                    if (echoFactor <= 0f)
                        echoFactor = 1f;
                    var volleyDamage = damage * echoFactor;
                    var hydra = EvolutionEvaluator.IsHydraBallista(tower);
                    for (var v = 0; v < volleys; v++)
                    {
                        if (hydra)
                        {
                            var laterals = EvolutionEvaluator.HydraHeadLateralOffsets;
                            var yaws = EvolutionEvaluator.HydraHeadYawOffsets;
                            for (var h = 0; h < laterals.Length; h++)
                                SpawnVolley(towerPos, primary, spec, ProjectileRuntime.DefaultChainRange, volleyDamage, speed, statuses, tower.Def, yaws[h], laterals[h]);
                        }
                        else
                        {
                            SpawnVolley(towerPos, primary, spec, ProjectileRuntime.DefaultChainRange, volleyDamage, speed, statuses, tower.Def, 0f, 0f);
                        }
                    }
                }
            }

            ListPool<EnemyRuntime>.Release(living);
        }

        void MergeSpawnBuffer()
        {
            for (var i = 0; i < _spawnBuffer.Count; i++)
                _projectiles.Add(_spawnBuffer[i]);
            _spawnBuffer.Clear();
        }

        void SpawnVolley(
            Vector3 origin,
            EnemyRuntime primary,
            AttackSpec spec,
            float chainRange,
            float damage,
            float speed,
            StatusRuntime statuses,
            TowerDefinition sourceTower,
            float headYawDegrees,
            float headLateral)
        {
            var aim = primary.WorldPosition - origin;
            if (aim.sqrMagnitude < 1e-8f)
                aim = Vector3.forward;
            else
                aim.Normalize();

            if (Mathf.Abs(headLateral) > 1e-4f)
            {
                var headSide = Quaternion.Euler(0f, 90f, 0f) * aim;
                origin += headSide * headLateral;
                aim = primary.WorldPosition - origin;
                if (aim.sqrMagnitude > 1e-8f)
                    aim.Normalize();
            }

            if (Mathf.Abs(headYawDegrees) > 1e-4f)
                aim = Quaternion.Euler(0f, headYawDegrees, 0f) * aim;

            var pierceRemaining = spec.Pierce ? ProjectileRuntime.DefaultPierceRemaining : 0;
            var forkRemaining = spec.ForkCount;
            var count = spec.ProjectileCount > 0 ? spec.ProjectileCount : 1;

            for (var i = 0; i < count; i++)
            {
                var yaw = 0f;
                if (count > 1 && spec.SpreadDegrees > 0f)
                {
                    var t = i / (float)(count - 1);
                    yaw = Mathf.Lerp(-spec.SpreadDegrees * 0.5f, spec.SpreadDegrees * 0.5f, t);
                }

                var dir = Quaternion.Euler(0f, yaw, 0f) * aim;

                var projectile = new ProjectileRuntime();
                projectile.Init(
                    origin,
                    dir,
                    primary,
                    damage,
                    spec.ChainCount,
                    speed,
                    chainRange,
                    spec.AoeRadius,
                    pierceRemaining,
                    forkRemaining,
                    spec.Ignite,
                    spec.Chill,
                    spec.Shock,
                    spec.Proliferate,
                    statuses,
                    _spawnBuffer,
                    softSeek: false,
                    seekOffset: default,
                    sourceTower,
                    _recordDamage);
                _projectiles.Add(projectile);
            }
        }

        Vector3 CellToWorld(Vector2Int cell)
        {
            var half = _cellSize * 0.5f;
            return new Vector3(cell.x * _cellSize + half, 0f, cell.y * _cellSize + half);
        }
    }
}
