using GemTD.Gameplay.Gems;
using NUnit.Framework;

namespace GemTD.Tests.EditMode
{
    public sealed class GemCategoryTests
    {
        [Test]
        public void FromSource_MapsAllFourAuthoredCategories()
        {
            Assert.AreEqual(GemCategory.Offensive, GemCategories.FromSource("offensive"));
            Assert.AreEqual(GemCategory.ProjectileTargeting, GemCategories.FromSource("projectile_targeting"));
            Assert.AreEqual(GemCategory.UtilityAndControl, GemCategories.FromSource("utility_control"));
            Assert.AreEqual(GemCategory.Transformation, GemCategories.FromSource("transformation"));
        }

        [Test]
        public void FromSource_UnknownOrEmpty_IsNone()
        {
            Assert.AreEqual(GemCategory.None, GemCategories.FromSource(null));
            Assert.AreEqual(GemCategory.None, GemCategories.FromSource(""));
            Assert.AreEqual(GemCategory.None, GemCategories.FromSource("offensive "));
            Assert.AreEqual(GemCategory.None, GemCategories.FromSource("unknown"));
        }

        [Test]
        public void Label_IsPlayerFacing_AndNoneIsEmpty()
        {
            Assert.AreEqual("Offensive", GemCategories.Label(GemCategory.Offensive));
            Assert.AreEqual("Projectile", GemCategories.Label(GemCategory.ProjectileTargeting));
            Assert.AreEqual("Utility", GemCategories.Label(GemCategory.UtilityAndControl));
            Assert.AreEqual("Transformation", GemCategories.Label(GemCategory.Transformation));
            Assert.AreEqual("", GemCategories.Label(GemCategory.None));
        }

        [Test]
        public void EveryCategory_HasALabel()
        {
            var values = (GemCategory[])System.Enum.GetValues(typeof(GemCategory));
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] == GemCategory.None)
                    continue;
                Assert.AreNotEqual("", GemCategories.Label(values[i]), values[i] + " needs a label.");
            }
        }
    }
}
