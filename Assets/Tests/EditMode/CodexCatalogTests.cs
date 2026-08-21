using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Meta;

namespace GemTD.Tests.EditMode
{
    public sealed class CodexCatalogTests
    {
        [Test]
        public void GetById_Found_ReturnsEntry()
        {
            var entry = ScriptableObject.CreateInstance<CodexEntry>();
            entry.Id = "hydra-ballista";
            entry.DisplayName = "Hydra Ballista";

            var catalog = ScriptableObject.CreateInstance<CodexCatalog>();
            catalog.Entries = new[] { entry };

            Assert.AreSame(entry, catalog.GetById("hydra-ballista"));
        }

        [Test]
        public void GetById_Miss_ReturnsNull()
        {
            var catalog = ScriptableObject.CreateInstance<CodexCatalog>();
            catalog.Entries = new[]
            {
                ScriptableObject.CreateInstance<CodexEntry>(),
            };
            catalog.Entries[0].Id = "other";

            Assert.IsNull(catalog.GetById("hydra-ballista"));
        }

        [Test]
        public void GetById_NullEntries_ReturnsNull()
        {
            var catalog = ScriptableObject.CreateInstance<CodexCatalog>();
            Assert.IsNull(catalog.GetById("anything"));
        }
    }
}