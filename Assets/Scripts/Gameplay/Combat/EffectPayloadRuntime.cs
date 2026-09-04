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
        public const float StationaryPulseVisualSeconds = 1f;
        public const float FallLandVisualSeconds = 1f;
        public const float FallEnemyHitVisualSeconds = 0.5f;

        EffectPayloadPlan _plan;
        float _progress;
        float _flightSeconds;
        float _delayRemaining;
        float _visualRemaining;
        bool _resolved;
        StatusRuntime _statuses;
        TowerDefinition _sourceTower;
        TowerInstance _owner;
        Action<TowerDefinition, float> _recordDamage;
        System.Random _critRng;
        readonly List<EnemyRuntime> _impactScratch = new List<EnemyRuntime>(8);

        public bool IsActive { get; private set; }
        public bool ShowsPulseVisual =>
            IsActive
            && _plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse
            && _resolved;
        public bool ShowsSlamVisual =>
            ShowsPulseVisual && _plan.Visual == EffectPayloadVisual.Slam;
        public bool ShowsAftershockVisual =>
            ShowsPulseVisual && _plan.Visual == EffectPayloadVisual.Aftershock;
        public bool ShowsFallVisual =>
            IsActive
            && _plan.TravelPattern == EffectPayloadTravelPattern.FallFromSky
            && _delayRemaining <= 0f
            && _progress > 0f;
        public bool IsAwaitingImpact =>
            IsActive
            && _plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse
            && !_resolved;
        public bool HasResolvedImpact => _resolved;
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
            _visualRemaining = 0f;
            _resolved = false;
            _statuses = statuses;
            _sourceTower = sourceTower;
            _recordDamage = recordDamage;
            _owner = owner;
            Position = plan.Origin;
            Direction = ResolveDirection(0f);
            IsActive = true;
            _critRng = null;
            if (_plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse
                && _delayRemaining <= 0f
                && _plan.DamageMin <= 0f
                && _plan.DamageMax <= 0f)
            {
                _resolved = true;
                _visualRemaining = StationaryPulseVisualSeconds;
                Position = _plan.LandingPoint;
            }
        }

        public void SetCritRng(System.Random rng)
        {
            _critRng = rng;
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
                if (!_resolved)
                    ResolveImpact(livingCandidates);
                _visualRemaining -= dt;
                if (_visualRemaining <= 0f)
                {
                    IsActive = false;
                    return false;
                }

                return true;
            }

            if (_plan.TravelPattern == EffectPayloadTravelPattern.FallFromSky && _resolved)
            {
                _visualRemaining -= dt;
                if (_visualRemaining <= 0f)
                {
                    IsActive = false;
                    return false;
                }

                return true;
            }

            if (dt <= 0f)
                return true;

            _progress += dt / _flightSeconds;
            if (_progress >= 1f)
            {
                Position = _plan.LandingPoint;
                ResolveImpact(livingCandidates);
                if (_plan.TravelPattern == EffectPayloadTravelPattern.FallFromSky)
                    return true;

                IsActive = false;
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
            Position = _plan.LandingPoint;
            if (_plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse)
                _visualRemaining = StationaryPulseVisualSeconds;

            var hitEnemy = false;
            if (livingCandidates != null && _plan.AoeRadius > 0f)
            {
                _impactScratch.Clear();
                AreaEffectResolver.CollectCircle(
                    _plan.LandingPoint,
                    _plan.AoeRadius,
                    livingCandidates,
                    _impactScratch,
                    _plan.HitPolicy);

                var damage = RoleStatValue.SampleHitDamage(_plan.DamageMin, _plan.DamageMax);
                for (var i = 0; i < _impactScratch.Count; i++)
                {
                    var enemy = _impactScratch[i];
                    if (enemy == null || !enemy.IsAlive)
                        continue;

                    hitEnemy = true;
                    if (damage > 0f)
                        ApplyDamage(enemy, damage);
                }
            }

            if (_plan.TravelPattern == EffectPayloadTravelPattern.FallFromSky)
            {
                _visualRemaining = hitEnemy
                    ? FallEnemyHitVisualSeconds
                    : FallLandVisualSeconds;
            }
        }

        void ApplyDamage(EnemyRuntime enemy, float damage)
        {
            if (enemy == null || !enemy.IsAlive || damage <= 0f)
                return;

            if (_sourceTower != null)
                enemy.LastDamageSource = _sourceTower;

            var dealt = IncomingHit.ApplyCrit(
                damage,
                _plan.HitSpec,
                _critRng != null ? _critRng.NextDouble() : 1d);
            if (_statuses != null)
                _statuses.ApplyDamage(enemy, dealt, _plan.HitSpec);
            else
                enemy.ApplyDamage(dealt, _plan.HitSpec, null);

            _recordDamage?.Invoke(_sourceTower, dealt);
        }
    }
}
