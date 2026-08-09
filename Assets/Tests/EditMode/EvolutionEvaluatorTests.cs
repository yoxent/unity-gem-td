using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class EvolutionEvaluatorTests
    {
        TowerDefinition _ballista;
        GemDefinition _lmp;
        GemDefinition _chain;
        GemDefinition _fork;

        [SetUp]
        public void SetUp()
        {
            _ballista = ScriptableObject.CreateInstance<TowerDefinition>();
            _ballista.DisplayName = "Ballista";
            _ballista.Kind = TowerKind.Projectile;
            _ballista.SocketCount = 3;
            _ballista.Damage = 10f;
            _ballista.Range = 20f;
            _ballista.AttackInterval = 1f;

            _lmp = ScriptableObject.CreateInstance<GemDefinition>();
            _lmp.Id = GemId.Lmp;

            _chain = ScriptableObject.CreateInstance<GemDefinition>();
            _chain.Id = GemId.Chain;

            _fork = ScriptableObject.CreateInstance<GemDefinition>();
            _fork.Id = GemId.Fork;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ballista);
            Object.DestroyImmediate(_lmp);
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
        public void IsHydra_WhenBallistaHasLmpChainFork()
        {
            var tower = MakeBallistaWith(_lmp, _chain, _fork);
            Assert.IsTrue(EvolutionEvaluator.IsHydraBallista(tower));
            Assert.IsTrue(tower.TryUnsocket(2, out _, true));
            Assert.IsFalse(EvolutionEvaluator.IsHydraBallista(tower));
        }

        [Test]
        public void IsHydra_False_WhenNotBallistaDisplayName()
        {
            _ballista.DisplayName = "Cannon";
            var tower = MakeBallistaWith(_lmp, _chain, _fork);
            Assert.IsFalse(EvolutionEvaluator.IsHydraBallista(tower));
        }

        TowerRuntime MakeBallistaWith(params GemDefinition[] gems)
        {
            var tower = new TowerRuntime(new Vector2Int(0, 0), _ballista);
            for (var i = 0; i < gems.Length; i++)
                Assert.IsTrue(tower.TrySocket(gems[i], i, allowSocket: true));
            return tower;
        }
    }
}
