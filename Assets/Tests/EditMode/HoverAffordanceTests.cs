using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using GemTD.UI;

namespace GemTD.Tests.EditMode
{
    public sealed class HoverAffordanceTests
    {
        [Test]
        public void Bind_WiresExistingRelays_DoesNotAddComponents()
        {
            var slotGo = new GameObject("slot", typeof(Image), typeof(Button), typeof(HoverPointerRelay));
            var xGo = new GameObject("x", typeof(HoverPointerRelay));
            try
            {
                var slotRelay = slotGo.GetComponent<HoverPointerRelay>();
                var xRelay = xGo.GetComponent<HoverPointerRelay>();
                HoverAffordance.BindXHover(slotRelay, xRelay, xGo, () => true);
                Assert.AreEqual(1, slotGo.GetComponents<HoverPointerRelay>().Length);
                Assert.IsNotNull(slotRelay.OnEnter);
                Assert.IsNotNull(slotRelay.OnExit);
            }
            finally
            {
                Object.DestroyImmediate(slotGo);
                Object.DestroyImmediate(xGo);
            }
        }

        [Test]
        public void Bind_HidesXByDefault()
        {
            var slotGo = new GameObject("slot", typeof(HoverPointerRelay));
            var xGo = new GameObject("x", typeof(HoverPointerRelay));
            xGo.SetActive(true);
            try
            {
                HoverAffordance.BindXHover(
                    slotGo.GetComponent<HoverPointerRelay>(),
                    xGo.GetComponent<HoverPointerRelay>(),
                    xGo,
                    () => true);
                Assert.IsFalse(xGo.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(slotGo);
                Object.DestroyImmediate(xGo);
            }
        }

        [Test]
        public void Bind_NullSlotRelay_DoesNothing()
        {
            var xGo = new GameObject("x");
            xGo.SetActive(true);
            try
            {
                HoverAffordance.BindXHover(null, null, xGo, () => true);
                Assert.IsTrue(xGo.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(xGo);
            }
        }
    }
}
