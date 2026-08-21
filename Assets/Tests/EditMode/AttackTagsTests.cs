using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class AttackTagsTests
    {
        TowerDefinition _ballista;
        TowerDefinition _cannon;
        TowerDefinition _beacon;
        GemDefinition _lmp;
        GemDefinition _chain;
        GemDefinition _area;
        GemDefinition _ignite;
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
            _lmp.Id = GemId.Lmp;
            _lmp.Tags = AttackTag.Support | AttackTag.Projectile;

            _chain = ScriptableObject.CreateInstance<GemDefinition>();
            _chain.Id = GemId.Chain;
            _chain.Tags = AttackTag.Support | AttackTag.Chaining | AttackTag.Projectile;

            _area = ScriptableObject.CreateInstance<GemDefinition>();
            _area.Id = GemId.IncreasedArea;
            _area.Tags = AttackTag.Support | AttackTag.Aoe;

            _ignite = ScriptableObject.CreateInstance<GemDefinition>();
            _ignite.Id = GemId.Ignite;
            _ignite.Tags = AttackTag.Support;

            _faster = ScriptableObject.CreateInstance<GemDefinition>();
            _faster.Id = GemId.FasterAttacks;
            _faster.Tags = AttackTag.Attack | AttackTag.Support;
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
            Object.DestroyImmediate(_ignite);
            Object.DestroyImmediate(_faster);
        }

        [Test]
        public void Infer_BallistaAttackProjectile_CannonAddsAoe_BeaconAura()
        {
            Assert.AreEqual(AttackTag.Attack | AttackTag.Projectile, AttackTags.EffectiveTowerTags(_ballista));
            Assert.AreEqual(AttackTag.Attack | AttackTag.Projectile | AttackTag.Aoe, AttackTags.EffectiveTowerTags(_cannon));
            Assert.AreEqual(AttackTag.Aura, AttackTags.EffectiveTowerTags(_beacon));
        }

        [Test]
        public void ProjectileGem_SocketsBallistaAndCannon_NotBeacon()
        {
            Assert.IsTrue(AttackTags.CanSocket(_ballista, _lmp));
            Assert.IsTrue(AttackTags.CanSocket(_cannon, _lmp));
            Assert.IsFalse(AttackTags.CanSocket(_beacon, _lmp));

            var ballista = new TowerRuntime(Vector2Int.zero, _ballista);
            var beacon = new TowerRuntime(Vector2Int.zero, _beacon);
            Assert.IsTrue(ballista.TrySocket(_lmp, 0, allowSocket: true));
            Assert.IsFalse(beacon.TrySocket(_lmp, 0, allowSocket: true));
        }

        [Test]
        public void Chain_RequiresProjectile_NotChaining()
        {
            Assert.AreEqual(AttackTag.Projectile, AttackTags.EffectiveRequiredTags(_chain));
            Assert.IsTrue(AttackTags.CanSocket(_ballista, _chain));
        }

        [Test]
        public void IncreasedArea_SocketsCannon_NotBallista()
        {
            Assert.IsTrue(AttackTags.CanSocket(_cannon, _area));
            Assert.IsFalse(AttackTags.CanSocket(_ballista, _area));
        }

        [Test]
        public void SupportOnlyGem_SocketsAnyTower()
        {
            Assert.IsTrue(AttackTags.CanSocket(_ballista, _ignite));
            Assert.IsTrue(AttackTags.CanSocket(_cannon, _ignite));
            Assert.IsTrue(AttackTags.CanSocket(_beacon, _ignite));
        }

        [Test]
        public void AttackGem_SocketsBallista_NotBeacon()
        {
            Assert.IsTrue(AttackTags.CanSocket(_ballista, _faster));
            Assert.IsFalse(AttackTags.CanSocket(_beacon, _faster));
        }

        [Test]
        public void Format_JoinsActiveTags()
        {
            Assert.AreEqual("—", AttackTags.Format(AttackTag.None));
            Assert.AreEqual("Attack, Projectile, AoE", AttackTags.Format(AttackTag.Attack | AttackTag.Projectile | AttackTag.Aoe));
        }
    }
}
