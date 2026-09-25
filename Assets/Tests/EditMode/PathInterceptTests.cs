using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Enemies;

namespace GemTD.Tests.EditMode
{
    public sealed class PathInterceptTests
    {
        [Test]
        public void Predict_LeadsAlongPath_NotCurrentFeet()
        {
            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            def.MaxHealth = 20f;
            def.MoveSpeed = 2f;
            try
            {
                var enemy = new EnemyRuntime();
                enemy.Init(
                    def,
                    new List<Vector3>
                    {
                        new Vector3(0f, 0f, 0f),
                        new Vector3(10f, 0f, 0f)
                    });
                var origin = new Vector3(0f, 0f, 2f);
                var predicted = PathIntercept.Predict(origin, projectileSpeed: 4f, enemy);
                Assert.Greater(predicted.x, enemy.WorldPosition.x + 0.5f);
                Assert.AreEqual(0f, predicted.z, 1e-3f);
            }
            finally
            {
                Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void Predict_ZeroSpeedOrNull_UsesCurrentPosition()
        {
            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            def.MaxHealth = 20f;
            def.MoveSpeed = 2f;
            try
            {
                var enemy = new EnemyRuntime();
                enemy.Init(
                    def,
                    new List<Vector3>
                    {
                        new Vector3(1f, 0f, 0f),
                        new Vector3(10f, 0f, 0f)
                    });
                Assert.AreEqual(
                    enemy.WorldPosition,
                    PathIntercept.Predict(Vector3.zero, 0f, enemy));
                Assert.AreEqual(Vector3.zero, PathIntercept.Predict(Vector3.zero, 4f, null));
            }
            finally
            {
                Object.DestroyImmediate(def);
            }
        }
    }
}
