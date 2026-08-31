using System.Collections.Generic;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>Gets/releases bolt vs slam vs aftershock views so those meshes stay distinct.</summary>
    public static class ProjectileViewBinder
    {
        public const int BoltPrewarm = 48;
        public const int SlamPrewarm = 16;
        public const int AftershockPrewarm = 16;

        public static void Release(
            ProjectileView view,
            ViewObjectPool<ProjectileView> boltPool,
            ViewObjectPool<ProjectileView> slamPool,
            ViewObjectPool<ProjectileView> aftershockPool)
        {
            if (view == null)
                return;

            view.Clear();
            var pool = PoolForView(view, boltPool, slamPool, aftershockPool);
            if (pool != null)
                pool.Release(view);
            else
                Object.Destroy(view.gameObject);
        }

        public static void SyncLive(
            List<ProjectileView> views,
            IReadOnlyList<ProjectileRuntime> bolts,
            IReadOnlyList<EffectPayloadRuntime> payloads,
            ViewObjectPool<ProjectileView> boltPool,
            ViewObjectPool<ProjectileView> slamPool,
            ViewObjectPool<ProjectileView> aftershockPool)
        {
            if (views == null)
                return;

            var boltCount = bolts != null ? bolts.Count : 0;
            var payloadCount = payloads != null ? payloads.Count : 0;

            for (var i = views.Count - 1; i >= 0; i--)
            {
                var view = views[i];
                if (view != null && IsLive(view, bolts, boltCount, payloads, payloadCount))
                    continue;

                views.RemoveAt(i);
                Release(view, boltPool, slamPool, aftershockPool);
            }

            for (var i = 0; i < boltCount; i++)
            {
                if (HasBoltView(views, bolts[i]))
                    continue;
                var view = Take(boltPool);
                if (view == null)
                    continue;
                views.Add(view);
                view.Bind(bolts[i]);
            }

            for (var i = 0; i < payloadCount; i++)
            {
                var payload = payloads[i];
                if (payload.Plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse
                    && !payload.ShowsPulseVisual)
                    continue;
                if (HasPayloadView(views, payload))
                    continue;
                var view = Take(PoolForPayload(payload, boltPool, slamPool, aftershockPool));
                if (view == null)
                    continue;
                views.Add(view);
                view.Bind(payload);
            }

            for (var i = 0; i < views.Count; i++)
            {
                var view = views[i];
                if (view != null)
                    view.SyncTransform();
            }
        }

        static bool IsLive(
            ProjectileView view,
            IReadOnlyList<ProjectileRuntime> bolts,
            int boltCount,
            IReadOnlyList<EffectPayloadRuntime> payloads,
            int payloadCount)
        {
            if (view.Runtime != null && bolts != null)
            {
                for (var i = 0; i < boltCount; i++)
                {
                    if (ReferenceEquals(view.Runtime, bolts[i]))
                        return true;
                }
            }

            if (view.Payload != null && payloads != null)
            {
                for (var i = 0; i < payloadCount; i++)
                {
                    if (!ReferenceEquals(view.Payload, payloads[i]))
                        continue;
                    if (payloads[i].Plan.TravelPattern == EffectPayloadTravelPattern.StationaryPulse
                        && !payloads[i].ShowsPulseVisual)
                        return false;
                    return true;
                }
            }

            return false;
        }

        static bool HasBoltView(List<ProjectileView> views, ProjectileRuntime bolt)
        {
            for (var i = 0; i < views.Count; i++)
            {
                if (views[i] != null && ReferenceEquals(views[i].Runtime, bolt))
                    return true;
            }

            return false;
        }

        static bool HasPayloadView(List<ProjectileView> views, EffectPayloadRuntime payload)
        {
            for (var i = 0; i < views.Count; i++)
            {
                if (views[i] != null && ReferenceEquals(views[i].Payload, payload))
                    return true;
            }

            return false;
        }

        static ViewObjectPool<ProjectileView> PoolForView(
            ProjectileView view,
            ViewObjectPool<ProjectileView> boltPool,
            ViewObjectPool<ProjectileView> slamPool,
            ViewObjectPool<ProjectileView> aftershockPool)
        {
            if (view != null && view.IsAftershockEffect)
                return aftershockPool != null ? aftershockPool : slamPool;
            if (view != null && view.IsSlamEffect)
                return slamPool;
            return boltPool;
        }

        static ViewObjectPool<ProjectileView> PoolForPayload(
            EffectPayloadRuntime payload,
            ViewObjectPool<ProjectileView> boltPool,
            ViewObjectPool<ProjectileView> slamPool,
            ViewObjectPool<ProjectileView> aftershockPool)
        {
            if (ProjectileView.WantsAftershockEffect(payload))
                return aftershockPool != null ? aftershockPool : slamPool;
            if (ProjectileView.WantsSlamEffect(payload))
                return slamPool;
            return boltPool;
        }

        static ProjectileView Take(ViewObjectPool<ProjectileView> pool)
        {
            return pool != null ? pool.Get() : null;
        }
    }
}
