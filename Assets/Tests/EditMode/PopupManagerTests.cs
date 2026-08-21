using NUnit.Framework;
using UnityEngine;
using GemTD.Core;
using GemTD.UI;

namespace GemTD.Tests.EditMode
{
    public sealed class PopupManagerTests
    {
        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteAll();

        [Test]
        public void DontShowKey_Convention()
        {
            Assert.AreEqual("GemTD.Popup.Sell.DontShow", PopupManager.DontShowKey("Sell"));
        }

        [Test]
        public void ShouldShow_UnknownKey_True()
        {
            Assert.IsTrue(PopupManager.ShouldShow("Discard"));
        }

        [Test]
        public void ShouldShow_Suppressed_False()
        {
            PlayerPrefs.SetInt(PopupManager.DontShowKey("Discard"), 1);
            Assert.IsFalse(PopupManager.ShouldShow("Discard"));
        }

        [Test]
        public void Suppress_WritesPlayerPrefs()
        {
            PopupManager.Suppress("Discard");
            Assert.AreEqual(1, PlayerPrefs.GetInt(PopupManager.DontShowKey("Discard"), 0));
        }

        [Test]
        public void DefaultButtonLabels_ConfirmCancel()
        {
            Assert.AreEqual("Confirm", PopupManager.DefaultYesText);
            Assert.AreEqual("Cancel", PopupManager.DefaultNoText);
        }
    }
}