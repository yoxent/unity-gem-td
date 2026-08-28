using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class GemTagsTests
    {
        TowerDefinition _projectileTower;
        TowerDefinition _splashTower;
        TowerDefinition _auraTower;
        AttackRoleDefinition _projectileRole;
        AttackRoleDefinition _splashRole;
        AuraRoleDefinition _auraRole;
        GemDefinition _multipleProjectiles;
        GemDefinition _chain;
        GemDefinition _area;
        GemDefinition _supportOnly;
        GemDefinition _faster;

        [SetUp]
        public void SetUp()
        {
            _projectileTower = ScriptableObject.CreateInstance<TowerDefinition>();
            _projectileRole = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            _projectileTower.Roles = new TowerRoleDefinition[] { _projectileRole };
            _projectileTower.SocketCount = 3;

            _splashTower = ScriptableObject.CreateInstance<TowerDefinition>();
            _splashRole = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            _splashTower.Roles = new TowerRoleDefinition[] { _splashRole };
            _splashTower.Tags = GemTag.Attack | GemTag.Projectile | GemTag.Aoe;
            _splashTower.SocketCount = 3;

            _auraTower = ScriptableObject.CreateInstance<TowerDefinition>();
            _auraRole = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            _auraTower.Roles = new TowerRoleDefinition[] { _auraRole };
            _auraTower.SocketCount = 1;

            _multipleProjectiles = ScriptableObject.CreateInstance<GemDefinition>();
            _multipleProjectiles.Id = GemId.MultipleProjectiles;
            _multipleProjectiles.Tags = GemTag.Support | GemTag.Projectile;

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
            Object.DestroyImmediate(_projectileTower);
            Object.DestroyImmediate(_splashTower);
            Object.DestroyImmediate(_auraTower);
            Object.DestroyImmediate(_projectileRole);
            Object.DestroyImmediate(_splashRole);
            Object.DestroyImmediate(_auraRole);
            Object.DestroyImmediate(_multipleProjectiles);
            Object.DestroyImmediate(_chain);
            Object.DestroyImmediate(_area);
            Object.DestroyImmediate(_supportOnly);
            Object.DestroyImmediate(_faster);
        }

        [Test]
        public void Infer_ProjectileTower_SplashTower_AuraRole()
        {
            Assert.AreEqual(GemTag.Attack | GemTag.Projectile, GemTags.EffectiveTowerTags(_projectileTower));
            Assert.AreEqual(GemTag.Attack | GemTag.Projectile | GemTag.Aoe, GemTags.EffectiveTowerTags(_splashTower));
            Assert.AreEqual(GemTag.Aura, GemTags.EffectiveTowerTags(_auraTower));
        }

        [Test]
        public void ProjectileGem_SocketsProjectileAndSplashTowers_NotAuraTower()
        {
            Assert.IsTrue(GemTags.CanSocket(_projectileTower, _multipleProjectiles));
            Assert.IsTrue(GemTags.CanSocket(_splashTower, _multipleProjectiles));
            Assert.IsFalse(GemTags.CanSocket(_auraTower, _multipleProjectiles));

            var projectileTower = new TowerInstance(Vector2Int.zero, _projectileTower);
            var auraTower = new TowerInstance(Vector2Int.zero, _auraTower);
            Assert.IsTrue(projectileTower.TrySocket(_multipleProjectiles, 0, allowSocket: true));
            Assert.IsFalse(auraTower.TrySocket(_multipleProjectiles, 0, allowSocket: true));
        }

        [Test]
        public void ProjectileGem_RarityDoesNotChangeSocketEligibility()
        {
            var lesser = new GemInstance(_multipleProjectiles, GemRarity.Lesser);
            var greater = new GemInstance(_multipleProjectiles, GemRarity.Greater);

            Assert.IsTrue(GemTags.CanSocket(_projectileTower, lesser));
            Assert.IsTrue(GemTags.CanSocket(_projectileTower, greater));
            Assert.IsFalse(GemTags.CanSocket(_auraTower, lesser));
            Assert.IsFalse(GemTags.CanSocket(_auraTower, greater));
        }

        [Test]
        public void Chain_RequiresProjectile_NotChaining()
        {
            Assert.AreEqual(GemTag.Projectile, GemTags.EffectiveRequiredTags(_chain));
            Assert.IsTrue(GemTags.CanSocket(_projectileTower, _chain));
        }

        [Test]
        public void IncreasedArea_SocketsSplashTower_NotProjectileTower()
        {
            Assert.IsTrue(GemTags.CanSocket(_splashTower, _area));
            Assert.IsFalse(GemTags.CanSocket(_projectileTower, _area));
        }

        [Test]
        public void SupportOnlyGem_SocketsAnyTower()
        {
            Assert.IsTrue(GemTags.CanSocket(_projectileTower, _supportOnly));
            Assert.IsTrue(GemTags.CanSocket(_splashTower, _supportOnly));
            Assert.IsTrue(GemTags.CanSocket(_auraTower, _supportOnly));
        }

        [Test]
        public void AttackGem_SocketsProjectileTower_NotAuraTower()
        {
            Assert.IsTrue(GemTags.CanSocket(_projectileTower, _faster));
            Assert.IsFalse(GemTags.CanSocket(_auraTower, _faster));
        }

        [Test]
        public void StrikeGem_RequiresStrikeOnTower()
        {
            var strikeTower = ScriptableObject.CreateInstance<TowerDefinition>();
            strikeTower.Tags = GemTag.Attack | GemTag.Melee | GemTag.Strike;
            strikeTower.SocketCount = 1;
            var meleeOnly = ScriptableObject.CreateInstance<TowerDefinition>();
            meleeOnly.Tags = GemTag.Attack | GemTag.Melee;
            meleeOnly.SocketCount = 1;
            var behead = ScriptableObject.CreateInstance<GemDefinition>();
            behead.Tags = GemTag.Support | GemTag.Strike | GemTag.Melee | GemTag.Attack;
            try
            {
                Assert.IsTrue(GemTags.CanSocket(strikeTower, behead));
                Assert.IsFalse(GemTags.CanSocket(meleeOnly, behead));
            }
            finally
            {
                Object.DestroyImmediate(strikeTower);
                Object.DestroyImmediate(meleeOnly);
                Object.DestroyImmediate(behead);
            }
        }

        [Test]
        public void Format_JoinsActiveTags()
        {
            Assert.AreEqual("—", GemTags.Format(GemTag.None));
            Assert.AreEqual("Attack, Projectile, AoE", GemTags.Format(GemTag.Attack | GemTag.Projectile | GemTag.Aoe));
        }
    }
}
