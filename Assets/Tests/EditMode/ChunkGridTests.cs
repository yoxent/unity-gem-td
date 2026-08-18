using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public class ChunkGridTests
    {
        static ChunkMask StraightNS()
        {
            var m = new bool[ChunkMask.CellCount];
            for (var y = 0; y < ChunkMask.Size; y++) m[y * ChunkMask.Size + 2] = true;
            return new ChunkMask(m);
        }

        static MapChunkStamp MakeStamp(ChunkMask mask)
        {
            var go = new GameObject("stamp");
            var stamp = go.AddComponent<MapChunkStamp>();
            stamp.ApplyMask(mask);
            return stamp;
        }

        [Test] public void Place_AndTryGet_ReturnsSlot()
        {
            var grid = new ChunkGrid(9, 9);
            var mask = StraightNS();
            var prefab = MakeStamp(mask);
            var slot = new ChunkSlot(prefab, 1, mask.Rotated(1));

            grid.Place(new Vector2Int(4, 4), slot);

            Assert.IsTrue(grid.TryGet(4, 4, out var got));
            Assert.AreSame(prefab, got.Prefab);
            Assert.AreEqual(1, got.Yaw);
            Assert.AreEqual(EdgeFlags.East | EdgeFlags.West, got.Mask.OpenEdges);
            Object.DestroyImmediate(prefab.gameObject);
        }

        [Test] public void IsOccupied_TrueAfterPlace_FalseBefore()
        {
            var grid = new ChunkGrid(9, 9);
            Assert.IsFalse(grid.IsOccupied(4, 4));
            grid.Place(new Vector2Int(4, 4), new ChunkSlot(null, 0, default));
            Assert.IsTrue(grid.IsOccupied(4, 4));
            Assert.AreEqual(1, grid.Count);
        }

        [Test] public void OpenEdgeAt_ReflectsRotatedMask()
        {
            var grid = new ChunkGrid(9, 9);
            var mask = StraightNS();
            grid.Place(new Vector2Int(4, 4), new ChunkSlot(null, 1, mask.Rotated(1)));
            Assert.IsTrue(grid.OpenEdgeAt(4, 4, EdgeFlags.East));
            Assert.IsTrue(grid.OpenEdgeAt(4, 4, EdgeFlags.West));
            Assert.IsFalse(grid.OpenEdgeAt(4, 4, EdgeFlags.North));
            Assert.IsFalse(grid.OpenEdgeAt(4, 4, EdgeFlags.South));
        }

        [Test] public void NeighborCoord_NorthIsPlusY()
        {
            var grid = new ChunkGrid(9, 9);
            var c = new Vector2Int(4, 4);
            Assert.AreEqual(new Vector2Int(4, 5), grid.NeighborCoord(c, EdgeFlags.North));
            Assert.AreEqual(new Vector2Int(4, 3), grid.NeighborCoord(c, EdgeFlags.South));
            Assert.AreEqual(new Vector2Int(5, 4), grid.NeighborCoord(c, EdgeFlags.East));
            Assert.AreEqual(new Vector2Int(3, 4), grid.NeighborCoord(c, EdgeFlags.West));
        }

        [Test] public void InBounds_RespectsGridSize()
        {
            var grid = new ChunkGrid(9, 9);
            Assert.IsTrue(grid.InBounds(0, 0));
            Assert.IsTrue(grid.InBounds(8, 8));
            Assert.IsFalse(grid.InBounds(-1, 0));
            Assert.IsFalse(grid.InBounds(0, -1));
            Assert.IsFalse(grid.InBounds(9, 0));
            Assert.IsFalse(grid.InBounds(0, 9));
        }
    }
}
