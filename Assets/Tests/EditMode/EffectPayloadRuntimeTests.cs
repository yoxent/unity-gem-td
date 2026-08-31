using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class EffectPayloadRuntimeTests
    {
        EnemyDefinition _enemyDef;

        [SetUp]
        public void SetUp()
        {
            _enemyDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            _enemyDef.MaxHealth = 100f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyDef);
        }

        [Test]
        public void Fountain_DoesNotDamageBeforeLanding()
        {
            var enemy = MakeEnemy(new Vector3(2f, 0f, 0f));
            var living = Living(enemy);
            var runtime = MakeFountainRuntime(
                origin: Vector3.zero,
                landing: new Vector3(2f, 0f, 0f),
                aoe: 1f,
                damage: 5f);

            runtime.Tick(0.05f, living);
            Assert.AreEqual(100f, enemy.Hp, 1e-4f);
            Assert.IsTrue(runtime.IsActive);
        }

        [Test]
        public void Fountain_ExposesTravelTangent()
        {
            var runtime = MakeFountainRuntime(
                origin: Vector3.zero,
                landing: new Vector3(2f, 0f, 0f),
                aoe: 1f,
                damage: 5f);

            Assert.Greater(runtime.Direction.y, 0f);
            runtime.Tick(0.15f, Living());
            Assert.Less(runtime.Direction.y, 0f);
        }

        [Test]
        public void Fountain_DetonatesAtEndpointWithoutExactCollision()
        {
            var enemy = MakeEnemy(new Vector3(2.5f, 0f, 0f));
            var living = Living(enemy);
            var runtime = MakeFountainRuntime(
                origin: Vector3.zero,
                landing: new Vector3(2f, 0f, 0f),
                aoe: 1f,
                damage: 5f);

            while (runtime.IsActive)
                runtime.Tick(0.05f, living);

            Assert.Less(enemy.Hp, 100f);
        }

        [Test]
        public void Fountain_AoeHitsMultipleEnemies()
        {
            var a = MakeEnemy(new Vector3(2.2f, 0f, 0.3f));
            var b = MakeEnemy(new Vector3(2.2f, 0f, -0.3f));
            var living = Living(a, b);
            var runtime = MakeFountainRuntime(
                origin: Vector3.zero,
                landing: new Vector3(2f, 0f, 0f),
                aoe: 1f,
                damage: 10f);

            while (runtime.IsActive)
                runtime.Tick(0.05f, living);

            Assert.Less(a.Hp, 100f);
            Assert.Less(b.Hp, 100f);
        }

        [Test]
        public void PerImpact_OverlappingAoEsCanShotgunSameEnemy()
        {
            var enemy = MakeEnemy(new Vector3(2f, 0f, 0f));
            var living = Living(enemy);
            var landing = new Vector3(2f, 0f, 0f);
            var first = MakeFountainRuntime(Vector3.zero, landing, 1f, 10f);
            var second = MakeFountainRuntime(Vector3.zero, landing, 1f, 10f);

            while (first.IsActive)
                first.Tick(0.05f, living);
            var hpAfterFirst = enemy.Hp;

            while (second.IsActive)
                second.Tick(0.05f, living);

            Assert.Less(hpAfterFirst, 100f);
            Assert.Less(enemy.Hp, hpAfterFirst);
        }

        [Test]
        public void FallFromSky_DoesNotDamageBeforeLanding()
        {
            var landing = new Vector3(2f, 0f, 0f);
            var enemy = MakeEnemy(landing);
            var living = Living(enemy);
            var plan = new EffectPayloadPlan
            {
                TravelPattern = EffectPayloadTravelPattern.FallFromSky,
                HitPolicy = EffectPayloadHitPolicy.PerImpact,
                Origin = landing + Vector3.up * 3f,
                LandingPoint = landing,
                DamageMin = 5f,
                DamageMax = 5f,
                AoeRadius = 1f
            };
            var runtime = new EffectPayloadRuntime();
            runtime.Init(plan, flightSeconds: 0.2f, statuses: null, sourceTower: null, recordDamage: null);
            runtime.Tick(0.05f, living);
            Assert.AreEqual(100f, enemy.Hp, 1e-4f);
            Assert.IsTrue(runtime.IsActive);
            Assert.AreEqual(Vector3.down, runtime.Direction);
        }

        [Test]
        public void FallFromSky_DetonatesAtLanding()
        {
            var landing = new Vector3(2f, 0f, 0f);
            var enemy = MakeEnemy(landing);
            var living = Living(enemy);
            var plan = new EffectPayloadPlan
            {
                TravelPattern = EffectPayloadTravelPattern.FallFromSky,
                HitPolicy = EffectPayloadHitPolicy.PerImpact,
                Origin = landing + Vector3.up * 3f,
                LandingPoint = landing,
                DamageMin = 5f,
                DamageMax = 5f,
                AoeRadius = 1f
            };
            var runtime = new EffectPayloadRuntime();
            runtime.Init(plan, flightSeconds: 0.2f, statuses: null, sourceTower: null, recordDamage: null);
            while (runtime.IsActive)
                runtime.Tick(0.05f, living);
            Assert.Less(enemy.Hp, 100f);
        }

        [Test]
        public void StationaryPulse_WaitsDelayThenHits()
        {
            var enemy = MakeEnemy(Vector3.zero);
            var living = Living(enemy);
            var plan = new EffectPayloadPlan
            {
                Trigger = EffectPayloadTrigger.AfterDelay,
                TravelPattern = EffectPayloadTravelPattern.StationaryPulse,
                HitPolicy = EffectPayloadHitPolicy.PerImpact,
                Origin = Vector3.zero,
                LandingPoint = Vector3.zero,
                DamageMin = 10f,
                DamageMax = 10f,
                AoeRadius = 2f,
                DelaySeconds = 1f,
                Visual = EffectPayloadVisual.Aftershock
            };
            var runtime = new EffectPayloadRuntime();
            runtime.Init(plan, flightSeconds: 0.08f, statuses: null, sourceTower: null, recordDamage: null);

            runtime.Tick(1f, living);
            Assert.AreEqual(100f, enemy.Hp, 1e-4f);
            Assert.IsTrue(runtime.IsActive);
            Assert.IsFalse(runtime.ShowsSlamVisual);
            Assert.IsFalse(runtime.ShowsAftershockVisual);

            runtime.Tick(0.016f, living);
            Assert.Less(enemy.Hp, 100f);
            Assert.IsTrue(runtime.IsActive);
            Assert.IsFalse(runtime.ShowsSlamVisual);
            Assert.IsTrue(runtime.ShowsAftershockVisual);

            runtime.Tick(EffectPayloadRuntime.StationaryPulseVisualSeconds, living);
            Assert.IsFalse(runtime.IsActive);
            Assert.IsFalse(runtime.ShowsAftershockVisual);
        }

        [Test]
        public void StationaryPulse_ImmediatePulse_ShowsSlamThenExpires()
        {
            var plan = new EffectPayloadPlan
            {
                Trigger = EffectPayloadTrigger.AfterDelay,
                TravelPattern = EffectPayloadTravelPattern.StationaryPulse,
                HitPolicy = EffectPayloadHitPolicy.PerImpact,
                Origin = Vector3.zero,
                LandingPoint = Vector3.zero,
                DamageMin = 0f,
                DamageMax = 0f,
                AoeRadius = 1.8f,
                DelaySeconds = 0f,
                Visual = EffectPayloadVisual.Slam
            };
            var runtime = new EffectPayloadRuntime();
            runtime.Init(plan, flightSeconds: 0.08f, statuses: null, sourceTower: null, recordDamage: null);

            Assert.IsTrue(runtime.ShowsSlamVisual);
            Assert.IsFalse(runtime.ShowsAftershockVisual);
            Assert.IsTrue(runtime.IsActive);

            runtime.Tick(EffectPayloadRuntime.StationaryPulseVisualSeconds, null);
            Assert.IsFalse(runtime.IsActive);
        }

        static EffectPayloadRuntime MakeFountainRuntime(
            Vector3 origin,
            Vector3 landing,
            float aoe,
            float damage)
        {
            var plan = new EffectPayloadPlan
            {
                Trigger = EffectPayloadTrigger.OnImpact,
                TravelPattern = EffectPayloadTravelPattern.Fountain,
                HitPolicy = EffectPayloadHitPolicy.PerImpact,
                Origin = origin,
                LandingPoint = landing,
                DamageMin = damage,
                DamageMax = damage,
                AoeRadius = aoe,
                ArcHeight = 1.5f
            };
            var runtime = new EffectPayloadRuntime();
            runtime.Init(plan, flightSeconds: 0.2f, statuses: null, sourceTower: null, recordDamage: null);
            return runtime;
        }

        EnemyRuntime MakeEnemy(Vector3 pos)
        {
            var enemy = new EnemyRuntime();
            enemy.Init(_enemyDef, new[] { pos });
            return enemy;
        }

        static List<EnemyRuntime> Living(params EnemyRuntime[] enemies)
        {
            var list = new List<EnemyRuntime>(enemies.Length);
            for (var i = 0; i < enemies.Length; i++)
                list.Add(enemies[i]);
            return list;
        }
    }
}
