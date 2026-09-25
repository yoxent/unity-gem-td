using System.Collections.Generic;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.Towers;

namespace GemTD.Gameplay.Combat
{
    /// <summary>Gets/releases pooled views so each delivery family stays distinct.</summary>
    public static class EffectViewBinder
    {
        public const int BoltPrewarm = 48;
        public const int SlamPrewarm = 16;
        public const int AftershockPrewarm = 16;
        public const int FallPrewarm = 48;
        public const int NovaPrewarm = 16;
        public const int WarpPrewarm = 48;
        public const int ChainLightningPrewarm = 24;
        public const int ImpactPrewarm = 16;

        static readonly List<Vector3> PendingImpacts = new List<Vector3>(8);
        static readonly List<TowerDefinition> PendingImpactTowers = new List<TowerDefinition>(8);

        public static void Release(
            EffectView view,
            ViewObjectPool<EffectView> boltPool,
            ViewObjectPool<EffectView> slamPool,
            ViewObjectPool<EffectView> aftershockPool,
            ViewObjectPool<EffectView> fallPool,
            ViewObjectPool<EffectView> novaPool,
            ViewObjectPool<EffectView> warpPool,
            ViewObjectPool<EffectView> chainLightningPool,
            ProjectileVisualPools visuals)
        {
            if (view == null)
                return;

            view.Clear();
            if (visuals != null && visuals.Release(view))
                return;

            var pool = PoolForView(
                view,
                boltPool,
                slamPool,
                aftershockPool,
                fallPool,
                novaPool,
                warpPool,
                chainLightningPool);
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
            ViewObjectPool<EffectView> fallPool,
            ViewObjectPool<EffectView> novaPool,
            ViewObjectPool<EffectView> warpPool,
            ViewObjectPool<EffectView> chainLightningPool,
            ProjectileVisualPools visuals,
            float dt)
        {
            if (views == null)
                return;

            PendingImpacts.Clear();
            PendingImpactTowers.Clear();
            var boltCount = bolts != null ? bolts.Count : 0;
            var payloadCount = payloads != null ? payloads.Count : 0;

            for (var i = 0; i < views.Count; i++)
            {
                if (views[i] is ImpactEffectView impact)
                    impact.Tick(dt);
            }

            for (var i = views.Count - 1; i >= 0; i--)
            {
                var view = views[i];
                if (view != null && IsLive(view, bolts, boltCount, payloads, payloadCount))
                    continue;

                CollectImpact(view, visuals);
                views.RemoveAt(i);
                Release(
                    view,
                    boltPool,
                    slamPool,
                    aftershockPool,
                    fallPool,
                    novaPool,
                    warpPool,
                    chainLightningPool,
                    visuals);
            }

            for (var i = 0; i < boltCount; i++)
            {
                if (HasRuntimeView(views, bolts[i]))
                    continue;
                var pool = PoolForRuntime(
                    bolts[i],
                    boltPool,
                    warpPool,
                    chainLightningPool,
                    visuals);
                var view = visuals != null ? visuals.Get(pool) : Take(pool);
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
                var view = Take(
                    PoolForPayload(
                        payload,
                        boltPool,
                        slamPool,
                        aftershockPool,
                        fallPool,
                        novaPool,
                        warpPool));
                if (view == null)
                    continue;
                views.Add(view);
                view.Bind(payload);
            }

            for (var i = 0; i < views.Count; i++)
                CollectImpact(views[i], visuals);

            for (var i = 0; i < PendingImpacts.Count; i++)
            {
                var pool = visuals != null ? visuals.ImpactPool(PendingImpactTowers[i]) : null;
                var spawned = visuals != null ? visuals.Get(pool) : null;
                if (spawned is not ImpactEffectView impact)
                {
                    if (spawned != null)
                        visuals.Release(spawned);
                    continue;
                }

                views.Add(impact);
                impact.Begin(PendingImpacts[i]);
            }

            for (var i = 0; i < views.Count; i++)
            {
                var view = views[i];
                if (view != null)
                    view.SyncTransform();
            }
        }

        static void CollectImpact(EffectView view, ProjectileVisualPools visuals)
        {
            if (visuals == null || view is not BoltEffectView bolt)
                return;
            if (!bolt.TryConsumeImpact(out var position))
                return;

            var tower = bolt.Runtime != null ? bolt.Runtime.SourceTower : null;
            if (visuals.ImpactPool(tower) == null)
                return;

            PendingImpacts.Add(position);
            PendingImpactTowers.Add(tower);
        }

        static bool IsLive(
            EffectView view,
            IReadOnlyList<ProjectileRuntime> bolts,
            int boltCount,
            IReadOnlyList<EffectPayloadRuntime> payloads,
            int payloadCount)
        {
            if (view is ImpactEffectView impact)
                return !impact.IsFinished;

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

        static bool HasRuntimeView(List<EffectView> views, ProjectileRuntime bolt)
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
            ViewObjectPool<EffectView> fallPool,
            ViewObjectPool<EffectView> novaPool,
            ViewObjectPool<EffectView> warpPool,
            ViewObjectPool<EffectView> chainLightningPool)
        {
            if (view is ChainLightningEffectView)
                return chainLightningPool != null ? chainLightningPool : boltPool;
            if (view is WarpEffectView)
                return warpPool != null ? warpPool : boltPool;
            if (view is NovaEffectView)
                return novaPool != null
                    ? novaPool
                    : slamPool != null ? slamPool : boltPool;
            if (view is FallEffectView)
                return fallPool != null ? fallPool : boltPool;
            if (view is AftershockEffectView)
                return aftershockPool != null ? aftershockPool : slamPool;
            if (view is SlamEffectView)
                return slamPool;
            return boltPool;
        }

        static ViewObjectPool<EffectView> PoolForRuntime(
            ProjectileRuntime runtime,
            ViewObjectPool<EffectView> boltPool,
            ViewObjectPool<EffectView> warpPool,
            ViewObjectPool<EffectView> chainLightningPool,
            ProjectileVisualPools visuals)
        {
            if (runtime != null && runtime.ChainRemaining > 0)
                return chainLightningPool != null ? chainLightningPool : boltPool;
            if (runtime != null && runtime.IsWarpStrike)
                return warpPool != null ? warpPool : boltPool;
            var tower = runtime != null ? runtime.SourceTower : null;
            return visuals != null ? visuals.FlightPool(tower, boltPool) : boltPool;
        }

        static ViewObjectPool<EffectView> PoolForPayload(
            EffectPayloadRuntime payload,
            ViewObjectPool<EffectView> boltPool,
            ViewObjectPool<EffectView> slamPool,
            ViewObjectPool<EffectView> aftershockPool,
            ViewObjectPool<EffectView> fallPool,
            ViewObjectPool<EffectView> novaPool,
            ViewObjectPool<EffectView> warpPool)
        {
            if (EffectView.WantsWarpEffect(payload))
                return warpPool != null ? warpPool : boltPool;
            if (EffectView.WantsNovaEffect(payload))
                return novaPool != null
                    ? novaPool
                    : slamPool != null ? slamPool : boltPool;
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
