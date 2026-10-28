using System.IO;
using NUnit.Framework;
using UnityEngine;
using GemTD.Core;
using GemTD.Gameplay.CameraControl;

namespace GemTD.Tests.EditMode
{
    public sealed class CameraShakeTests
    {
        string _tempPath;

        [SetUp]
        public void SetUp()
        {
            CameraShake.Suppressed = false;
            GameEvents.ClearAll();
            PlayerProfile.ResetForTests();
            _tempPath = Path.Combine(Path.GetTempPath(), "gemtd-shake-test-" + Path.GetRandomFileName() + ".json");
            PlayerProfile.Initialize(new JsonFileGemTdSaveStore(_tempPath));
        }

        [TearDown]
        public void TearDown()
        {
            CameraShake.Suppressed = false;
            GameEvents.ClearAll();
            PlayerProfile.ResetForTests();
            if (!string.IsNullOrEmpty(_tempPath) && File.Exists(_tempPath))
                File.Delete(_tempPath);
        }

        [Test]
        public void Shake_Default_UsesConfiguredIntensity()
        {
            var shake = new CameraShake();
            shake.Shake();
            Assert.AreEqual(CameraShakeRequest.DefaultIntensity, shake.CurrentAmplitude, 1e-4f);
        }

        [Test]
        public void Shake_NegativeIntensity_UsesDefault()
        {
            var shake = new CameraShake();
            shake.Shake(-1f);
            Assert.AreEqual(CameraShakeRequest.DefaultIntensity, shake.CurrentAmplitude, 1e-4f);
        }

        [Test]
        public void Shake_ClampsToMax()
        {
            var shake = new CameraShake();
            shake.Shake(2f);
            Assert.AreEqual(CameraShakeRequest.MaxIntensity, shake.CurrentAmplitude, 1e-4f);
        }

        [Test]
        public void Shake_Zero_Stops()
        {
            var shake = new CameraShake();
            shake.Shake(0.04f);
            shake.Shake(0f);
            Assert.IsFalse(shake.IsShaking);
            Assert.AreEqual(Vector3.zero, shake.CurrentOffset);
        }

        [Test]
        public void Shake_WeakerThanRemaining_DoesNotReplace()
        {
            var shake = new CameraShake();
            shake.Shake(0.06f);
            shake.Shake(0.02f);
            Assert.AreEqual(0.06f, shake.CurrentAmplitude, 1e-4f);
        }

        [Test]
        public void Shake_Stronger_ReplacesAmplitude()
        {
            var shake = new CameraShake();
            shake.Shake(0.02f);
            shake.Shake(0.05f);
            Assert.AreEqual(0.05f, shake.CurrentAmplitude, 1e-4f);
        }

        [Test]
        public void Tick_PastDuration_Stops()
        {
            var shake = new CameraShake(defaultDuration: 0.1f);
            shake.Shake(0.04f);
            shake.Tick(0.1f, Quaternion.identity, Vector3.zero);
            Assert.IsFalse(shake.IsShaking);
            Assert.AreEqual(Vector3.zero, shake.CurrentOffset);
        }

        [Test]
        public void Tick_OffsetStaysOnCameraPlane()
        {
            var shake = new CameraShake();
            shake.Shake(0.04f);
            var offset = shake.Tick(0f, Quaternion.identity, Vector3.zero);
            Assert.AreEqual(0f, offset.z, 1e-4f);
            Assert.LessOrEqual(offset.magnitude, CameraShakeRequest.MaxIntensity + 1e-4f);
        }

        [Test]
        public void ShakeAt_BeyondFalloff_HasNoOffset()
        {
            var shake = new CameraShake(falloffDistance: 10f);
            shake.ShakeAt(new Vector3(40f, 0f, 0f), 0.08f);
            var far = shake.Tick(0f, Quaternion.identity, Vector3.zero);
            Assert.AreEqual(Vector3.zero, far);
            Assert.IsTrue(shake.IsShaking);
        }

        [Test]
        public void Suppressed_IgnoresNewShake()
        {
            CameraShake.Suppressed = true;
            var shake = new CameraShake();
            shake.Shake(0.04f);
            Assert.IsFalse(shake.IsShaking);
        }

        [Test]
        public void RequestFire_RaisesFireIntensity()
        {
            CameraShakeRequest received = default;
            GameEvents.CameraShake += r => received = r;
            CameraShake.RequestFire();
            Assert.AreEqual(CameraShakeRequest.FireIntensity, received.Intensity, 1e-4f);
            Assert.AreEqual(CameraShakeKind.Burst, received.Kind);
        }

        [Test]
        public void RequestHit_RaisesHitIntensity()
        {
            CameraShakeRequest received = default;
            GameEvents.CameraShake += r => received = r;
            CameraShake.RequestHit();
            Assert.AreEqual(CameraShakeRequest.HitIntensity, received.Intensity, 1e-4f);
            Assert.AreEqual(CameraShakeKind.Burst, received.Kind);
        }

        [Test]
        public void Burst_DecaysAndStops()
        {
            var shake = new CameraShake(defaultDuration: 0.1f);
            shake.ShakeBurst(0.04f);
            shake.Tick(0.05f, Quaternion.identity, Vector3.zero);
            Assert.IsTrue(shake.IsShaking);
            Assert.Less(shake.CurrentAmplitude, 0.04f);
            shake.Tick(0.05f, Quaternion.identity, Vector3.zero);
            Assert.IsFalse(shake.IsShaking);
        }

        [Test]
        public void Duration_HoldsThenStops()
        {
            var shake = new CameraShake();
            shake.ShakeFor(0.4f, 0.04f);
            shake.Tick(0.2f, Quaternion.identity, Vector3.zero);
            Assert.AreEqual(0.04f, shake.CurrentAmplitude, 1e-4f);
            shake.Tick(0.2f, Quaternion.identity, Vector3.zero);
            Assert.IsFalse(shake.IsShaking);
        }

        [Test]
        public void Continuous_HoldsUntilStopped()
        {
            var shake = new CameraShake();
            shake.ShakeContinuous(0.03f);
            shake.Tick(2f, Quaternion.identity, Vector3.zero);
            Assert.IsTrue(shake.IsContinuous);
            Assert.AreEqual(0.03f, shake.CurrentAmplitude, 1e-4f);
            shake.StopContinuous();
            Assert.IsFalse(shake.IsContinuous);
            shake.Tick(CameraShakeRequest.DefaultDuration, Quaternion.identity, Vector3.zero);
            Assert.IsFalse(shake.IsShaking);
        }

        [Test]
        public void Burst_DoesNotCancelContinuous()
        {
            var shake = new CameraShake(defaultDuration: 0.1f);
            shake.ShakeContinuous(0.03f);
            shake.ShakeBurst(0.05f);
            shake.Tick(0.1f, Quaternion.identity, Vector3.zero);
            Assert.IsTrue(shake.IsContinuous);
            Assert.AreEqual(0.03f, shake.CurrentAmplitude, 1e-4f);
        }

        [Test]
        public void RequestBurst_RaisesBurstKind()
        {
            CameraShakeRequest received = default;
            GameEvents.CameraShake += r => received = r;
            CameraShake.RequestBurst(0.04f);
            Assert.AreEqual(CameraShakeKind.Burst, received.Kind);
            Assert.AreEqual(0.04f, received.Intensity, 1e-4f);
        }

        [Test]
        public void RequestFor_RaisesDurationKind()
        {
            CameraShakeRequest received = default;
            GameEvents.CameraShake += r => received = r;
            CameraShake.RequestFor(0.5f, 0.04f);
            Assert.AreEqual(CameraShakeKind.Duration, received.Kind);
            Assert.AreEqual(0.5f, received.Duration, 1e-4f);
        }

        [Test]
        public void RequestContinuous_RaisesContinuousKind()
        {
            CameraShakeRequest received = default;
            GameEvents.CameraShake += r => received = r;
            CameraShake.RequestContinuous(0.03f);
            Assert.AreEqual(CameraShakeKind.Continuous, received.Kind);
        }

        [Test]
        public void SettingsDisabled_IgnoresNewShake()
        {
            GameSettings.SetCameraShakeEnabled(false);
            var shake = new CameraShake();
            shake.Shake(0.04f);
            Assert.IsFalse(shake.IsShaking);
        }

        [Test]
        public void SettingsDisabled_StopsActiveShakeOnTick()
        {
            var shake = new CameraShake();
            shake.Shake(0.04f);
            Assert.IsTrue(shake.IsShaking);
            GameSettings.SetCameraShakeEnabled(false);
            shake.Tick(0f, Quaternion.identity, Vector3.zero);
            Assert.IsFalse(shake.IsShaking);
        }
    }
}
