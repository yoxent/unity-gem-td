using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerVfxTests
    {
        [Test]
        public void ChainingShot_UsesChainPrefab()
        {
            var flight = Make<BoltEffectView>();
            var chain = Make<ChainLightningEffectView>();
            try
            {
                var vfx = new TowerVfx { flightPrefab = flight, chainPrefab = chain };
                Assert.AreSame(chain, vfx.ResolveFlight(true));
                Assert.AreSame(flight, vfx.ResolveFlight(false));
            }
            finally
            {
                Object.DestroyImmediate(flight.gameObject);
                Object.DestroyImmediate(chain.gameObject);
            }
        }

        [Test]
        public void PrimaryLanding_UsesImpactPrefab()
        {
            var flight = Make<BoltEffectView>();
            var impact = Make<WarpEffectView>();
            try
            {
                var vfx = new TowerVfx { flightPrefab = flight, impactPrefab = impact };
                var plan = new EffectPayloadPlan
                {
                    PayloadIndex = TowerVfx.UnassignedPayload,
                    TravelPattern = EffectPayloadTravelPattern.StationaryPulse
                };
                Assert.AreSame(impact, TowerVfx.ResolvePayloadView(vfx, plan));
            }
            finally
            {
                Object.DestroyImmediate(flight.gameObject);
                Object.DestroyImmediate(impact.gameObject);
            }
        }

        [Test]
        public void PayloadRow_FlightWhileTraveling_ImpactWhenStationary()
        {
            var ball = Make<BoltEffectView>();
            var hit = Make<SlamEffectView>();
            try
            {
                var vfx = new TowerVfx { flightPrefab = ball };
                var flying = new EffectPayloadPlan
                {
                    PayloadIndex = 0,
                    TravelPattern = EffectPayloadTravelPattern.Fountain,
                    FlightPrefab = ball,
                    ImpactPrefab = hit
                };
                var landed = new EffectPayloadPlan
                {
                    PayloadIndex = 0,
                    TravelPattern = EffectPayloadTravelPattern.StationaryPulse,
                    FlightPrefab = ball,
                    ImpactPrefab = hit
                };
                Assert.AreSame(ball, TowerVfx.ResolvePayloadView(vfx, flying));
                Assert.AreSame(hit, TowerVfx.ResolvePayloadView(vfx, landed));
            }
            finally
            {
                Object.DestroyImmediate(ball.gameObject);
                Object.DestroyImmediate(hit.gameObject);
            }
        }

        [Test]
        public void EmptyAftershock_UsesTowerImpact()
        {
            var flight = Make<BoltEffectView>();
            var impact = Make<SlamEffectView>();
            try
            {
                var vfx = new TowerVfx { flightPrefab = flight, impactPrefab = impact };
                var plan = new EffectPayloadPlan
                {
                    PayloadIndex = 0,
                    TravelPattern = EffectPayloadTravelPattern.StationaryPulse,
                    Visual = EffectPayloadVisual.Aftershock
                };
                Assert.AreSame(impact, TowerVfx.ResolvePayloadView(vfx, plan));
            }
            finally
            {
                Object.DestroyImmediate(flight.gameObject);
                Object.DestroyImmediate(impact.gameObject);
            }
        }

        [Test]
        public void EmptyTravelingPayload_UsesTowerFlight()
        {
            var flight = Make<BoltEffectView>();
            var impact = Make<SlamEffectView>();
            try
            {
                var vfx = new TowerVfx { flightPrefab = flight, impactPrefab = impact };
                var plan = new EffectPayloadPlan
                {
                    PayloadIndex = 0,
                    TravelPattern = EffectPayloadTravelPattern.Fountain,
                    Visual = EffectPayloadVisual.None
                };
                Assert.AreSame(flight, TowerVfx.ResolvePayloadView(vfx, plan));
            }
            finally
            {
                Object.DestroyImmediate(flight.gameObject);
                Object.DestroyImmediate(impact.gameObject);
            }
        }

        static T Make<T>() where T : Component
        {
            var go = new GameObject(typeof(T).Name);
            return go.AddComponent<T>();
        }
    }
}
