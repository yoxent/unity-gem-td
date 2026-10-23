using GemTD.Gameplay.Gems;
using GemTD.UI;
using NUnit.Framework;
using UnityEngine;

namespace GemTD.Tests.EditMode
{
    public sealed class DraftElementColorsTests
    {
        DraftElementColors _colors;

        [SetUp]
        public void SetUp()
        {
            _colors = ScriptableObject.CreateInstance<DraftElementColors>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_colors);
        }

        [Test]
        public void Resolve_NoElementTags_UsesNonElemental()
        {
            AssertSame(_colors.nonElemental, _colors.Resolve(GemTag.None));
            AssertSame(_colors.nonElemental, _colors.Resolve(GemTag.Physical | GemTag.Attack | GemTag.Projectile));
        }

        [Test]
        public void Resolve_SingleElement_IgnoresOtherTags()
        {
            AssertSame(_colors.fire, _colors.Resolve(GemTag.Fire | GemTag.Spell | GemTag.Projectile));
            AssertSame(_colors.cold, _colors.Resolve(GemTag.Cold));
            AssertSame(_colors.lightning, _colors.Resolve(GemTag.Lightning));
            AssertSame(_colors.chaos, _colors.Resolve(GemTag.Chaos));
        }

        [Test]
        public void Resolve_EachPair_UsesItsOwnRow()
        {
            AssertSame(_colors.fireCold, _colors.Resolve(GemTag.Fire | GemTag.Cold));
            AssertSame(_colors.fireLightning, _colors.Resolve(GemTag.Fire | GemTag.Lightning));
            AssertSame(_colors.fireChaos, _colors.Resolve(GemTag.Fire | GemTag.Chaos));
            AssertSame(_colors.coldLightning, _colors.Resolve(GemTag.Cold | GemTag.Lightning));
            AssertSame(_colors.coldChaos, _colors.Resolve(GemTag.Cold | GemTag.Chaos));
            AssertSame(_colors.lightningChaos, _colors.Resolve(GemTag.Lightning | GemTag.Chaos));
        }

        [Test]
        public void Resolve_ThreeOrFourElements_ShareOneRow()
        {
            AssertSame(
                _colors.threeElements,
                _colors.Resolve(GemTag.Fire | GemTag.Cold | GemTag.Lightning));
            AssertSame(
                _colors.threeElements,
                _colors.Resolve(GemTag.Fire | GemTag.Cold | GemTag.Chaos));
            AssertSame(
                _colors.threeElements,
                _colors.Resolve(GemTag.Fire | GemTag.Lightning | GemTag.Chaos));
            AssertSame(
                _colors.threeElements,
                _colors.Resolve(GemTag.Cold | GemTag.Lightning | GemTag.Chaos));
            AssertSame(
                _colors.threeElements,
                _colors.Resolve(GemTag.Fire | GemTag.Cold | GemTag.Lightning | GemTag.Chaos | GemTag.Aura));
        }

        [Test]
        public void Resolve_TagName_UsesThatElementOrNonElemental()
        {
            AssertSame(_colors.fire, _colors.Resolve(GemTags.FromPoe("Fire")));
            AssertSame(_colors.cold, _colors.Resolve(GemTags.FromPoe("Cold")));
            AssertSame(_colors.lightning, _colors.Resolve(GemTags.FromPoe("Lightning")));
            AssertSame(_colors.chaos, _colors.Resolve(GemTags.FromPoe("Chaos")));
            AssertSame(_colors.nonElemental, _colors.Resolve(GemTags.FromPoe("Bow")));
            AssertSame(_colors.nonElemental, _colors.Resolve(GemTags.FromPoe("Physical")));
        }

        [Test]
        public void ColdAndLightning_UseAuthoredNormalAndDimmed()
        {
            AssertRgb(_colors.cold.normal, 0x4A, 0x9B, 0xF0);
            AssertRgb(_colors.cold.dimmed, 0xDE, 0xED, 0xFC);
            AssertRgb(_colors.lightning.normal, 0xFF, 0xA6, 0x2B);
            AssertRgb(_colors.lightning.dimmed, 0xFF, 0xEF, 0xD9);
        }

        static void AssertSame(DraftElementColors.Palette expected, DraftElementColors.Palette actual)
        {
            Assert.AreEqual(expected.normal, actual.normal);
            Assert.AreEqual(expected.dimmed, actual.dimmed);
        }

        static void AssertRgb(Color color, byte r, byte g, byte b)
        {
            var bytes = (Color32)color;
            Assert.AreEqual(r, bytes.r);
            Assert.AreEqual(g, bytes.g);
            Assert.AreEqual(b, bytes.b);
            Assert.AreEqual(255, bytes.a);
        }
    }
}
