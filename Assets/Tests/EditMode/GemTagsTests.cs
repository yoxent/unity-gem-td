using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class GemTagsTests
    {
        TowerDefinition _ballista;
        TowerDefinition _cannon;
        TowerDefinition _beacon;
        GemDefinition _lmp;
        GemDefinition _chain;
        GemDefinition _area;
        GemDefinition _supportOnly;
        GemDefinition _faster;

        [SetUp]
        public void SetUp()
        {
            _ballista = ScriptableObject.CreateInstance<TowerDefinition>();
            _ballista.Kind = TowerKind.Projectile;
            _ballista.SocketCount = 3;

            _cannon = ScriptableObject.CreateInstance<TowerDefinition>();
            _cannon.Kind = TowerKind.Splash;
            _cannon.SocketCount = 3;

            _beacon = ScriptableObject.CreateInstance<TowerDefinition>();
            _beacon.Kind = TowerKind.Aura;
            _beacon.SocketCount = 1;

            _lmp = ScriptableObject.CreateInstance<GemDefinition>();
            _lmp.Id = GemId.MultipleProjectiles;
            _lmp.Tags = GemTag.Support | GemTag.Projectile;

            _chain = ScriptableObject.CreateInstance<GemDefinition>();
            _chain.Id = GemId.Chain;
            _chain.Tags = GemTag.Support | GemTag.Chaining | GemTag.Projectile;

            _area = ScriptableObject.CreateInstance<GemDefinition>();
            _area.Id = GemId.IncreasedArea;
            _area.Tags = GemTag.Support | GemTag.Aoe;

            _supportOnly = ScriptableObject.CreateInstance<GemDefinition>();
            _supportOnly.Id = GemId.Pierce;
            _supportOnly.Tags = GemTag.Support;

            _faster = ScriptableObject.CreateInstance<GemDefinition>();
            _faster.Id = GemId.FasterAttacks;
            _faster.Tags = GemTag.Attack | GemTag.Support;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ballista);
            Object.DestroyImmediate(_cannon);
            Object.DestroyImmediate(_beacon);
            Object.DestroyImmediate(_lmp);
            Object.DestroyImmediate(_chain);
            Object.DestroyImmediate(_area);
            Object.DestroyImmediate(_supportOnly);
            Object.DestroyImmediate(_faster);
        }

        [Test]
        public void Infer_BallistaAttackProjectile_CannonAddsAoe_BeaconAura()
        {
            Assert.AreEqual(GemTag.Attack | GemTag.Projectile, GemTags.EffectiveTowerTags(_ballista));
            Assert.AreEqual(GemTag.Attack | GemTag.Projectile | GemTag.Aoe, GemTags.EffectiveTowerTags(_cannon));
            Assert.AreEqual(GemTag.Aura, GemTags.EffectiveTowerTags(_beacon));
        }

        [Test]
        public void ProjectileGem_SocketsBallistaAndCannon_NotBeacon()
        {
            Assert.IsTrue(GemTags.CanSocket(_ballista, _lmp));
            Assert.IsTrue(GemTags.CanSocket(_cannon, _lmp));
            Assert.IsFalse(GemTags.CanSocket(_beacon, _lmp));

            var ballista = new TowerRuntime(Vector2Int.zero, _ballista);
            var beacon = new TowerRuntime(Vector2Int.zero, _beacon);
            Assert.IsTrue(ballista.TrySocket(_lmp, 0, allowSocket: true));
            Assert.IsFalse(beacon.TrySocket(_lmp, 0, allowSocket: true));
        }

        [Test]
        public void Chain_RequiresProjectile_NotChaining()
        {
            Assert.AreEqual(GemTag.Projectile, GemTags.EffectiveRequiredTags(_chain));
            Assert.IsTrue(GemTags.CanSocket(_ballista, _chain));
        }

        [Test]
        public void IncreasedArea_SocketsCannon_NotBallista()
        {
            Assert.IsTrue(GemTags.CanSocket(_cannon, _area));
            Assert.IsFalse(GemTags.CanSocket(_ballista, _area));
        }

        [Test]
        public void SupportOnlyGem_SocketsAnyTower()
        {
            Assert.IsTrue(GemTags.CanSocket(_ballista, _supportOnly));
            Assert.IsTrue(GemTags.CanSocket(_cannon, _supportOnly));
            Assert.IsTrue(GemTags.CanSocket(_beacon, _supportOnly));
        }

        [Test]
        public void AttackGem_SocketsBallista_NotBeacon()
        {
            Assert.IsTrue(GemTags.CanSocket(_ballista, _faster));
            Assert.IsFalse(GemTags.CanSocket(_beacon, _faster));
        }

        [Test]
        public void Format_JoinsActiveTags()
        {
            Assert.AreEqual("—", GemTags.Format(GemTag.None));
            Assert.AreEqual("Attack, Projectile, AoE", GemTags.Format(GemTag.Attack | GemTag.Projectile | GemTag.Aoe));
        }
    }
}
