using NUnit.Framework;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerCatalogLayoutTests
    {
        [Test]
        public void Attack_SlamBeatsProjectile()
        {
            var tags = GemTag.Attack | GemTag.Slam | GemTag.Projectile | GemTag.Aoe | GemTag.Melee;
            Assert.AreEqual("Attack/Slam", TowerCatalogLayout.RelativeFolder("attack", tags));
        }

        [Test]
        public void Attack_StrikeBeatsProjectile()
        {
            var tags = GemTag.Attack | GemTag.Strike | GemTag.Projectile | GemTag.Melee | GemTag.Fire;
            Assert.AreEqual("Attack/Strike", TowerCatalogLayout.RelativeFolder("attack", tags));
        }

        [Test]
        public void Attack_BowWhenNotSlamOrStrike()
        {
            var tags = GemTag.Attack | GemTag.Projectile | GemTag.Bow;
            Assert.AreEqual("Attack/Bow", TowerCatalogLayout.RelativeFolder("attack", tags));
        }

        [Test]
        public void Attack_LeftoverStaysAtRoot()
        {
            Assert.AreEqual(
                "Attack",
                TowerCatalogLayout.RelativeFolder("attack", GemTag.Attack | GemTag.Aoe | GemTag.Melee));
            Assert.AreEqual(
                "Attack",
                TowerCatalogLayout.RelativeFolder(
                    "attack",
                    GemTag.Attack | GemTag.Aoe | GemTag.Melee | GemTag.Channeling));
        }

        [Test]
        public void Spell_ChannelingBeforeProjectileAndAoe()
        {
            var tags = GemTag.Spell | GemTag.Channeling | GemTag.Projectile | GemTag.Aoe | GemTag.Cold | GemTag.Orb;
            Assert.AreEqual("Spell/Channeling", TowerCatalogLayout.RelativeFolder("spell", tags));
        }

        [Test]
        public void Spell_ProjectileBeforeAoe()
        {
            var tags = GemTag.Spell | GemTag.Projectile | GemTag.Aoe | GemTag.Fire;
            Assert.AreEqual("Spell/Projectile", TowerCatalogLayout.RelativeFolder("spell", tags));
        }

        [Test]
        public void Spell_AoeWhenNotChannelingOrProjectile()
        {
            var tags = GemTag.Spell | GemTag.Aoe | GemTag.Fire | GemTag.Duration;
            Assert.AreEqual("Spell/AOE", TowerCatalogLayout.RelativeFolder("spell", tags));
        }

        [Test]
        public void Spell_LeftoverStaysAtRoot()
        {
            Assert.AreEqual(
                "Spell",
                TowerCatalogLayout.RelativeFolder("spell", GemTag.Spell | GemTag.Chaining | GemTag.Lightning));
        }

        [Test]
        public void Herald_LivesWithAura()
        {
            var tags = GemTag.Spell | GemTag.Aoe | GemTag.Fire | GemTag.Herald | GemTag.Duration;
            Assert.AreEqual("Aura", TowerCatalogLayout.RelativeFolder("spell", tags));
        }

        [Test]
        public void CurseAndAura_StayAtCategoryRoot()
        {
            Assert.AreEqual(
                "Curse",
                TowerCatalogLayout.RelativeFolder("curse", GemTag.Spell | GemTag.Curse | GemTag.Aoe));
            Assert.AreEqual(
                "Aura",
                TowerCatalogLayout.RelativeFolder("aura", GemTag.Spell | GemTag.Aura | GemTag.Aoe));
        }
    }
}
