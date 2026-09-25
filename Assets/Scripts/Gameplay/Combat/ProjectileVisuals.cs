using System;
using System.Collections.Generic;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>
    /// Straight-projectile presentation for one tower. Empty flight uses the shared bolt.
    /// </summary>
    [Serializable]
    public sealed class ProjectileVisual
    {
        public TowerDefinition tower;
        public EffectView flightPrefab;
        public EffectView impactPrefab;
    }

    /// <summary>Pools flight and impact prefabs for straight projectiles. The default bolt is the fallback flight.</summary>
    public sealed class ProjectileVisualPools
    {
        readonly List<TowerDefinition> _towers = new List<TowerDefinition>(8);
        readonly List<ViewObjectPool<EffectView>> _flights = new List<ViewObjectPool<EffectView>>(8);
        readonly List<ViewObjectPool<EffectView>> _impacts = new List<ViewObjectPool<EffectView>>(8);
        readonly Dictionary<EffectView, ViewObjectPool<EffectView>> _owners =
            new Dictionary<EffectView, ViewObjectPool<EffectView>>(64);

        public void Add(TowerDefinition tower, EffectView flightPrefab, EffectView impactPrefab, Transform parent)
        {
            if (tower == null || parent == null)
                return;

            _towers.Add(tower);
            _flights.Add(flightPrefab != null
                ? new ViewObjectPool<EffectView>(flightPrefab, parent, EffectViewBinder.BoltPrewarm)
                : null);
            _impacts.Add(impactPrefab != null
                ? new ViewObjectPool<EffectView>(impactPrefab, parent, EffectViewBinder.ImpactPrewarm)
                : null);
        }

        public void Prewarm()
        {
            for (var i = 0; i < _towers.Count; i++)
            {
                _flights[i]?.Prewarm(EffectViewBinder.BoltPrewarm);
                _impacts[i]?.Prewarm(EffectViewBinder.ImpactPrewarm);
            }
        }

        public ViewObjectPool<EffectView> FlightPool(TowerDefinition tower, ViewObjectPool<EffectView> fallback)
        {
            var index = IndexOf(tower);
            if (index >= 0 && _flights[index] != null)
                return _flights[index];
            return fallback;
        }

        public ViewObjectPool<EffectView> ImpactPool(TowerDefinition tower)
        {
            var index = IndexOf(tower);
            if (index >= 0)
                return _impacts[index];
            return null;
        }

        public EffectView Get(ViewObjectPool<EffectView> pool)
        {
            if (pool == null)
                return null;

            var view = pool.Get();
            if (view != null)
                _owners[view] = pool;
            return view;
        }

        public bool Release(EffectView view)
        {
            if (view == null || !_owners.TryGetValue(view, out var pool))
                return false;

            _owners.Remove(view);
            pool.Release(view);
            return true;
        }

        public void Clear()
        {
            for (var i = 0; i < _towers.Count; i++)
            {
                _flights[i]?.Clear();
                _impacts[i]?.Clear();
            }

            _owners.Clear();
        }

        int IndexOf(TowerDefinition tower)
        {
            if (tower == null)
                return -1;

            for (var i = 0; i < _towers.Count; i++)
            {
                if (_towers[i] == tower)
                    return i;
            }

            return -1;
        }
    }
}
