using NUnit.Framework;
using UnityEngine;
using GemTD.UI;

namespace GemTD.Tests.EditMode
{
    public sealed class TooltipControllerTests
    {
        [Test]
        public void Reposition_NoClampNeeded_ReturnsAnchor()
        {
            var pos = TooltipController.RepositionWithinScreen(
                new Vector2(100f, 500f), new Vector2(200f, 100f), new Vector2(1920f, 1080f), 12f);
            Assert.AreEqual(new Vector2(100f, 500f), pos);
        }

        [Test]
        public void Reposition_RightOverflow_ClampsRight()
        {
            var pos = TooltipController.RepositionWithinScreen(
                new Vector2(1850f, 500f), new Vector2(200f, 100f), new Vector2(1920f, 1080f), 12f);
            Assert.AreEqual(1708f, pos.x, 0.01f);
        }

        [Test]
        public void Reposition_LeftOverflow_ClampsLeft()
        {
            var pos = TooltipController.RepositionWithinScreen(
                new Vector2(-50f, 500f), new Vector2(200f, 100f), new Vector2(1920f, 1080f), 12f);
            Assert.AreEqual(12f, pos.x, 0.01f);
        }

        [Test]
        public void Reposition_TopOverflow_ClampsTop()
        {
            var pos = TooltipController.RepositionWithinScreen(
                new Vector2(100f, 1070f), new Vector2(200f, 100f), new Vector2(1920f, 1080f), 12f);
            Assert.AreEqual(968f, pos.y, 0.01f); // 1080 - 100 - 12
        }

        [Test]
        public void Reposition_BottomOverflow_ClampsBottom()
        {
            var pos = TooltipController.RepositionWithinScreen(
                new Vector2(100f, -20f), new Vector2(200f, 100f), new Vector2(1920f, 1080f), 12f);
            Assert.AreEqual(12f, pos.y, 0.01f);
        }
    }
}