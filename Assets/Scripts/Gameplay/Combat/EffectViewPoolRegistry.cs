using System.Collections.Generic;
using GemTD.Core;
using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    /// <summary>Pools effect views by the prefab assigned on a tower.</summary>
    public sealed class EffectViewPoolRegistry
    {
        readonly Transform _parent;
        readonly Dictionary<EffectView, ViewObjectPool<EffectView>> _pools =
            new Dictionary<EffectView, ViewObjectPool<EffectView>>(16);
        readonly Dictionary<EffectView, ViewObjectPool<EffectView>> _owners =
            new Dictionary<EffectView, ViewObjectPool<EffectView>>(64);

        public EffectViewPoolRegistry(Transform parent)
        {
            _parent = parent;
        }

        public EffectView Get(EffectView prefab)
        {
            if (prefab == null || _parent == null)
                return null;

            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new ViewObjectPool<EffectView>(prefab, _parent, PrewarmFor(prefab));
                _pools.Add(prefab, pool);
                pool.Prewarm(PrewarmFor(prefab));
            }

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
            foreach (var pool in _pools.Values)
                pool.Clear();
            _pools.Clear();
            _owners.Clear();
        }

        static int PrewarmFor(EffectView prefab)
        {
            if (prefab is WarpEffectView || prefab is FallEffectView || prefab is BoltEffectView)
                return 24;
            return 8;
        }
    }
}
