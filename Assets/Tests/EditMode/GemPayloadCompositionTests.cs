using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class GemPayloadCompositionTests
    {
        TowerDefinition _towerDefinition;
        AttackRoleDefinition _role;
        GemDefinition _gem;

        [SetUp]
        public void SetUp()
        {
            _towerDefinition = ScriptableObject.CreateInstance<TowerDefinition>();
            _towerDefinition.SocketCount = 2;
            _role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            _towerDefinition.Roles = new TowerRoleDefinition[] { _role };
            _gem = ScriptableObject.CreateInstance<GemDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_towerDefinition);
            Object.DestroyImmediate(_role);
            Object.DestroyImmediate(_gem);
        }

        [Test]
        public void CollectEffectPayloads_ReturnsRoleThenSocketedGemPayloads()
        {
            var rolePayload = ValidPayload(0.25f);
            var firstGemPayload = ValidPayload(0.5f);
            var secondGemPayload = ValidPayload(0.75f);
            _role.EffectPayloads = new[] { rolePayload };
            _gem.EffectPayloads = new[] { firstGemPayload };
            var secondGem = ScriptableObject.CreateInstance<GemDefinition>();
            secondGem.EffectPayloads = new[] { secondGemPayload };
            var tower = new TowerInstance(Vector2Int.zero, _towerDefinition);
            Assert.IsTrue(tower.TrySocket(
                new GemInstance(_gem, GemRarity.Greater),
                0,
                allowSocket: true));
            Assert.IsTrue(tower.TrySocket(
                new GemInstance(secondGem, GemRarity.Lesser),
                1,
                allowSocket: true));

            try
            {
                var definitions = new List<EffectPayloadDefinition>();
                GemModifierPipeline.CollectEffectPayloads(tower, definitions);

                Assert.AreEqual(3, definitions.Count);
                Assert.AreSame(rolePayload, definitions[0]);
                Assert.AreSame(firstGemPayload, definitions[1]);
                Assert.AreSame(secondGemPayload, definitions[2]);
            }
            finally
            {
                Object.DestroyImmediate(secondGem);
            }
        }

        [Test]
        public void CollectEffectPayloads_EmptyGemPayloadsDoNotChangeRoleList()
        {
            _role.EffectPayloads = new[] { ValidPayload(0.25f) };
            var tower = new TowerInstance(Vector2Int.zero, _towerDefinition);
            Assert.IsTrue(tower.TrySocket(
                new GemInstance(_gem, GemRarity.Lesser),
                0,
                allowSocket: true));

            var definitions = new List<EffectPayloadDefinition>();
            GemModifierPipeline.CollectEffectPayloads(tower, definitions);

            Assert.AreEqual(1, definitions.Count);
            Assert.AreSame(_role.EffectPayloads[0], definitions[0]);
        }

        [Test]
        public void CollectEffectPayloads_ClearsCallerListAndSkipsNullPayloads()
        {
            var rolePayload = ValidPayload(0.25f);
            var gemPayload = ValidPayload(0.5f);
            _role.EffectPayloads = new[] { null, rolePayload };
            _gem.EffectPayloads = new[] { null, gemPayload };
            var tower = new TowerInstance(Vector2Int.zero, _towerDefinition);
            Assert.IsTrue(tower.TrySocket(
                new GemInstance(_gem, GemRarity.Normal),
                0,
                allowSocket: true));
            var stalePayload = ValidPayload(1f);
            var definitions = new List<EffectPayloadDefinition> { stalePayload };

            GemModifierPipeline.CollectEffectPayloads(tower, definitions);

            Assert.AreEqual(2, definitions.Count);
            Assert.AreSame(rolePayload, definitions[0]);
            Assert.AreSame(gemPayload, definitions[1]);
        }

        [Test]
        public void CollectEffectPayloads_NullTowerClearsCallerList()
        {
            var definitions = new List<EffectPayloadDefinition> { ValidPayload(1f) };

            GemModifierPipeline.CollectEffectPayloads(null, definitions);

            Assert.AreEqual(0, definitions.Count);
        }

        [Test]
        public void CollectEffectPayloads_NullCallerListThrows()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => GemModifierPipeline.CollectEffectPayloads(null, null));
        }

        static EffectPayloadDefinition ValidPayload(float damageMultiplier)
        {
            return new EffectPayloadDefinition
            {
                Count = 1,
                DamageMultiplier = damageMultiplier,
                AoeRadius = 1f,
                MinDistance = 1f,
                MaxDistance = 4f
            };
        }
    }
}
