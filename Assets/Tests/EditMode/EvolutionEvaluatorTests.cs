using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class EvolutionEvaluatorTests
    {
        TowerDefinition _hydraTower;
        AttackRoleDefinition _attackRole;
        GemDefinition _multipleProjectiles;
        GemDefinition _chain;
        GemDefinition _fork;

        [SetUp]
        public void SetUp()
        {
            _hydraTower = ScriptableObject.CreateInstance<TowerDefinition>();
            _hydraTower.DisplayName = "Hydra Test Tower";
            _attackRole = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            _hydraTower.Roles = new TowerRoleDefinition[] { _attackRole };
            _hydraTower.AllowsHydraEvolution = true;
            _hydraTower.SocketCount = 3;
            _hydraTower.Damage = 10f;

            _multipleProjectiles = ScriptableObject.CreateInstance<GemDefinition>();
            _multipleProjectiles.Id = GemId.MultipleProjectiles;

            _chain = ScriptableObject.CreateInstance<GemDefinition>();
            _chain.Id = GemId.Chain;

            _fork = ScriptableObject.CreateInstance<GemDefinition>();
            _fork.Id = GemId.Fork;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_hydraTower);
            Object.DestroyImmediate(_attackRole);
            Object.DestroyImmediate(_multipleProjectiles);
            Object.DestroyImmediate(_chain);
            Object.DestroyImmediate(_fork);
        }

        [Test]
        public void HydraHeadYawOffsets_AreMinus25ZeroPlus25()
        {
            var offsets = EvolutionEvaluator.HydraHeadYawOffsets;
            Assert.AreEqual(3, offsets.Length);
            Assert.AreEqual(-25f, offsets[0], 1e-4f);
            Assert.AreEqual(0f, offsets[1], 1e-4f);
            Assert.AreEqual(25f, offsets[2], 1e-4f);
        }

        [Test]
        public void IsHydra_AlwaysFalse_ThisPass()
        {
            Assert.IsFalse(EvolutionEvaluator.HydraEnabled);
            var tower = MakeHydraTowerWith(_multipleProjectiles, _chain, _fork);
            Assert.IsFalse(EvolutionEvaluator.IsHydraTower(tower));
            Assert.IsTrue(tower.TryUnsocket(2, out _, true));
        }

        [Test]
        public void IsHydra_False_WhenNotHydraEligible()
        {
            _hydraTower.AllowsHydraEvolution = false;
            var tower = MakeHydraTowerWith(_multipleProjectiles, _chain, _fork);
            Assert.IsFalse(EvolutionEvaluator.IsHydraTower(tower));
        }

        TowerInstance MakeHydraTowerWith(params GemDefinition[] gems)
        {
            var tower = new TowerInstance(new Vector2Int(0, 0), _hydraTower);
            for (var i = 0; i < gems.Length; i++)
                Assert.IsTrue(tower.TrySocket(gems[i], i, allowSocket: true));
            return tower;
        }
    }
}
