using System.Collections.Generic;
using UnityEngine;
using GemTD.Core;

namespace GemTD.Gameplay.Combat
{
    /// <summary>Gets/releases bolt vs slam views so StationaryPulse effects do not reuse the bolt mesh.</summary>
    public static class ProjectileViewBinder
    {
        public static void Release(
            ProjectileView view,
            ViewObjectPool<ProjectileView> boltPool,
            ViewObjectPool<ProjectileView> slamPool)
        {
            if (view == null)
                return;

            view.Clear();
            var pool = view.IsSlamEffect ? slamPool : boltPool;
            if (pool != null)
                pool.Release(view);
            else
                Object.Destroy(view.gameObject);
        }

        public static ProjectileView Ensure(
            List<ProjectileView> views,
            int index,
            bool slam,
            ViewObjectPool<ProjectileView> boltPool,
            ViewObjectPool<ProjectileView> slamPool)
        {
            var wantSlam = slam && slamPool != null;
            var pool = wantSlam ? slamPool : boltPool;
            if (index >= views.Count)
            {
                if (pool == null)
                    return null;
                var created = pool.Get();
                views.Add(created);
                return created;
            }

            var existing = views[index];
            var existingIsSlam = existing != null && existing.IsSlamEffect;
            if (existing != null && existingIsSlam == wantSlam)
                return existing;

            if (existing != null)
                Release(existing, boltPool, slamPool);

            if (pool == null)
            {
                views[index] = null;
                return null;
            }

            var next = pool.Get();
            views[index] = next;
            return next;
        }
    }
}
