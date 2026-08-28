using System;
using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// In-flight secondary effect (fountain bolt, future pulse). Plain C# runtime — no scene objects.
    /// </summary>
    public sealed class EffectPayloadRuntime
    {
        public const float MinFlightSeconds = 0.08f;

        EffectPayloadPlan _plan;
        float _progress;
        float _flightSeconds;
        float _delayRemaining;
        bool _resolved;
        StatusRuntime _statuses;
        TowerDefinition _sourceTower;
        Action<TowerDefinition, float> _recordDamage;
        readonly List<EnemyRuntime> _impactScratch = new List<EnemyRuntime>(8);

        public bool IsActive { get; private set; }
        public Vector3 Position { get; private set; }

        public void Init(
            in EffectPayloadPlan plan,
            float flightSeconds,
            StatusRuntime statuses,
            TowerDefinition sourceTower,
            Action<TowerDefinition, float> recordDamage)
        {
            _plan = plan;
            _progress = 0f;
            _flightSeconds = flightSeconds > MinFlightSeconds ? flightSeconds : MinFlightSeconds;
            _delayRemaining = plan.DelaySeconds;
            _resolved = false;
            _statuses = statuses;
            _sourceTower = sourceTower;
            _recordDamage = recordDamage;
            Position = plan.Origin;
            IsActive = true;
        }

        public void Deactivate() => IsActive = false;

        /// <summary>Returns false when this payload should be removed.</summary>
        public bool Tick(float dt, List<EnemyRuntime> livingCandidates)
        {
            if (!IsActive)
                return false;

            if (_delayRemaining > 0f)
            {
                _delayRemaining -= dt;
                return true;
            }

            if (_plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse)
            {
                ResolveImpact(livingCandidates);
                return false;
            }

            if (dt <= 0f)
                return true;

            _progress += dt / _flightSeconds;
            if (_progress >= 1f)
            {
                Position = _plan.LandingPoint;
                ResolveImpact(livingCandidates);
                return false;
            }

            Position = FountainTrajectory.Evaluate(
                _plan.Origin,
                _plan.LandingPoint,
                _plan.ArcHeight,
                _progress);
            return true;
        }

        void ResolveImpact(List<EnemyRuntime> livingCandidates)
        {
            if (_resolved)
                return;

            _resolved = true;
            IsActive = false;
            Position = _plan.LandingPoint;

            if (livingCandidates == null || _plan.AoeRadius <= 0f)
                return;

            _impactScratch.Clear();
            AreaEffectResolver.CollectCircle(
                _plan.LandingPoint,
                _plan.AoeRadius,
                livingCandidates,
                _impactScratch,
                _plan.HitPolicy);

            var damage = RoleStatValue.SampleHitDamage(_plan.DamageMin, _plan.DamageMax);
            for (var i = 0; i < _impactScratch.Count; i++)
                ApplyDamage(_impactScratch[i], damage);
        }

        void ApplyDamage(EnemyRuntime enemy, float damage)
        {
            if (enemy == null || !enemy.IsAlive)
                return;

            if (_sourceTower != null)
                enemy.LastDamageSource = _sourceTower;

            if (_statuses != null)
                _statuses.ApplyDamage(enemy, damage);
            else
                enemy.ApplyDamage(damage);

            _recordDamage?.Invoke(_sourceTower, damage);
        }
    }
}
