using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class HydraSeedAndSocketTests
    {
        [Test]
        public void HydraTower_SocketCountThree_FitsHydraRecipe()
        {
            var def = ScriptableObject.CreateInstance<TowerDefinition>();
            def.DisplayName = "Hydra Test Tower";
            var attack = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            def.Roles = new TowerRoleDefinition[] { attack };
            def.SocketCount = 3;
            def.AllowsHydraEvolution = true;

            var multipleProjectiles = ScriptableObject.CreateInstance<GemDefinition>();
            multipleProjectiles.Id = GemId.MultipleProjectiles;
            var chain = ScriptableObject.CreateInstance<GemDefinition>();
            chain.Id = GemId.Chain;
            var fork = ScriptableObject.CreateInstance<GemDefinition>();
            fork.Id = GemId.Fork;

            try
            {
                var tower = new TowerInstance(Vector2Int.zero, def);
                Assert.AreEqual(3, tower.Sockets.Length);
                Assert.IsTrue(tower.TrySocket(multipleProjectiles, 0, true));
                Assert.IsTrue(tower.TrySocket(chain, 1, true));
                Assert.IsTrue(tower.TrySocket(fork, 2, true));
                Assert.IsFalse(EvolutionEvaluator.IsHydraTower(tower));
            }
            finally
            {
                Object.DestroyImmediate(def);
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(multipleProjectiles);
                Object.DestroyImmediate(chain);
                Object.DestroyImmediate(fork);
            }
        }
    }
}
