using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;

namespace GemTD.Tests.EditMode
{
    public sealed class SlamEffectVisualTests
    {
        [Test]
        public void ScaleToDiameter_MatchesAoeDiameterOnAllAxes()
        {
            var scale = SlamEffectVisual.ScaleToDiameter(2.8f);

            Assert.AreEqual(5.6f, scale.x, 0.0001f);
            Assert.AreEqual(5.6f, scale.y, 0.0001f);
            Assert.AreEqual(5.6f, scale.z, 0.0001f);
        }

        [Test]
        public void ScaleToDiameter_UsesMinimumRadius()
        {
            var small = SlamEffectVisual.ScaleToDiameter(0.2f);
            var large = SlamEffectVisual.ScaleToDiameter(2.8f);

            Assert.AreEqual(0.8f, small.x, 0.0001f);
            Assert.AreEqual(small.x, small.y, 0.0001f);
            Assert.AreEqual(small.x, small.z, 0.0001f);
            Assert.Greater(large.x, small.x);
        }

        [Test]
        public void SitOnGround_PlacesMeshBottomOnGroundPlane()
        {
            var ground = new Vector3(3f, 0.2f, 4f);
            const float meshExtentsY = 1f;
            const float scaleY = 0.25f;

            var pos = SlamEffectVisual.SitOnGround(ground, meshExtentsY, scaleY);

            Assert.AreEqual(3f, pos.x, 0.0001f);
            Assert.AreEqual(4f, pos.z, 0.0001f);
            Assert.AreEqual(0.2f + 0.25f, pos.y, 0.0001f);
        }
    }
}
