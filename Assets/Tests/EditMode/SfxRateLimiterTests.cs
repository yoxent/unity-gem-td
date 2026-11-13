using NUnit.Framework;
using GemTD.Core;

namespace GemTD.Tests.EditMode
{
    public sealed class SfxRateLimiterTests
    {
        [Test]
        public void TryPlay_ZeroInterval_AlwaysAllows()
        {
            var limiter = new SfxRateLimiter();
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0f, 0f));
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0f, 0f));
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0f, 0.001f));
        }

        [Test]
        public void TryPlay_NegativeInterval_AlwaysAllows()
        {
            var limiter = new SfxRateLimiter();
            Assert.IsTrue(limiter.TryPlay("HitEnemy", -1f, 5f));
            Assert.IsTrue(limiter.TryPlay("HitEnemy", -1f, 5f));
        }

        [Test]
        public void TryPlay_FirstCallForKey_Allows()
        {
            var limiter = new SfxRateLimiter();
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0.08f, 100f));
        }

        [Test]
        public void TryPlay_WithinInterval_Rejects()
        {
            var limiter = new SfxRateLimiter();
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0.08f, 100f));
            Assert.IsFalse(limiter.TryPlay("TowerShoot", 0.08f, 100.04f));
        }

        [Test]
        public void TryPlay_AtExactInterval_Allows()
        {
            var limiter = new SfxRateLimiter();
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0.08f, 100f));
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0.08f, 100.08f));
        }

        [Test]
        public void TryPlay_AfterInterval_Allows()
        {
            var limiter = new SfxRateLimiter();
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0.08f, 100f));
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0.08f, 100.5f));
        }

        [Test]
        public void TryPlay_RejectedCall_DoesNotConsumeBudget()
        {
            var limiter = new SfxRateLimiter();
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0.08f, 100f));
            // Rejected at 100.04; the window still started at 100.
            Assert.IsFalse(limiter.TryPlay("TowerShoot", 0.08f, 100.04f));
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0.08f, 100.08f));
        }

        [Test]
        public void TryPlay_KeysAreIndependent()
        {
            var limiter = new SfxRateLimiter();
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0.08f, 100f));
            Assert.IsTrue(limiter.TryPlay("HitEnemy", 0.05f, 100f));
            Assert.IsFalse(limiter.TryPlay("TowerShoot", 0.08f, 100.01f));
            Assert.IsFalse(limiter.TryPlay("HitEnemy", 0.05f, 100.01f));
        }

        [Test]
        public void TryPlay_IsCaseInsensitive()
        {
            var limiter = new SfxRateLimiter();
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0.1f, 1f));
            Assert.IsFalse(limiter.TryPlay("towershoot", 0.1f, 1.05f));
        }

        [Test]
        public void TryPlay_EmptyKey_DoesNotThrottle()
        {
            var limiter = new SfxRateLimiter();
            Assert.IsTrue(limiter.TryPlay(null, 0.5f, 1f));
            Assert.IsTrue(limiter.TryPlay(string.Empty, 0.5f, 1f));
        }

        [Test]
        public void TryPlay_ClockMovesBackwards_AllowsRatherThanSilencing()
        {
            var limiter = new SfxRateLimiter();
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0.08f, 10f));
            // A backwards jump must not silence the key until the clock catches up.
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 0.08f, 5f));
        }

        [Test]
        public void Reset_ClearsAllWindows()
        {
            var limiter = new SfxRateLimiter();
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 5f, 100f));
            Assert.IsFalse(limiter.TryPlay("TowerShoot", 5f, 101f));
            limiter.Reset();
            Assert.IsTrue(limiter.TryPlay("TowerShoot", 5f, 101f));
        }

        [Test]
        public void DenseVolley_IsCappedToInterval()
        {
            var limiter = new SfxRateLimiter();
            // 48 shots/second demanded, 12.5/second allowed at 0.08s.
            var allowed = 0;
            for (var i = 0; i < 96; i++)
            {
                var now = i / 48f;
                if (limiter.TryPlay("TowerShoot", 0.08f, now))
                    allowed++;
            }

            Assert.LessOrEqual(allowed, 13);
            Assert.GreaterOrEqual(allowed, 12);
        }
    }
}
