using System.Collections.Generic;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Domain combat tick: cooldown → gem pipeline → targeting mode → projectiles (LMP/Chain).
    /// </summary>
    public sealed class CombatDirector
    {
        readonly float _cellSize;
        readonly float _projectileSpeed;
        readonly TargetSelector _selector = new TargetSelector();
        readonly List<ProjectileRuntime> _projectiles = new List<ProjectileRuntime>(32);

        public IReadOnlyList<ProjectileRuntime> Projectiles => _projectiles;

        public CombatDirector(float cellSize = 1f, float projectileSpeed = 20f)
        {
            _cellSize = cellSize > 0f ? cellSize : 1f;
            _projectileSpeed = projectileSpeed > 0f ? projectileSpeed : 20f;
        }

        /// <summary>Despawn all in-flight bolts (wave end / leave combat). Views pool via sync.</summary>
        public void ClearProjectiles()
        {
            for (var i = 0; i < _projectiles.Count; i++)
                _projectiles[i].Deactivate();
            _projectiles.Clear();
        }

        public void Tick(
            float dt,
            List<TowerRuntime> towers,
            EnemyRegistry enemies,
            GemModifierPipeline pipeline)
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
                    BuildSocketModifiers(tower, modifiers);
                    var baseline = AttackSpec.FromBase(tower.Def.Damage, 1, tower.Def.SplashRadius);
                    var spec = pipeline.Apply(baseline, modifiers);
                    ListPool<IAttackModifier>.Release(modifiers);

                    var towerPos = CellToWorld(tower.Cell);
                    if (!_selector.TrySelect(tower.TargetingMode, towerPos, tower.Def.Range, living, out var primary))
                        continue;

                    var fireRate = spec.FireRateMultiplier > 0.01f ? spec.FireRateMultiplier : 0.01f;
                    tower.Cooldown = tower.Def.AttackInterval / fireRate;
                    var damage = spec.Damage * tower.OutgoingDamageMultiplier;
                    SpawnVolley(towerPos, primary, spec, tower.Def.Range, damage);
                }
            }

            ListPool<EnemyRuntime>.Release(living);
        }

        static void BuildSocketModifiers(TowerRuntime tower, List<IAttackModifier> into)
        {
            into.Clear();
            var sockets = tower.Sockets;
            if (sockets == null)
                return;

            for (var i = 0; i < sockets.Length; i++)
            {
                var gem = sockets[i];
                if (gem == null || gem.Id == GemId.None)
                    continue;

                var mod = GemModifierFactory.Create(gem.Id);
                if (mod != null)
                    into.Add(mod);
            }
        }

        void SpawnVolley(Vector3 origin, EnemyRuntime primary, AttackSpec spec, float chainRange, float damage)
        {
            var aim = primary.WorldPosition - origin;
            if (aim.sqrMagnitude < 1e-8f)
                aim = Vector3.forward;
            else
                aim.Normalize();

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
                    _projectileSpeed,
                    chainRange,
                    spec.AoeRadius);
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
