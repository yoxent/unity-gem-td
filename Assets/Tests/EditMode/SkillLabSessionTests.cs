using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.SkillLab;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class SkillLabSessionTests
    {
        EnemyDefinition _enemyDef;
        TowerDefinition _fireball;
        TowerDefinition _alternateTower;
        SpellRoleDefinition _fireballRole;
        SpellRoleDefinition _alternateRole;
        GemDefinition[] _catalog;

        [SetUp]
        public void SetUp()
        {
            _enemyDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            _enemyDef.MaxHealth = 100f;

            _fireball = ScriptableObject.CreateInstance<TowerDefinition>();
            _fireballRole = ScriptableObject.CreateInstance<SpellRoleDefinition>();
            _fireballRole.Modifiers = new[]
            {
                Modifier(RoleStat.CastTime, 0.75f),
                Modifier(RoleStat.CastSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.SplashRadius, 1.5f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };
            _fireball.Roles = new TowerRoleDefinition[] { _fireballRole };
            _fireball.Tags = GemTag.Spell | GemTag.Projectile | GemTag.Aoe;
            _fireball.SocketCount = 3;
            _fireball.Damage = 8f;
            _fireball.DisplayName = "Fireball";

            _alternateTower = ScriptableObject.CreateInstance<TowerDefinition>();
            _alternateRole = ScriptableObject.CreateInstance<SpellRoleDefinition>();
            _alternateRole.Modifiers = new[]
            {
                Modifier(RoleStat.CastTime, 0.75f),
                Modifier(RoleStat.CastSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 20f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };
            _alternateTower.Roles = new TowerRoleDefinition[] { _alternateRole };
            _alternateTower.Tags = GemTag.Spell | GemTag.Projectile | GemTag.Aoe;
            _alternateTower.SocketCount = 3;
            _alternateTower.Damage = 8f;
            _alternateTower.DisplayName = "Cleave";

            var ids = new[]
            {
                GemId.MultipleProjectiles, GemId.Chain, GemId.Fork, GemId.IncreasedArea,
                GemId.Pierce, GemId.ElementalProliferation, GemId.Combustion, GemId.AddedFireDamage,
                GemId.AddedColdDamage, GemId.AddedLightningDamage, GemId.Knockback
            };
            _catalog = new GemDefinition[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                _catalog[i] = ScriptableObject.CreateInstance<GemDefinition>();
                _catalog[i].Id = ids[i];
                CatalogGemModifiers.Bind(_catalog[i]);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyDef);
            Object.DestroyImmediate(_fireballRole);
            Object.DestroyImmediate(_alternateRole);
            Object.DestroyImmediate(_fireball);
            Object.DestroyImmediate(_alternateTower);
            for (var i = 0; i < _catalog.Length; i++)
                Object.DestroyImmediate(_catalog[i]);
        }

        [Test]
        public void Fire_OutOfRange_SetsStatus_ClearsSegments()
        {
            _fireballRole.Modifiers = new[]
            {
                Modifier(RoleStat.CastTime, 0.75f),
                Modifier(RoleStat.CastSpeed, 100f),
                Modifier(RoleStat.TowerRadius, 1f),
                Modifier(RoleStat.SplashRadius, 1.5f),
                Modifier(RoleStat.ProjectileCount, 1f)
            };
            var session = MakeSession();
            session.TowerPosition = DummyField.DefaultTowerPosition;
            session.Fire();
            Assert.AreEqual(SkillLabSession.StatusNoTarget, session.Status);
            Assert.IsFalse(session.LastTrace.HasTarget);
        }

        [Test]
        public void Range_UsesSpellRoleModifier()
        {
            var session = MakeSession();
            Assert.AreEqual(20f, session.Range, 0.001f);

            session.Tower.SetLevel(10);
            Assert.AreEqual(20f, session.Range, 0.001f);
        }

        [Test]
        public void SetSocket_AutoClearsOverlay()
        {
            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            Assert.IsTrue(session.LastTrace.HasTarget);
            session.SetSocket(0, GemId.MultipleProjectiles);
            Assert.AreEqual(0, session.LastTrace.Segments.Count);
            Assert.AreEqual(GemId.MultipleProjectiles, session.Tower.Sockets[0].Id);
            Assert.AreEqual(GemRarity.Normal, session.Tower.Sockets[0].Rarity);
        }

        [Test]
        public void SetTowerDef_AutoClearsOverlay()
        {
            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            session.SetTowerDef(_alternateTower);
            Assert.AreEqual(0, session.LastTrace.Segments.Count);
            Assert.AreEqual(_alternateTower, session.Tower.Def);
        }

        [Test]
        public void BindTowers_SortsByDisplayName_SkipsNulls()
        {
            var session = new SkillLabSession();
            session.BindTowers(new[] { _fireball, null, _alternateTower });
            Assert.AreEqual(2, session.Towers.Length);
            Assert.AreSame(_alternateTower, session.Towers[0]);
            Assert.AreSame(_fireball, session.Towers[1]);
        }

        [Test]
        public void SelectTower_LoadsBoundDefinition_ClearsOverlay()
        {
            var session = MakeSession();
            session.BindTowers(new[] { _fireball, _alternateTower });
            session.SelectTower(session.IndexOfDisplayName("Fireball"));
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            Assert.IsTrue(session.LastTrace.HasTarget);

            session.SelectTower(session.IndexOfDisplayName("Cleave"));
            Assert.AreSame(_alternateTower, session.Tower.Def);
            Assert.AreEqual(session.IndexOfDisplayName("Cleave"), session.SelectedTowerIndex);
            Assert.AreEqual(0, session.LastTrace.Segments.Count);
        }

        [Test]
        public void SelectTower_SameTower_KeepsSockets()
        {
            var session = MakeSession();
            session.BindTowers(new[] { _fireball });
            session.SelectTower(0);
            session.SetSocket(0, GemId.MultipleProjectiles);
            session.SelectTower(0);
            Assert.AreEqual(GemId.MultipleProjectiles, session.Tower.Sockets[0].Id);
            Assert.AreEqual(GemRarity.Normal, session.Tower.Sockets[0].Rarity);
        }

        [Test]
        public void SelectTower_OutOfRange_NoOp()
        {
            var session = MakeSession();
            session.BindTowers(new[] { _fireball, _alternateTower });
            session.SelectTower(0);
            session.SelectTower(-1);
            session.SelectTower(99);
            Assert.AreSame(session.Towers[0], session.Tower.Def);
        }

        [Test]
        public void IndexOfDisplayName_FindsFireball()
        {
            var session = new SkillLabSession();
            session.BindTowers(new[] { _alternateTower, _fireball });
            Assert.AreEqual(1, session.IndexOfDisplayName("Fireball"));
            Assert.AreEqual(-1, session.IndexOfDisplayName("Missing"));
        }

        [Test]
        public void ResetPins_DoesNotClearOverlay()
        {
            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            Assert.IsTrue(session.LastTrace.HasTarget);
            session.ResetPins();
            Assert.IsTrue(session.LastTrace.HasTarget);
            Assert.AreEqual(DummyField.HeadPin, session.Dummies.GetDummy(0).WorldPosition);
        }

        [Test]
        public void SetSocket_CanUnsocketRecipeTrio()
        {
            var session = MakeSession();
            session.SetSocket(0, GemId.MultipleProjectiles);
            session.SetSocket(1, GemId.Chain);
            session.SetSocket(2, GemId.Fork);
            Assert.AreEqual(GemRarity.Normal, session.Tower.Sockets[0].Rarity);
            Assert.AreEqual(GemRarity.Normal, session.Tower.Sockets[1].Rarity);
            Assert.AreEqual(GemRarity.Normal, session.Tower.Sockets[2].Rarity);
            Assert.IsFalse(session.IsHydra);
            session.SetSocket(2, GemId.None);
            Assert.IsTrue(session.Tower.Sockets[2].IsEmpty);
            Assert.IsFalse(session.IsHydra);
        }

        [Test]
        public void SetSocket_RejectsDuplicateId()
        {
            var session = MakeSession();
            session.SetSocket(0, GemId.MultipleProjectiles);
            session.SetSocket(1, GemId.MultipleProjectiles);
            Assert.AreEqual(GemId.MultipleProjectiles, session.Tower.Sockets[0].Id);
            Assert.IsTrue(session.Tower.Sockets[1].IsEmpty);
        }

        static RoleStatModifier Modifier(RoleStat stat, float value)
        {
            return RoleStatModifier.Single(stat, RoleModifierOperation.Set, value);
        }

        SkillLabSession MakeSession()
        {
            var session = new SkillLabSession();
            session.BindCatalog(_catalog);
            session.SetTowerDef(_fireball);
            session.Dummies.Init(_enemyDef);
            return session;
        }
    }
}
