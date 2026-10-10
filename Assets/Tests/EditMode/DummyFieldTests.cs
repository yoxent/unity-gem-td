using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Enemies;
using GemTD.Gameplay.SkillLab;

namespace GemTD.Tests.EditMode
{
    public sealed class DummyFieldTests
    {
        EnemyDefinition _def;

        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<EnemyDefinition>();
            _def.MaxHealth = 100f;
            _def.MoveSpeed = 0.01f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void WriteHomes_BowlingTriangle_MatchesSpecOffsets()
        {
            var homes = new Vector3[DummyField.PinCount];
            DummyField.WriteHomes(homes);

            Assert.AreEqual(DummyField.HeadPin, homes[0]);
            var pitch = DummyField.PinPitch;
            Assert.AreEqual(new Vector3(DummyField.HeadPin.x + pitch, 0f, DummyField.HeadPin.z - pitch * 0.5f), homes[1]);
            Assert.AreEqual(new Vector3(DummyField.HeadPin.x + pitch, 0f, DummyField.HeadPin.z + pitch * 0.5f), homes[2]);
            Assert.AreEqual(new Vector3(DummyField.HeadPin.x + pitch * 2f, 0f, DummyField.HeadPin.z - pitch), homes[3]);
            Assert.AreEqual(new Vector3(DummyField.HeadPin.x + pitch * 2f, 0f, DummyField.HeadPin.z), homes[4]);
            Assert.AreEqual(new Vector3(DummyField.HeadPin.x + pitch * 2f, 0f, DummyField.HeadPin.z + pitch), homes[5]);
            Assert.AreEqual(new Vector3(DummyField.HeadPin.x + pitch * 3f, 0f, DummyField.HeadPin.z - pitch * 1.5f), homes[6]);
            Assert.AreEqual(new Vector3(DummyField.HeadPin.x + pitch * 3f, 0f, DummyField.HeadPin.z - pitch * 0.5f), homes[7]);
            Assert.AreEqual(new Vector3(DummyField.HeadPin.x + pitch * 3f, 0f, DummyField.HeadPin.z + pitch * 0.5f), homes[8]);
            Assert.AreEqual(new Vector3(DummyField.HeadPin.x + pitch * 3f, 0f, DummyField.HeadPin.z + pitch * 1.5f), homes[9]);
        }

        [Test]
        public void ResetPins_RestoresHomes_AfterTeleport()
        {
            var field = new DummyField();
            field.Init(_def);
            field.GetDummy(0).SetWorldPosition(new Vector3(99f, 0f, 99f));
            field.ResetPins();
            Assert.AreEqual(DummyField.HeadPin, field.GetDummy(0).WorldPosition);
            for (var i = 0; i < DummyField.PinCount; i++)
                Assert.IsTrue(field.GetDummy(i).IsAlive);
        }

        [Test]
        public void Init_MarksDummiesInvulnerable()
        {
            var field = new DummyField();
            field.Init(_def);
            for (var i = 0; i < DummyField.PinCount; i++)
            {
                var dummy = field.GetDummy(i);
                Assert.IsTrue(dummy.Invulnerable);
                dummy.ApplyDamage(999f);
                Assert.IsTrue(dummy.IsAlive);
                Assert.AreEqual(100f, dummy.Hp, 1e-4f);
            }
        }

        [Test]
        public void CopyLiving_ReturnsAllTen()
        {
            var field = new DummyField();
            field.Init(_def);
            var living = new List<EnemyRuntime>();
            field.CopyLiving(living);
            Assert.AreEqual(DummyField.PinCount, living.Count);
        }
    }
}
