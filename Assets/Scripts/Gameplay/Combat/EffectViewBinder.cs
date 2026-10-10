using System.Collections.Generic;
using GemTD.Gameplay.Towers;
using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    /// <summary>Shows the effect prefabs assigned on each tower.</summary>
    public static class EffectViewBinder
    {
        static readonly List<Vector3> PendingImpacts = new List<Vector3>(8);
        static readonly List<TowerDefinition> PendingImpactTowers = new List<TowerDefinition>(8);
        static readonly List<EffectView> Overlays = new List<EffectView>(8);
        static readonly List<float> OverlayAges = new List<float>(8);

        public static void SyncLive(
            List<EffectView> views,
            IReadOnlyList<ProjectileRuntime> bolts,
            IReadOnlyList<EffectPayloadRuntime> payloads,
            EffectViewPoolRegistry pools,
            IReadOnlyList<CastMoment> casts,
            float dt)
        {
            if (views == null)
                return;

            if (dt < 0f)
                dt = 0f;

            TickOverlays(dt);
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

                CollectBoltImpact(view);
                CollectPayloadImpact(view, pools, views);
                ForgetOverlay(view);
                views.RemoveAt(i);
                Release(view, pools);
            }

            for (var i = 0; i < boltCount; i++)
            {
                if (HasRuntimeView(views, bolts[i]))
                    continue;
                var prefab = FlightPrefab(bolts[i]);
                var view = pools != null ? pools.Get(prefab) : null;
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
                var prefab = TowerVfx.ResolvePayloadView(VfxOf(payload), payload.Plan);
                var view = pools != null ? pools.Get(prefab) : null;
                if (view == null)
                    continue;
                views.Add(view);
                view.Bind(payload);
            }

            for (var i = 0; i < views.Count; i++)
                CollectBoltImpact(views[i]);

            for (var i = 0; i < PendingImpacts.Count; i++)
            {
                var tower = PendingImpactTowers[i];
                var prefab = tower != null && tower.Vfx != null ? tower.Vfx.impactPrefab : null;
                PlayOverlay(pools, views, prefab, PendingImpacts[i]);
            }

            PlayCasts(pools, views, casts);

            for (var i = 0; i < views.Count; i++)
            {
                var view = views[i];
                if (view != null)
                    view.SyncTransform();
            }
        }

        static void PlayCasts(
            EffectViewPoolRegistry pools,
            List<EffectView> views,
            IReadOnlyList<CastMoment> casts)
        {
            if (casts == null)
                return;

            for (var i = 0; i < casts.Count; i++)
            {
                var moment = casts[i];
                var prefab = moment.Tower != null && moment.Tower.Vfx != null
                    ? moment.Tower.Vfx.castPrefab
                    : null;
                PlayOverlay(pools, views, prefab, moment.Position);
            }
        }

        static void CollectPayloadImpact(
            EffectView view,
            EffectViewPoolRegistry pools,
            List<EffectView> views)
        {
            var payload = view != null ? view.Payload : null;
            if (payload == null || payload.Plan.PayloadIndex < 0 || !payload.HasResolvedImpact)
                return;

            var travel = payload.Plan.TravelPattern;
            if (travel == EffectPayloadTravelPattern.StationaryPulse
                || travel == EffectPayloadTravelPattern.FallFromSky)
                return;

            PlayOverlay(pools, views, payload.Plan.ImpactPrefab, payload.LandingPoint);
        }

        static void PlayOverlay(
            EffectViewPoolRegistry pools,
            List<EffectView> views,
            EffectView prefab,
            Vector3 position)
        {
            if (prefab == null || pools == null)
                return;

            var spawned = pools.Get(prefab);
            if (spawned == null)
                return;

            if (spawned is ImpactEffectView impact)
                impact.Begin(position);
            else
                spawned.SnapTo(position);

            views.Add(spawned);
            Overlays.Add(spawned);
            OverlayAges.Add(0f);
        }

        static void TickOverlays(float dt)
        {
            for (var i = 0; i < OverlayAges.Count; i++)
                OverlayAges[i] += dt;
        }

        static bool IsLiveOverlay(EffectView view)
        {
            for (var i = 0; i < Overlays.Count; i++)
            {
                if (!ReferenceEquals(Overlays[i], view))
                    continue;
                return OverlayAges[i] < TowerVfx.CastLifetimeSeconds;
            }

            return false;
        }

        static void ForgetOverlay(EffectView view)
        {
            for (var i = Overlays.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(Overlays[i], view))
                    continue;
                Overlays.RemoveAt(i);
                OverlayAges.RemoveAt(i);
            }
        }

        static void Release(EffectView view, EffectViewPoolRegistry pools)
        {
            if (view == null)
                return;

            view.Clear();
            if (pools != null && pools.Release(view))
                return;

            Object.Destroy(view.gameObject);
        }

        static EffectView FlightPrefab(ProjectileRuntime runtime)
        {
            if (runtime == null || runtime.SourceTower == null)
                return null;
            var vfx = runtime.SourceTower.Vfx;
            if (vfx == null)
                return null;
            return vfx.ResolveFlight(runtime.ChainRemaining > 0);
        }

        static TowerVfx VfxOf(EffectPayloadRuntime payload)
        {
            if (payload == null)
                return null;
            var tower = payload.SourceTower;
            if (tower == null && payload.Owner != null)
                tower = payload.Owner.Def;
            return tower != null ? tower.Vfx : null;
        }

        static void CollectBoltImpact(EffectView view)
        {
            if (view is not BoltEffectView bolt)
                return;
            if (!bolt.TryConsumeImpact(out var position))
                return;

            var tower = bolt.Runtime != null ? bolt.Runtime.SourceTower : null;
            if (tower == null || tower.Vfx == null || tower.Vfx.impactPrefab == null)
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
            if (IsLiveOverlay(view))
                return true;

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
    }
}
