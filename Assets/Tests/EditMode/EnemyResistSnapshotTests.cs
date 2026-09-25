using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class EnemyResistSnapshotTests
    {
        [Test]
        public void Init_SnapshotsResists()
        {
            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            def.MaxHealth = 10f;
            def.FireResistance = 20;
            def.ChaosResistance = -10;
            var enemy = new EnemyRuntime();
            enemy.Init(def, new List<Vector3> { Vector3.zero, Vector3.right });
            Assert.AreEqual(20, enemy.FireResistance);
            Assert.AreEqual(0, enemy.ColdResistance);
            Assert.AreEqual(0, enemy.LightningResistance);
            Assert.AreEqual(-10, enemy.ChaosResistance);
            Object.DestroyImmediate(def);
        }
    }
}
