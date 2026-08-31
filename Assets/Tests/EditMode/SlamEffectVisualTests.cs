using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;

namespace GemTD.Tests.EditMode
{
    public sealed class SlamEffectVisualTests
    {
        [Test]
        public void ScaleXz_MatchesDiameter_KeepsAuthoredHeight()
        {
            var authored = new Vector3(1f, 0.25f, 1f);

            var scale = SlamEffectVisual.ScaleXz(authored, 2.8f);

            Assert.AreEqual(5.6f, scale.x, 0.0001f);
            Assert.AreEqual(0.25f, scale.y, 0.0001f);
            Assert.AreEqual(5.6f, scale.z, 0.0001f);
        }

        [Test]
        public void ScaleXz_DoesNotScaleHeightWithRadius()
        {
            var authored = new Vector3(1f, 1f, 1f);

            var small = SlamEffectVisual.ScaleXz(authored, 0.5f);
            var large = SlamEffectVisual.ScaleXz(authored, 2.8f);

            Assert.AreEqual(small.y, large.y, 0.0001f);
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
