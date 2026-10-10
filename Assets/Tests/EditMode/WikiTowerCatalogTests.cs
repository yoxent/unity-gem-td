using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class WikiTowerCatalogTests
    {
        TowerDefinition _tower;
        AttackRoleDefinition _role;

        [SetUp]
        public void SetUp()
        {
            _tower = ScriptableObject.CreateInstance<TowerDefinition>();
            _role = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            _role.AimMode = AimMode.Direct;
            _role.DeliveryPattern = DeliveryPattern.WarpStrike;
            _role.Mix = new[]
            {
                new DamageTypeShare { Type = DamageType.Physical, Percent = 100 }
            };
            _role.Modifiers = new[]
            {
                RoleStatModifier.Single(RoleStat.AttackTime, RoleModifierOperation.Set, 1f),
                RoleStatModifier.Single(RoleStat.AttackSpeed, RoleModifierOperation.Set, 80f),
                RoleStatModifier.Single(RoleStat.TowerRadius, RoleModifierOperation.Set, 3.5f),
                RoleStatModifier.Single(RoleStat.SplashRadius, RoleModifierOperation.Set, 2.4f),
                RoleStatModifier.Single(RoleStat.Damage, RoleModifierOperation.Set, 10f)
            };
            _role.Levels = new[]
            {
                new RoleLevelDefinition { SourceLevel = 1, Modifiers = new[] { RoleStatModifier.Single(RoleStat.Damage, RoleModifierOperation.Multiply, 2.1f) } },
                new RoleLevelDefinition { SourceLevel = 10, Modifiers = new[] { RoleStatModifier.Single(RoleStat.Damage, RoleModifierOperation.Multiply, 4f) } }
            };
            _tower.DisplayName = "Cleave";
            _tower.Description = "Swings in an arc.";
            _tower.Cost = 20;
            _tower.SocketCount = 3;
            _tower.Tags = GemTag.Attack | GemTag.Aoe | GemTag.Melee;
            _tower.Roles = new TowerRoleDefinition[] { _role };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_role);
            Object.DestroyImmediate(_tower);
        }

        [Test]
        public void CompletedSet_IsGameplayCompleteTowers()
        {
            Assert.AreEqual(29, WikiTowerCatalogSets.Completed.Length);
            Assert.IsTrue(WikiTowerCatalogSets.TryGet("Cleave", out var cleave));
            Assert.AreEqual(WikiTowerCatalogCategory.Attack, cleave.Category);
            Assert.AreEqual(WikiTowerImportStatus.ProofSet, cleave.Status);
            Assert.IsTrue(cleave.InTowerCatalog);

            Assert.IsTrue(WikiTowerCatalogSets.TryGet("Envy", out var envy));
            Assert.AreEqual(WikiTowerCatalogCategory.Aura, envy.Category);
            Assert.AreEqual(WikiTowerImportStatus.GameplayComplete, envy.Status);
            Assert.IsFalse(envy.InTowerCatalog);

            Assert.IsTrue(WikiTowerCatalogSets.TryGet("Elemental_Weakness", out var weakness));
            Assert.AreEqual(WikiTowerImportStatus.GameplayComplete, weakness.Status);

            Assert.IsFalse(WikiTowerCatalogSets.TryGet("Vigilant_Strike", out _));
            Assert.IsFalse(WikiTowerCatalogSets.TryGet("Vitality", out _));
            Assert.IsFalse(WikiTowerCatalogSets.TryGet("Hexblast", out _));
            Assert.IsFalse(WikiTowerCatalogSets.TryGet("Enfeeble", out _));
        }

        [Test]
        public void FileName_UsesKebabSlug()
        {
            Assert.AreEqual("molten-strike.md", WikiTowerMarkdown.FileNameFromSlug("Molten_Strike"));
            Assert.AreEqual("warlords-mark.md", WikiTowerMarkdown.FileNameFromSlug("Warlords_Mark"));
            Assert.AreEqual("cleave.md", WikiTowerMarkdown.FileNameFromSlug("Cleave"));
        }

        [Test]
        public void Builder_MapsWarpStrikeMixAndSplash()
        {
            Assert.IsTrue(WikiTowerCatalogSets.TryGet("Cleave", out var entry));
            var page = WikiTowerPageBuilder.From(_tower, entry);

            Assert.AreEqual("Cleave", page.Slug);
            Assert.AreEqual("Attack", page.CategoryName);
            Assert.AreEqual("attack", page.CategoryFolder);
            Assert.AreEqual("Direct", page.AimMode);
            Assert.AreEqual("WarpStrike", page.DeliveryPattern);
            Assert.AreEqual("Physical 100", page.Mix);
            StringAssert.Contains("Attack", page.Tags);
            StringAssert.Contains("AoE", page.Tags);
            StringAssert.Contains("Melee", page.Tags);
            Assert.AreEqual(1, page.FirstSourceLevel);
            Assert.AreEqual(10, page.LastSourceLevel);
            Assert.AreEqual("2.4", page.First.SplashRadius);
            Assert.IsTrue(page.InTowerCatalog);
        }

        [Test]
        public void Markdown_IncludesIdentityDeliveryAndNotesMarkers()
        {
            Assert.IsTrue(WikiTowerCatalogSets.TryGet("Cleave", out var entry));
            var markdown = WikiTowerMarkdown.TowerPage(WikiTowerPageBuilder.From(_tower, entry));

            StringAssert.Contains("<!-- wiki-catalog:generated -->", markdown);
            StringAssert.Contains("# Cleave", markdown);
            StringAssert.Contains("`Cleave`", markdown);
            StringAssert.Contains("WarpStrike", markdown);
            StringAssert.Contains("Physical 100", markdown);
            StringAssert.Contains("planning/handoff.md", markdown);
            StringAssert.Contains("../../../../HOME.md", markdown);
            StringAssert.Contains("<!-- wiki-catalog:notes-start -->", markdown);
            StringAssert.Contains("<!-- wiki-catalog:notes-end -->", markdown);
        }

        [Test]
        public void MergeNotes_PreservesHumanText()
        {
            const string generated =
                "<!-- wiki-catalog:generated -->\n# Cleave\n\n## Notes\n\n<!-- wiki-catalog:notes-start -->\n\n<!-- wiki-catalog:notes-end -->\n";
            const string existing =
                "<!-- wiki-catalog:generated -->\n# Old\n\n## Notes\n\n<!-- wiki-catalog:notes-start -->\nSkill Lab: drag into melee.\n<!-- wiki-catalog:notes-end -->\n";

            var merged = WikiTowerMarkdown.MergeNotes(generated, existing);
            StringAssert.Contains("# Cleave", merged);
            StringAssert.Contains("Skill Lab: drag into melee.", merged);
            StringAssert.DoesNotContain("# Old", merged);
        }
    }
}
