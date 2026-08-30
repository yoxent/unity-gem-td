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
        TowerInstance _owner;
        Action<TowerDefinition, float> _recordDamage;
        readonly List<EnemyRuntime> _impactScratch = new List<EnemyRuntime>(8);

        public bool IsActive { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public Vector3 Origin => _plan.Origin;
        public Vector3 LandingPoint => _plan.LandingPoint;
        public EffectPayloadPlan Plan => _plan;
        public TowerInstance Owner => _owner;

        public void Init(
            in EffectPayloadPlan plan,
            float flightSeconds,
            StatusRuntime statuses,
            TowerDefinition sourceTower,
            Action<TowerDefinition, float> recordDamage,
            TowerInstance owner = null)
        {
            _plan = plan;
            _progress = 0f;
            _flightSeconds = flightSeconds > MinFlightSeconds ? flightSeconds : MinFlightSeconds;
            _delayRemaining = plan.DelaySeconds;
            _resolved = false;
            _statuses = statuses;
            _sourceTower = sourceTower;
            _recordDamage = recordDamage;
            _owner = owner;
            Position = plan.Origin;
            Direction = ResolveDirection(0f);
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

            if (_plan.TravelPattern == EffectPayloadTravelPattern.FallFromSky)
                Position = Vector3.Lerp(_plan.Origin, _plan.LandingPoint, _progress);
            else
                Position = FountainTrajectory.Evaluate(
                    _plan.Origin,
                    _plan.LandingPoint,
                    _plan.ArcHeight,
                    _progress);
            Direction = ResolveDirection(_progress);
            return true;
        }

        Vector3 ResolveDirection(float progress)
        {
            if (_plan.TravelPattern == EffectPayloadTravelPattern.FallFromSky)
            {
                var toLanding = _plan.LandingPoint - Position;
                return toLanding.sqrMagnitude > 1e-8f
                    ? toLanding.normalized
                    : Vector3.down;
            }

            if (_plan.TravelPattern != EffectPayloadTravelPattern.Fountain)
                return Vector3.down;

            var nextProgress = Mathf.Min(progress + 0.01f, 1f);
            var next = FountainTrajectory.Evaluate(
                _plan.Origin,
                _plan.LandingPoint,
                _plan.ArcHeight,
                nextProgress);
            var delta = next - Position;
            if (delta.sqrMagnitude <= 1e-8f && progress > 0f)
            {
                var previousProgress = Mathf.Max(progress - 0.01f, 0f);
                var previous = FountainTrajectory.Evaluate(
                    _plan.Origin,
                    _plan.LandingPoint,
                    _plan.ArcHeight,
                    previousProgress);
                delta = Position - previous;
            }

            return delta.sqrMagnitude > 1e-8f
                ? delta.normalized
                : Vector3.down;
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
                _statuses.ApplyDamage(enemy, damage, _plan.HitSpec);
            else
                enemy.ApplyDamage(damage, _plan.HitSpec, null);

            _recordDamage?.Invoke(_sourceTower, damage);
        }
    }
}
