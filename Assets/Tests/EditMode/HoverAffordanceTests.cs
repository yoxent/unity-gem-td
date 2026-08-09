using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GemTD.UI;

namespace GemTD.Tests.EditMode
{
    public sealed class HoverAffordanceTests
    {
        [Test]
        public void Bind_AddsEventTrigger_WhenNone()
        {
            var slotGo = new GameObject("slot", typeof(Image), typeof(Button));
            var xGo = new GameObject("x");
            try
            {
                HoverAffordance.BindXHover(slotGo.GetComponent<Button>(), xGo, () => true);
                var trigger = slotGo.GetComponent<EventTrigger>();
                Assert.IsNotNull(trigger);
                Assert.GreaterOrEqual(trigger.triggers.Count, 2); // enter + exit
            }
            finally
            {
                Object.DestroyImmediate(slotGo);
                Object.DestroyImmediate(xGo);
            }
        }

        [Test]
        public void Bind_ReusesExistingEventTrigger()
        {
            var slotGo = new GameObject("slot", typeof(Image), typeof(Button));
            var xGo = new GameObject("x");
            try
            {
                var existing = slotGo.AddComponent<EventTrigger>();
                HoverAffordance.BindXHover(slotGo.GetComponent<Button>(), xGo, () => true);
                Assert.AreSame(existing, slotGo.GetComponent<EventTrigger>());
                Assert.GreaterOrEqual(slotGo.GetComponent<EventTrigger>().triggers.Count, 2);
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
            var slotGo = new GameObject("slot", typeof(Image), typeof(Button));
            var xGo = new GameObject("x");
            xGo.SetActive(true);
            try
            {
                HoverAffordance.BindXHover(slotGo.GetComponent<Button>(), xGo, () => true);
                Assert.IsFalse(xGo.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(slotGo);
                Object.DestroyImmediate(xGo);
            }
        }
    }
}