using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class EnemyViewPrefabResolverTests
    {
        EnemyView _fallback;
        EnemyView _assigned;
        EnemyDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _fallback = NewView("Fallback");
            _assigned = NewView("Assigned");
            _def = ScriptableObject.CreateInstance<EnemyDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_fallback.gameObject);
            Object.DestroyImmediate(_assigned.gameObject);
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void Resolve_AssignedPrefab_WinsOverFallback()
        {
            _def.ViewPrefab = _assigned;
            Assert.AreSame(_assigned, EnemyViewPrefabResolver.Resolve(_def, _fallback));
        }

        [Test]
        public void Resolve_EmptyPrefab_UsesFallback()
        {
            Assert.AreSame(_fallback, EnemyViewPrefabResolver.Resolve(_def, _fallback));
        }

        [Test]
        public void Resolve_NullDefinition_UsesFallback()
        {
            Assert.AreSame(_fallback, EnemyViewPrefabResolver.Resolve(null, _fallback));
        }

        [Test]
        public void Resolve_NoPrefabAndNoFallback_ReturnsNull()
        {
            Assert.IsNull(EnemyViewPrefabResolver.Resolve(_def, null));
        }

        static EnemyView NewView(string name)
        {
            return new GameObject(name).AddComponent<EnemyView>();
        }
    }
}
