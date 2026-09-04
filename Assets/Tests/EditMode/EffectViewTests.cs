using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class EffectViewTests
    {
        [Test]
        public void BindSlam_KeepsParentScaleOne_ScalesChildToAoeDiameter()
        {
            var (view, scaleRoot) = MakeView<SlamEffectView>(Vector3.one);
            try
            {
                view.Bind(MakeSlamPayload(new Vector3(3f, 0.5f, 4f), aoeRadius: 2.8f));

                Assert.AreEqual(Vector3.one, view.transform.localScale);
                var expected = SlamEffectVisual.ScaleToDiameter(2.8f);
                Assert.AreEqual(expected.x, scaleRoot.localScale.x, 0.0001f);
                Assert.AreEqual(expected.y, scaleRoot.localScale.y, 0.0001f);
                Assert.AreEqual(expected.z, scaleRoot.localScale.z, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }

        [Test]
        public void BindSlam_PlacesAndRotatesParent_LeavesChildLocalPose()
        {
            var (view, scaleRoot) = MakeView<SlamEffectView>(Vector3.one);
            scaleRoot.localPosition = new Vector3(0.1f, 0.2f, 0.3f);
            scaleRoot.localRotation = Quaternion.Euler(10f, 20f, 30f);
            try
            {
                var landing = new Vector3(3f, 0.5f, 4f);
                view.Bind(MakeSlamPayload(landing, aoeRadius: 1.8f));

                var groundY = landing.y + TileHeightVisual.PathScaleY * 0.5f;
                var expected = SlamEffectVisual.SitOnGround(new Vector3(landing.x, groundY, landing.z), 1f, 1f);
                Assert.AreEqual(expected.x, view.transform.position.x, 0.0001f);
                Assert.AreEqual(expected.y, view.transform.position.y, 0.0001f);
                Assert.AreEqual(expected.z, view.transform.position.z, 0.0001f);
                Assert.AreEqual(Quaternion.identity, view.transform.rotation);

                Assert.AreEqual(0.1f, scaleRoot.localPosition.x, 0.0001f);
                Assert.AreEqual(0.2f, scaleRoot.localPosition.y, 0.0001f);
                Assert.AreEqual(0.3f, scaleRoot.localPosition.z, 0.0001f);
                Assert.AreEqual(0f, Quaternion.Angle(Quaternion.Euler(10f, 20f, 30f), scaleRoot.localRotation), 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }

        [Test]
        public void BindFountain_ScalesChildFromAuthored_KeepsParentScaleOne()
        {
            var authored = new Vector3(0.15f, 0.15f, 0.4f);
            var (view, scaleRoot) = MakeView<BoltEffectView>(authored);
            try
            {
                view.Bind(MakeFountainPayload(Vector3.zero, Vector3.forward));

                Assert.AreEqual(Vector3.one, view.transform.localScale);
                Assert.AreEqual(authored.x * 0.65f, scaleRoot.localScale.x, 0.0001f);
                Assert.AreEqual(authored.y * 0.65f, scaleRoot.localScale.y, 0.0001f);
                Assert.AreEqual(authored.z * 0.65f, scaleRoot.localScale.z, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }

        [Test]
        public void BindFallFromSky_DisablesColliderAfterLand_RestoresOnClear()
        {
            var (view, _) = MakeView<FallEffectView>(Vector3.one);
            var collider = view.gameObject.AddComponent<SphereCollider>();
            collider.enabled = true;
            try
            {
                var landing = new Vector3(2f, 0f, 0f);
                var runtime = new EffectPayloadRuntime();
                runtime.Init(
                    new EffectPayloadPlan
                    {
                        TravelPattern = EffectPayloadTravelPattern.FallFromSky,
                        HitPolicy = EffectPayloadHitPolicy.PerImpact,
                        Origin = landing + Vector3.up * 3f,
                        LandingPoint = landing,
                        DamageMin = 5f,
                        DamageMax = 5f,
                        AoeRadius = 1f
                    },
                    flightSeconds: 0.2f,
                    statuses: null,
                    sourceTower: null,
                    recordDamage: null);

                view.Bind(runtime);
                runtime.Tick(0.05f, null);
                view.SyncTransform();
                Assert.IsTrue(collider.enabled);

                runtime.Tick(0.2f, null);
                view.SyncTransform();
                Assert.IsTrue(runtime.HasResolvedImpact);
                Assert.IsFalse(collider.enabled);

                view.Clear();
                Assert.IsTrue(collider.enabled);
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }

        [Test]
        public void BindFallFromSky_DoesNotRotateParent_LeavesChildLocalPose()
        {
            var (view, scaleRoot) = MakeView<FallEffectView>(Vector3.one);
            scaleRoot.localRotation = Quaternion.Euler(90f, 0f, 0f);
            view.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            try
            {
                var landing = new Vector3(2f, 0f, 0f);
                var runtime = new EffectPayloadRuntime();
                runtime.Init(
                    new EffectPayloadPlan
                    {
                        TravelPattern = EffectPayloadTravelPattern.FallFromSky,
                        HitPolicy = EffectPayloadHitPolicy.PerImpact,
                        Origin = landing + Vector3.up * 3f,
                        LandingPoint = landing,
                        DamageMin = 5f,
                        DamageMax = 5f,
                        AoeRadius = 1f
                    },
                    flightSeconds: 0.2f,
                    statuses: null,
                    sourceTower: null,
                    recordDamage: null);

                runtime.Tick(0.05f, null);
                view.Bind(runtime);

                Assert.AreEqual(landing.x, view.transform.position.x, 0.0001f);
                Assert.Less(view.transform.position.y, landing.y + 3f);
                Assert.AreEqual(Quaternion.identity, view.transform.rotation);
                Assert.AreEqual(0f, Quaternion.Angle(Quaternion.Euler(90f, 0f, 0f), scaleRoot.localRotation), 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(view.gameObject);
            }
        }

        [Test]
        public void BindFallFromSky_PlaysDrop_ThenImpactOnLand_StopsOnClear()
        {
            var go = new GameObject("Fall");
            var scale = new GameObject("Scale");
            scale.transform.SetParent(go.transform, false);
            var dropGo = new GameObject("Drop");
            dropGo.transform.SetParent(go.transform, false);
            var drop = dropGo.AddComponent<ParticleSystem>();
            drop.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var impactGo = new GameObject("Impact");
            impactGo.transform.SetParent(go.transform, false);
            var impact = impactGo.AddComponent<ParticleSystem>();
            impact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var view = go.AddComponent<FallEffectView>();
            var so = new SerializedObject(view);
            so.FindProperty("scaleRoot").objectReferenceValue = scale.transform;
            so.FindProperty("fallDrop").objectReferenceValue = drop;
            var impacts = so.FindProperty("fallImpacts");
            impacts.arraySize = 1;
            impacts.GetArrayElementAtIndex(0).objectReferenceValue = impact;
            so.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                var landing = new Vector3(2f, 0f, 0f);
                var runtime = new EffectPayloadRuntime();
                runtime.Init(
                    new EffectPayloadPlan
                    {
                        TravelPattern = EffectPayloadTravelPattern.FallFromSky,
                        HitPolicy = EffectPayloadHitPolicy.PerImpact,
                        Origin = landing + Vector3.up * 3f,
                        LandingPoint = landing,
                        DamageMin = 5f,
                        DamageMax = 5f,
                        AoeRadius = 1f
                    },
                    flightSeconds: 0.2f,
                    statuses: null,
                    sourceTower: null,
                    recordDamage: null);

                runtime.Tick(0.05f, null);
                view.Bind(runtime);
                Assert.IsTrue(drop.isPlaying);
                Assert.IsFalse(impact.isPlaying);

                runtime.Tick(0.2f, null);
                view.SyncTransform();
                Assert.IsTrue(runtime.HasResolvedImpact);
                Assert.IsFalse(drop.isPlaying);
                Assert.IsTrue(impact.isPlaying);

                view.Clear();
                Assert.IsFalse(drop.isPlaying);
                Assert.IsFalse(impact.isPlaying);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static (T view, Transform scaleRoot) MakeView<T>(Vector3 authoredScale) where T : EffectView
        {
            var go = new GameObject("Payload");
            var child = new GameObject("Scale");
            child.transform.SetParent(go.transform, false);
            child.transform.localScale = authoredScale;
            var view = go.AddComponent<T>();
            var so = new SerializedObject(view);
            so.FindProperty("scaleRoot").objectReferenceValue = child.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return (view, child.transform);
        }

        static EffectPayloadRuntime MakeSlamPayload(Vector3 landing, float aoeRadius)
        {
            var runtime = new EffectPayloadRuntime();
            runtime.Init(
                new EffectPayloadPlan
                {
                    Trigger = EffectPayloadTrigger.AfterDelay,
                    TravelPattern = EffectPayloadTravelPattern.StationaryPulse,
                    HitPolicy = EffectPayloadHitPolicy.PerImpact,
                    Origin = landing,
                    LandingPoint = landing,
                    DamageMin = 0f,
                    DamageMax = 0f,
                    AoeRadius = aoeRadius,
                    DelaySeconds = 0f,
                    Visual = EffectPayloadVisual.Slam
                },
                flightSeconds: 0.08f,
                statuses: null,
                sourceTower: null,
                recordDamage: null);
            return runtime;
        }

        static EffectPayloadRuntime MakeFountainPayload(Vector3 origin, Vector3 landing)
        {
            var runtime = new EffectPayloadRuntime();
            runtime.Init(
                new EffectPayloadPlan
                {
                    Trigger = EffectPayloadTrigger.OnImpact,
                    TravelPattern = EffectPayloadTravelPattern.Fountain,
                    HitPolicy = EffectPayloadHitPolicy.PerImpact,
                    Origin = origin,
                    LandingPoint = landing,
                    DamageMin = 1f,
                    DamageMax = 1f,
                    AoeRadius = 0.5f
                },
                flightSeconds: 0.2f,
                statuses: null,
                sourceTower: null,
                recordDamage: null);
            return runtime;
        }
    }
}
