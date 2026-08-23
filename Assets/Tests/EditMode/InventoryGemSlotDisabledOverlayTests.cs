using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Gems;
using GemTD.Gameplay.Towers;
using GemTD.UI;

namespace GemTD.Tests.EditMode
{
    public sealed class InventoryGemSlotDisabledOverlayTests
    {
        TowerDefinition _projectile;
        TowerDefinition _aura;
        AttackRoleDefinition _projectileRole;
        AuraRoleDefinition _auraRole;
        GemDefinition _projectileGem;
        GemDefinition _aoeGem;

        [SetUp]
        public void SetUp()
        {
            _projectile = ScriptableObject.CreateInstance<TowerDefinition>();
            _projectileRole = ScriptableObject.CreateInstance<AttackRoleDefinition>();
            _projectile.Roles = new TowerRoleDefinition[] { _projectileRole };

            _aura = ScriptableObject.CreateInstance<TowerDefinition>();
            _auraRole = ScriptableObject.CreateInstance<AuraRoleDefinition>();
            _aura.Roles = new TowerRoleDefinition[] { _auraRole };

            _projectileGem = ScriptableObject.CreateInstance<GemDefinition>();
            _projectileGem.Tags = GemTag.Support | GemTag.Projectile;

            _aoeGem = ScriptableObject.CreateInstance<GemDefinition>();
            _aoeGem.Tags = GemTag.Support | GemTag.Aoe;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_projectile);
            Object.DestroyImmediate(_aura);
            Object.DestroyImmediate(_projectileRole);
            Object.DestroyImmediate(_auraRole);
            Object.DestroyImmediate(_projectileGem);
            Object.DestroyImmediate(_aoeGem);
        }

        [Test]
        public void Hidden_WhenNoTowerSelected()
        {
            Assert.IsFalse(InventoryGemSlot.ShouldShowDisabledOverlay(_projectileGem, null));
        }

        [Test]
        public void Hidden_WhenSlotEmpty()
        {
            Assert.IsFalse(InventoryGemSlot.ShouldShowDisabledOverlay(null, _projectile));
        }

        [Test]
        public void Hidden_WhenGemSocketsSelectedTower()
        {
            Assert.IsFalse(InventoryGemSlot.ShouldShowDisabledOverlay(_projectileGem, _projectile));
        }

        [Test]
        public void Shown_WhenGemCannotSocketSelectedTower()
        {
            Assert.IsTrue(InventoryGemSlot.ShouldShowDisabledOverlay(_projectileGem, _aura));
            Assert.IsTrue(InventoryGemSlot.ShouldShowDisabledOverlay(_aoeGem, _projectile));
        }
    }
}
