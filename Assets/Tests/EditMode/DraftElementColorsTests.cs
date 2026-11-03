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
        public void ForTagLabel_ChipUsesItsRow_ElementKeepsCardChrome()
        {
            AssertSame(_colors.attack, _colors.ForTagLabel(GemTag.Attack));
            AssertSame(_colors.area, _colors.ForTagLabel(GemTag.Aoe));
            AssertSame(_colors.bow, _colors.ForTagLabel(GemTag.Bow));
            AssertSame(_colors.curse, _colors.ForTagLabel(GemTag.Hex));
            AssertSame(_colors.summon, _colors.ForTagLabel(GemTag.Minion));
            AssertSame(_colors.fire, _colors.ForTagLabel(GemTag.Fire));
            AssertSame(_colors.cold, _colors.ForTagLabel(GemTag.Cold));
            AssertSame(_colors.lightning, _colors.ForTagLabel(GemTag.Lightning));
            AssertSame(_colors.chaos, _colors.ForTagLabel(GemTag.Chaos));
        }

        [Test]
        public void ResolveTag_EachChip_UsesItsOwnRow()
        {
            AssertSame(_colors.attack, _colors.ResolveTag(GemTag.Attack));
            AssertSame(_colors.spell, _colors.ResolveTag(GemTag.Spell));
            AssertSame(_colors.melee, _colors.ResolveTag(GemTag.Melee));
            AssertSame(_colors.bow, _colors.ResolveTag(GemTag.Bow));
            AssertSame(_colors.projectile, _colors.ResolveTag(GemTag.Projectile));
            AssertSame(_colors.area, _colors.ResolveTag(GemTag.Aoe));
            AssertSame(_colors.aura, _colors.ResolveTag(GemTag.Aura));
            AssertSame(_colors.curse, _colors.ResolveTag(GemTag.Curse));
            AssertSame(_colors.curse, _colors.ResolveTag(GemTag.Hex));
            AssertSame(_colors.curse, _colors.ResolveTag(GemTag.Mark));
            AssertSame(_colors.duration, _colors.ResolveTag(GemTag.Duration));
            AssertSame(_colors.channeling, _colors.ResolveTag(GemTag.Channeling));
            AssertSame(_colors.summon, _colors.ResolveTag(GemTag.Minion));
            AssertSame(_colors.summon, _colors.ResolveTag(GemTag.Totem));
            AssertSame(_colors.summon, _colors.ResolveTag(GemTag.Brand));
            AssertSame(_colors.summon, _colors.ResolveTag(GemTag.Golem));
            AssertSame(_colors.trap, _colors.ResolveTag(GemTag.Trap));
            AssertSame(_colors.mine, _colors.ResolveTag(GemTag.Mine));
        }

        [Test]
        public void ResolveTag_OtherTags_UseNonElemental()
        {
            AssertSame(_colors.nonElemental, _colors.ResolveTag(GemTag.None));
            AssertSame(_colors.nonElemental, _colors.ResolveTag(GemTag.Fire));
            AssertSame(_colors.nonElemental, _colors.ResolveTag(GemTag.Strike));
            AssertSame(_colors.nonElemental, _colors.ResolveTag(GemTag.Support));
            AssertSame(_colors.nonElemental, _colors.ResolveTag(GemTag.Attack | GemTag.Melee));
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
        public void CardChrome_UsesBrighterPaletteHuesAndALighterBorder()
        {
            AssertChannelLeads(_colors.fire.normal, 0);
            AssertChannelLeads(_colors.cold.normal, 2);
            AssertChannelLeads(_colors.lightning.normal, 0);
            Assert.Greater(Channel(_colors.lightning.normal, 1), Channel(_colors.lightning.normal, 2));
            AssertChannelLeads(_colors.chaos.normal, 1);
            AssertLighterSteps(_colors.fire);
            AssertLighterSteps(_colors.cold);
            AssertLighterSteps(_colors.lightning);
            AssertLighterSteps(_colors.chaos);
            AssertLighterSteps(_colors.fireChaos);
            AssertLighterSteps(_colors.threeElements);
        }

        static void AssertSame(DraftElementColors.Palette expected, DraftElementColors.Palette actual)
        {
            Assert.AreEqual(expected.normal, actual.normal);
            Assert.AreEqual(expected.dimmed, actual.dimmed);
            Assert.AreEqual(expected.border, actual.border);
        }

        static void AssertChannelLeads(Color color, int channel)
        {
            var bytes = (Color32)color;
            var values = new[] { bytes.r, bytes.g, bytes.b };
            for (var i = 0; i < values.Length; i++)
            {
                if (i != channel)
                    Assert.Greater(values[channel], values[i]);
            }
        }

        static void AssertLighterSteps(DraftElementColors.Palette palette)
        {
            Assert.Greater(Sum(palette.border), Sum(palette.normal));
            Assert.Greater(Sum(palette.dimmed), Sum(palette.border));
        }

        static int Channel(Color color, int channel)
        {
            var bytes = (Color32)color;
            if (channel == 0)
                return bytes.r;
            if (channel == 1)
                return bytes.g;
            return bytes.b;
        }

        static int Sum(Color color)
        {
            var bytes = (Color32)color;
            return bytes.r + bytes.g + bytes.b;
        }
    }
}
