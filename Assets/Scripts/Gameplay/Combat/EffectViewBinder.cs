using System.Collections.Generic;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>Gets/releases bolt vs slam vs aftershock vs fall views so those meshes stay distinct.</summary>
    public static class EffectViewBinder
    {
        public const int BoltPrewarm = 48;
        public const int SlamPrewarm = 16;
        public const int AftershockPrewarm = 16;
        public const int FallPrewarm = 48;

        public static void Release(
            EffectView view,
            ViewObjectPool<EffectView> boltPool,
            ViewObjectPool<EffectView> slamPool,
            ViewObjectPool<EffectView> aftershockPool,
            ViewObjectPool<EffectView> fallPool)
        {
            if (view == null)
                return;

            view.Clear();
            var pool = PoolForView(view, boltPool, slamPool, aftershockPool, fallPool);
            if (pool != null)
                pool.Release(view);
            else
                Object.Destroy(view.gameObject);
        }

        public static void SyncLive(
            List<EffectView> views,
            IReadOnlyList<ProjectileRuntime> bolts,
            IReadOnlyList<EffectPayloadRuntime> payloads,
            ViewObjectPool<EffectView> boltPool,
            ViewObjectPool<EffectView> slamPool,
            ViewObjectPool<EffectView> aftershockPool,
            ViewObjectPool<EffectView> fallPool)
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
                Release(view, boltPool, slamPool, aftershockPool, fallPool);
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
                if (payload.Plan.TravelPattern == EffectPayloadTravelPattern.FallFromSky
                    && !payload.ShowsFallVisual)
                    continue;
                if (HasPayloadView(views, payload))
                    continue;
                var view = Take(PoolForPayload(payload, boltPool, slamPool, aftershockPool, fallPool));
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
            EffectView view,
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
                    if (payloads[i].Plan.TravelPattern == EffectPayloadTravelPattern.FallFromSky
                        && !payloads[i].ShowsFallVisual)
                        return false;
                    return true;
                }
            }

            return false;
        }

        static bool HasBoltView(List<EffectView> views, ProjectileRuntime bolt)
        {
            for (var i = 0; i < views.Count; i++)
            {
                if (views[i] != null && ReferenceEquals(views[i].Runtime, bolt))
                    return true;
            }

            return false;
        }

        static bool HasPayloadView(List<EffectView> views, EffectPayloadRuntime payload)
        {
            for (var i = 0; i < views.Count; i++)
            {
                if (views[i] != null && ReferenceEquals(views[i].Payload, payload))
                    return true;
            }

            return false;
        }

        static ViewObjectPool<EffectView> PoolForView(
            EffectView view,
            ViewObjectPool<EffectView> boltPool,
            ViewObjectPool<EffectView> slamPool,
            ViewObjectPool<EffectView> aftershockPool,
            ViewObjectPool<EffectView> fallPool)
        {
            if (view is FallEffectView)
                return fallPool != null ? fallPool : boltPool;
            if (view is AftershockEffectView)
                return aftershockPool != null ? aftershockPool : slamPool;
            if (view is SlamEffectView)
                return slamPool;
            return boltPool;
        }

        static ViewObjectPool<EffectView> PoolForPayload(
            EffectPayloadRuntime payload,
            ViewObjectPool<EffectView> boltPool,
            ViewObjectPool<EffectView> slamPool,
            ViewObjectPool<EffectView> aftershockPool,
            ViewObjectPool<EffectView> fallPool)
        {
            if (EffectView.WantsFallEffect(payload))
                return fallPool != null ? fallPool : boltPool;
            if (EffectView.WantsAftershockEffect(payload))
                return aftershockPool != null ? aftershockPool : slamPool;
            if (EffectView.WantsSlamEffect(payload))
                return slamPool;
            return boltPool;
        }

        static EffectView Take(ViewObjectPool<EffectView> pool)
        {
            return pool != null ? pool.Get() : null;
        }
    }
}
