using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerViewPrefabResolverTests
    {
        TowerView _fallback;
        TowerView _slam;
        TowerView _spell;
        TowerView _curse;
        TowerDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _fallback = NewView("Fallback");
            _slam = NewView("Slam");
            _spell = NewView("Spell");
            _curse = NewView("Curse");
            _def = ScriptableObject.CreateInstance<TowerDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_fallback.gameObject);
            Object.DestroyImmediate(_slam.gameObject);
            Object.DestroyImmediate(_spell.gameObject);
            Object.DestroyImmediate(_curse.gameObject);
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void Resolve_SlamFamily_UsesSlamPrefab()
        {
            _def.Tags = GemTag.Attack | GemTag.Melee | GemTag.Slam | GemTag.Aoe;
            Assert.AreSame(_slam, Pick(_def));
        }

        [Test]
        public void Resolve_SpellFamily_UsesSpellPrefab()
        {
            _def.Tags = GemTag.Spell | GemTag.Projectile | GemTag.Aoe | GemTag.Fire;
            Assert.AreSame(_spell, Pick(_def));
        }

        [Test]
        public void Resolve_CurseRole_UsesCursePrefab()
        {
            var curseRole = ScriptableObject.CreateInstance<CurseRoleDefinition>();
            _def.Roles = new TowerRoleDefinition[] { curseRole };
            try
            {
                Assert.AreSame(_curse, Pick(_def));
            }
            finally
            {
                Object.DestroyImmediate(curseRole);
            }
        }

        [Test]
        public void Resolve_MissingFamilyPrefab_FallsBackToDefault()
        {
            _def.Tags = GemTag.Attack | GemTag.Melee | GemTag.Strike;
            Assert.AreSame(_fallback, Pick(_def));
        }

        TowerView Pick(TowerDefinition def)
        {
            return TowerViewPrefabResolver.Resolve(
                def,
                _fallback,
                aura: null,
                curse: _curse,
                slam: _slam,
                strike: null,
                bow: null,
                attack: null,
                spell: _spell);
        }

        static TowerView NewView(string name)
        {
            return new GameObject(name).AddComponent<TowerView>();
        }
    }
}
