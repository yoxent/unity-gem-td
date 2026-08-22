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
        TowerDefinition _ballista;
        TowerDefinition _cannon;
        GemDefinition[] _catalog;

        [SetUp]
        public void SetUp()
        {
            _enemyDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            _enemyDef.MaxHealth = 100f;

            _ballista = ScriptableObject.CreateInstance<TowerDefinition>();
            _ballista.Kind = TowerKind.Projectile;
            _ballista.AllowsHydraEvolution = true;
            _ballista.SocketCount = 3;
            _ballista.Damage = 10f;
            _ballista.Range = 20f;

            _cannon = ScriptableObject.CreateInstance<TowerDefinition>();
            _cannon.Kind = TowerKind.Splash;
            _cannon.SocketCount = 3;
            _cannon.Damage = 8f;
            _cannon.Range = 20f;
            _cannon.SplashRadius = 1.5f;

            var ids = new[]
            {
                GemId.Lmp, GemId.Chain, GemId.Fork, GemId.IncreasedArea, GemId.Pierce,
                GemId.Ignite, GemId.Chill, GemId.Shock, GemId.ElementalProliferation
            };
            _catalog = new GemDefinition[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                _catalog[i] = ScriptableObject.CreateInstance<GemDefinition>();
                _catalog[i].Id = ids[i];
            }
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemyDef);
            Object.DestroyImmediate(_ballista);
            Object.DestroyImmediate(_cannon);
            for (var i = 0; i < _catalog.Length; i++)
                Object.DestroyImmediate(_catalog[i]);
        }

        [Test]
        public void Fire_OutOfRange_SetsStatus_ClearsSegments()
        {
            _ballista.Range = 1f;
            var session = MakeSession();
            session.TowerPosition = DummyField.DefaultTowerPosition;
            session.Fire();
            Assert.AreEqual(SkillLabSession.StatusNoTarget, session.Status);
            Assert.IsFalse(session.LastTrace.HasTarget);
        }

        [Test]
        public void SetSocket_AutoClearsOverlay()
        {
            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            Assert.IsTrue(session.LastTrace.HasTarget);
            session.SetSocket(0, GemId.Lmp);
            Assert.AreEqual(0, session.LastTrace.Segments.Count);
            Assert.AreEqual(GemId.Lmp, session.Tower.Sockets[0].Id);
        }

        [Test]
        public void SetTowerDef_AutoClearsOverlay()
        {
            var session = MakeSession();
            session.TowerPosition = Vector3.zero;
            session.Dummies.GetDummy(0).SetWorldPosition(new Vector3(3f, 0f, 0f));
            session.Fire();
            session.SetTowerDef(_cannon);
            Assert.AreEqual(0, session.LastTrace.Segments.Count);
            Assert.AreEqual(_cannon, session.Tower.Def);
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
        public void SetSocket_CanUnsocketHydra()
        {
            var session = MakeSession();
            session.SetSocket(0, GemId.Lmp);
            session.SetSocket(1, GemId.Chain);
            session.SetSocket(2, GemId.Fork);
            Assert.IsTrue(session.IsHydra);
            session.SetSocket(2, GemId.None);
            Assert.IsNull(session.Tower.Sockets[2]);
            Assert.IsFalse(session.IsHydra);
        }

        [Test]
        public void SetSocket_RejectsDuplicateId()
        {
            var session = MakeSession();
            session.SetSocket(0, GemId.Lmp);
            session.SetSocket(1, GemId.Lmp);
            Assert.AreEqual(GemId.Lmp, session.Tower.Sockets[0].Id);
            Assert.IsNull(session.Tower.Sockets[1]);
        }

        SkillLabSession MakeSession()
        {
            var session = new SkillLabSession();
            session.BindCatalog(_catalog);
            session.SetTowerDef(_ballista);
            session.Dummies.Init(_enemyDef);
            return session;
        }
    }
}
