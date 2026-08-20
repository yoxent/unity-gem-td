using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public sealed class ChunkCatalogTests
    {
        [Test]
        public void CatalogFor_ReturnsBoundTypeCatalog()
        {
            var stamp = MakeStamp("corner");
            var corner = ScriptableObject.CreateInstance<ChunkTypeCatalog>();
            corner.Configure(ChunkType.Corner, new[] { stamp });
            var index = ScriptableObject.CreateInstance<ChunkCatalog>();
            index.SetTypeCatalog(ChunkType.Corner, corner);

            Assert.AreSame(corner, index.CatalogFor(ChunkType.Corner));
            Assert.IsNull(index.CatalogFor(ChunkType.Land));

            Object.DestroyImmediate(stamp.gameObject);
            Object.DestroyImmediate(corner);
            Object.DestroyImmediate(index);
        }

        [Test]
        public void CopyAll_ReadsFromTypeCatalogs_SkipsNulls()
        {
            var dead = MakeStamp("dead");
            var straight = MakeStamp("straight");
            var deadCat = ScriptableObject.CreateInstance<ChunkTypeCatalog>();
            deadCat.Configure(ChunkType.DeadEnd, new[] { dead, null });
            var straightCat = ScriptableObject.CreateInstance<ChunkTypeCatalog>();
            straightCat.Configure(ChunkType.Straight, new[] { straight });
            var index = ScriptableObject.CreateInstance<ChunkCatalog>();
            index.SetTypeCatalog(ChunkType.DeadEnd, deadCat);
            index.SetTypeCatalog(ChunkType.Straight, straightCat);

            var into = new List<MapChunkStamp>();
            index.CopyAll(into);

            Assert.AreEqual(2, into.Count);
            Assert.AreSame(dead, into[0]);
            Assert.AreSame(straight, into[1]);

            Object.DestroyImmediate(dead.gameObject);
            Object.DestroyImmediate(straight.gameObject);
            Object.DestroyImmediate(deadCat);
            Object.DestroyImmediate(straightCat);
            Object.DestroyImmediate(index);
        }

        [Test]
        public void CopyAll_EmptyTypeCatalogs_Ok()
        {
            var index = ScriptableObject.CreateInstance<ChunkCatalog>();
            var into = new List<MapChunkStamp>();
            index.CopyAll(into);
            Assert.AreEqual(0, into.Count);
            Object.DestroyImmediate(index);
        }

        [Test]
        public void CatalogFor_Homebase_ReturnsBoundCatalog()
        {
            var stamp = MakeStamp("keep");
            var home = ScriptableObject.CreateInstance<ChunkTypeCatalog>();
            home.Configure(ChunkType.Homebase, new[] { stamp });
            var index = ScriptableObject.CreateInstance<ChunkCatalog>();
            index.SetTypeCatalog(ChunkType.Homebase, home);

            Assert.AreSame(home, index.CatalogFor(ChunkType.Homebase));

            Object.DestroyImmediate(stamp.gameObject);
            Object.DestroyImmediate(home);
            Object.DestroyImmediate(index);
        }

        [Test]
        public void CopyAll_DoesNotIncludeHomebase()
        {
            var keep = MakeStamp("keep");
            var straight = MakeStamp("straight");
            var homeCat = ScriptableObject.CreateInstance<ChunkTypeCatalog>();
            homeCat.Configure(ChunkType.Homebase, new[] { keep });
            var straightCat = ScriptableObject.CreateInstance<ChunkTypeCatalog>();
            straightCat.Configure(ChunkType.Straight, new[] { straight });
            var index = ScriptableObject.CreateInstance<ChunkCatalog>();
            index.SetTypeCatalog(ChunkType.Homebase, homeCat);
            index.SetTypeCatalog(ChunkType.Straight, straightCat);

            var into = new List<MapChunkStamp>();
            index.CopyAll(into);

            Assert.AreEqual(1, into.Count);
            Assert.AreSame(straight, into[0]);

            Object.DestroyImmediate(keep.gameObject);
            Object.DestroyImmediate(straight.gameObject);
            Object.DestroyImmediate(homeCat);
            Object.DestroyImmediate(straightCat);
            Object.DestroyImmediate(index);
        }

        [Test]
        public void CopyType_ReadsOnlyThatBucket()
        {
            var keep = MakeStamp("keep");
            var straight = MakeStamp("straight");
            var homeCat = ScriptableObject.CreateInstance<ChunkTypeCatalog>();
            homeCat.Configure(ChunkType.Homebase, new[] { keep });
            var straightCat = ScriptableObject.CreateInstance<ChunkTypeCatalog>();
            straightCat.Configure(ChunkType.Straight, new[] { straight });
            var index = ScriptableObject.CreateInstance<ChunkCatalog>();
            index.SetTypeCatalog(ChunkType.Homebase, homeCat);
            index.SetTypeCatalog(ChunkType.Straight, straightCat);

            var keeps = new List<MapChunkStamp>();
            index.CopyType(ChunkType.Homebase, keeps);
            Assert.AreEqual(1, keeps.Count);
            Assert.AreSame(keep, keeps[0]);

            var straights = new List<MapChunkStamp>();
            index.CopyType(ChunkType.Straight, straights);
            Assert.AreEqual(1, straights.Count);
            Assert.AreSame(straight, straights[0]);

            Object.DestroyImmediate(keep.gameObject);
            Object.DestroyImmediate(straight.gameObject);
            Object.DestroyImmediate(homeCat);
            Object.DestroyImmediate(straightCat);
            Object.DestroyImmediate(index);
        }

        [Test]
        public void TryAddStamp_AddsOnce_SkipsSameReference()
        {
            var stamp = MakeStamp("corner");
            var catalog = ScriptableObject.CreateInstance<ChunkTypeCatalog>();
            catalog.Configure(ChunkType.Corner, null);

            Assert.IsTrue(catalog.TryAddStamp(stamp));
            Assert.AreEqual(1, catalog.Stamps.Count);
            Assert.IsFalse(catalog.TryAddStamp(stamp));
            Assert.AreEqual(1, catalog.Stamps.Count);

            Object.DestroyImmediate(stamp.gameObject);
            Object.DestroyImmediate(catalog);
        }

        static MapChunkStamp MakeStamp(string name)
        {
            var go = new GameObject(name);
            return go.AddComponent<MapChunkStamp>();
        }
    }
}
