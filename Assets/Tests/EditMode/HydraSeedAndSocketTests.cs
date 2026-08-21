using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class HydraSeedAndSocketTests
    {
        [Test]
        public void BallistaDefinition_SocketCountThree_FitsHydraRecipe()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.DisplayName = "Ballista";
            def.Kind = TowerKind.Projectile;
            def.SocketCount = 3;
            def.AllowsHydraEvolution = true;

            var lmp = ScriptableObject.CreateInstance<GemDefinition>();
            lmp.Id = GemId.Lmp;
            var chain = ScriptableObject.CreateInstance<GemDefinition>();
            chain.Id = GemId.Chain;
            var fork = ScriptableObject.CreateInstance<GemDefinition>();
            fork.Id = GemId.Fork;

            try
            {
                var tower = new TowerRuntime(Vector2Int.zero, def);
                Assert.AreEqual(3, tower.Sockets.Length);
                Assert.IsTrue(tower.TrySocket(lmp, 0, true));
                Assert.IsTrue(tower.TrySocket(chain, 1, true));
                Assert.IsTrue(tower.TrySocket(fork, 2, true));
                Assert.IsTrue(EvolutionEvaluator.IsHydraBallista(tower));
            }
            finally
            {
                Object.DestroyImmediate(def);
                Object.DestroyImmediate(lmp);
                Object.DestroyImmediate(chain);
                Object.DestroyImmediate(fork);
            }
        }
    }
}
