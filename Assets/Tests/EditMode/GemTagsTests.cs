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
            Assert.AreEqual("Fire, Lightning", GemTags.Format(GemTag.Fire | GemTag.Lightning));
        }

        [Test]
        public void ExistingFlagBits_StayStable()
        {
            Assert.AreEqual(1L << 0, (long)GemTag.Projectile);
            Assert.AreEqual(1L << 1, (long)GemTag.Aoe);
            Assert.AreEqual(1L << 2, (long)GemTag.Slam);
            Assert.AreEqual(1L << 3, (long)GemTag.Attack);
            Assert.AreEqual(1L << 4, (long)GemTag.Spell);
            Assert.AreEqual(1L << 5, (long)GemTag.Aura);
            Assert.AreEqual(1L << 6, (long)GemTag.Melee);
            Assert.AreEqual(1L << 7, (long)GemTag.Chaining);
            Assert.AreEqual(1L << 8, (long)GemTag.Support);
            Assert.AreEqual(1L << 9, (long)GemTag.Strike);
        }

        [Test]
        public void HighBits_FitInLongNotInt()
        {
            Assert.AreEqual(1L << 32, (long)GemTag.Nova);
            Assert.AreEqual(1L << 35, (long)GemTag.Physical);
            Assert.AreEqual(1L << 40, (long)GemTag.Trap);
            Assert.AreNotEqual(GemTag.Spell, GemTag.Spell | GemTag.Nova);
            Assert.AreNotEqual(GemTag.None, GemTag.Physical);
        }

        [Test]
        public void FromPoe_MapsCatalogStrings()
        {
            Assert.AreEqual(GemTag.Aoe, GemTags.FromPoe("AoE"));
            Assert.AreEqual(GemTag.Arcane, GemTags.FromPoe("Arcane"));
            Assert.AreEqual(GemTag.Attack, GemTags.FromPoe("Attack"));
            Assert.AreEqual(GemTag.Aura, GemTags.FromPoe("Aura"));
            Assert.AreEqual(GemTag.Blink, GemTags.FromPoe("Blink"));
            Assert.AreEqual(GemTag.Bow, GemTags.FromPoe("Bow"));
            Assert.AreEqual(GemTag.Brand, GemTags.FromPoe("Brand"));
            Assert.AreEqual(GemTag.Chaining, GemTags.FromPoe("Chaining"));
            Assert.AreEqual(GemTag.Channeling, GemTags.FromPoe("Channelling"));
            Assert.AreEqual(GemTag.Channeling, GemTags.FromPoe("Channeling"));
            Assert.AreEqual(GemTag.Chaos, GemTags.FromPoe("Chaos"));
            Assert.AreEqual(GemTag.Cold, GemTags.FromPoe("Cold"));
            Assert.AreEqual(GemTag.Critical, GemTags.FromPoe("Critical"));
            Assert.AreEqual(GemTag.Curse, GemTags.FromPoe("Curse"));
            Assert.AreEqual(GemTag.Duration, GemTags.FromPoe("Duration"));
            Assert.AreEqual(GemTag.Exceptional, GemTags.FromPoe("Exceptional"));
            Assert.AreEqual(GemTag.Fire, GemTags.FromPoe("Fire"));
            Assert.AreEqual(GemTag.Golem, GemTags.FromPoe("Golem"));
            Assert.AreEqual(GemTag.Guard, GemTags.FromPoe("Guard"));
            Assert.AreEqual(GemTag.Herald, GemTags.FromPoe("Herald"));
            Assert.AreEqual(GemTag.Hex, GemTags.FromPoe("Hex"));
            Assert.AreEqual(GemTag.Lightning, GemTags.FromPoe("Lightning"));
            Assert.AreEqual(GemTag.Link, GemTags.FromPoe("Link"));
            Assert.AreEqual(GemTag.Mark, GemTags.FromPoe("Mark"));
            Assert.AreEqual(GemTag.Melee, GemTags.FromPoe("Melee"));
            Assert.AreEqual(GemTag.Mine, GemTags.FromPoe("Mine"));
            Assert.AreEqual(GemTag.Minion, GemTags.FromPoe("Minion"));
            Assert.AreEqual(GemTag.Movement, GemTags.FromPoe("Movement"));
            Assert.AreEqual(GemTag.Nova, GemTags.FromPoe("Nova"));
            Assert.AreEqual(GemTag.Orb, GemTags.FromPoe("Orb"));
            Assert.AreEqual(GemTag.Pact, GemTags.FromPoe("Pact"));
            Assert.AreEqual(GemTag.Physical, GemTags.FromPoe("Physical"));
            Assert.AreEqual(GemTag.Prismatic, GemTags.FromPoe("Prismatic"));
            Assert.AreEqual(GemTag.Projectile, GemTags.FromPoe("Projectile"));
            Assert.AreEqual(GemTag.Retaliation, GemTags.FromPoe("Retaliation"));
            Assert.AreEqual(GemTag.Slam, GemTags.FromPoe("Slam"));
            Assert.AreEqual(GemTag.Spell, GemTags.FromPoe("Spell"));
            Assert.AreEqual(GemTag.Stance, GemTags.FromPoe("Stance"));
            Assert.AreEqual(GemTag.Strike, GemTags.FromPoe("Strike"));
            Assert.AreEqual(GemTag.Support, GemTags.FromPoe("Support"));
            Assert.AreEqual(GemTag.Totem, GemTags.FromPoe("Totem"));
            Assert.AreEqual(GemTag.Trap, GemTags.FromPoe("Trap"));
            Assert.AreEqual(GemTag.Travel, GemTags.FromPoe("Travel"));
            Assert.AreEqual(GemTag.Trigger, GemTags.FromPoe("Trigger"));
            Assert.AreEqual(GemTag.Vaal, GemTags.FromPoe("Vaal"));
            Assert.AreEqual(GemTag.Warcry, GemTags.FromPoe("Warcry"));
            Assert.AreEqual(GemTag.None, GemTags.FromPoe("Bow?"));
            Assert.AreEqual(GemTag.None, GemTags.FromPoe(""));
            Assert.AreEqual(GemTag.None, GemTags.FromPoe(null));
        }

        [Test]
        public void RestrictionMask_ExcludesDamageTypeAndUnusedCatalogTags()
        {
            var unused =
                GemTag.Fire
                | GemTag.Cold
                | GemTag.Lightning
                | GemTag.Physical
                | GemTag.Chaos
                | GemTag.Duration
                | GemTag.Bow
                | GemTag.Curse
                | GemTag.Hex
                | GemTag.Mark
                | GemTag.Nova
                | GemTag.Trap
                | GemTag.Mine
                | GemTag.Support
                | GemTag.Chaining;
            Assert.AreEqual(GemTag.None, unused & GemTags.RestrictionMask);
        }

        [Test]
        public void DamageTypeTags_DoNotGateSockets()
        {
            var physTower = ScriptableObject.CreateInstance<TowerDefinition>();
            physTower.Tags = GemTag.Attack | GemTag.Projectile;
            physTower.SocketCount = 1;
            var fireTower = ScriptableObject.CreateInstance<TowerDefinition>();
            fireTower.Tags = GemTag.Attack | GemTag.Projectile | GemTag.Fire;
            fireTower.SocketCount = 1;
            var addedFire = ScriptableObject.CreateInstance<GemDefinition>();
            addedFire.Tags = GemTag.Support | GemTag.Fire | GemTag.Physical;
            try
            {
                Assert.AreEqual(GemTag.None, GemTags.EffectiveRequiredTags(addedFire));
                Assert.IsTrue(GemTags.CanSocket(physTower, addedFire));
                Assert.IsTrue(GemTags.CanSocket(fireTower, addedFire));
            }
            finally
            {
                Object.DestroyImmediate(physTower);
                Object.DestroyImmediate(fireTower);
                Object.DestroyImmediate(addedFire);
            }
        }
    }
}
