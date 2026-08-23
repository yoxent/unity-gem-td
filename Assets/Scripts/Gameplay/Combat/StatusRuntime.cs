using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.Combat
{
    public sealed class StatusRuntime
    {
        struct StatusEntry
        {
            public StatusId Id;
            public float Duration;
            public float Magnitude;
            public float InitialDuration;
        }

        readonly Dictionary<EnemyRuntime, List<StatusEntry>> _byEnemy = new Dictionary<EnemyRuntime, List<StatusEntry>>();

        public void Apply(EnemyRuntime enemy, StatusId id, float duration, float magnitude)
        {
            if (enemy == null || duration <= 0f)
                return;

            if (!_byEnemy.TryGetValue(enemy, out var list))
            {
                list = new List<StatusEntry>();
                _byEnemy[enemy] = list;
            }

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id != id)
                    continue;

                list[i] = new StatusEntry
                {
                    Id = id,
                    Duration = duration,
                    Magnitude = magnitude,
                    InitialDuration = duration,
                };
                return;
            }

            list.Add(new StatusEntry
            {
                Id = id,
                Duration = duration,
                Magnitude = magnitude,
                InitialDuration = duration,
            });
        }

        public bool Has(EnemyRuntime enemy, StatusId id)
        {
            if (enemy == null || !_byEnemy.TryGetValue(enemy, out var list))
                return false;

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id == id && list[i].Duration > 0f)
                    return true;
            }

            return false;
        }

        public void Tick(float dt, List<EnemyRuntime> living)
        {
            if (living == null)
                return;

            for (var e = 0; e < living.Count; e++)
            {
                var enemy = living[e];
                if (enemy == null || !enemy.IsAlive || !_byEnemy.TryGetValue(enemy, out var list))
                    continue;

                enemy.MoveSpeedMultiplier = 1f;
                var chillMagnitude = -1f;

                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var entry = list[i];

                    if (entry.Id == StatusId.Ignite && dt > 0f && entry.InitialDuration > 0f)
                    {
                        var tickDt = dt;
                        if (tickDt > entry.Duration)
                            tickDt = entry.Duration;
                        var damage = entry.Magnitude * (tickDt / entry.InitialDuration);
                        if (damage > 0f)
                            enemy.ApplyDamage(damage);
                    }

                    if (dt > 0f)
                    {
                        entry.Duration -= dt;
                        if (entry.Duration <= 0f)
                        {
                            list.RemoveAt(i);
                            continue;
                        }

                        list[i] = entry;
                    }

                    if (entry.Id == StatusId.Chill)
                        chillMagnitude = entry.Magnitude;
                }

                if (chillMagnitude >= 0f)
                    enemy.MoveSpeedMultiplier = chillMagnitude;

                if (list.Count == 0)
                    _byEnemy.Remove(enemy);
            }
        }

        public float ApplyDamage(EnemyRuntime enemy, float amount)
        {
            if (enemy == null || amount <= 0f)
                return 0f;

            var amplified = amount;
            if (_byEnemy.TryGetValue(enemy, out var list))
            {
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].Id == StatusId.Shock && list[i].Duration > 0f)
                    {
                        amplified = amount * list[i].Magnitude;
                        break;
                    }
                }
            }

            enemy.ApplyDamage(amplified);
            return amplified;
        }

        public void ProliferateIgniteChillShock(EnemyRuntime source, float radius, List<EnemyRuntime> living)
        {
            if (source == null || living == null || radius <= 0f)
                return;

            if (!_byEnemy.TryGetValue(source, out var sourceList))
                return;

            var srcPos = source.WorldPosition;
            var radiusSq = radius * radius;

            for (var t = 0; t < living.Count; t++)
            {
                var target = living[t];
                if (target == null || target == source || !target.IsAlive)
                    continue;

                var delta = target.WorldPosition - srcPos;
                if (delta.sqrMagnitude > radiusSq)
                    continue;

                for (var s = 0; s < sourceList.Count; s++)
                {
                    var entry = sourceList[s];
                    if (entry.Duration <= 0f)
                        continue;

                    if (entry.Id == StatusId.Ignite)
                    {
                        var remainingMag = entry.Magnitude;
                        if (entry.InitialDuration > 0f)
                            remainingMag = entry.Magnitude * (entry.Duration / entry.InitialDuration);
                        Apply(target, StatusId.Ignite, entry.Duration, remainingMag);
                    }
                    else if (entry.Id == StatusId.Chill || entry.Id == StatusId.Shock)
                    {
                        Apply(target, entry.Id, entry.Duration, entry.Magnitude);
                    }
                }
            }
        }
    }
}
