using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GemTD.Grass;
using GemTD.Gameplay.Grid;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Run;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class ChunkGrassPatchAdapterTests
    {
        readonly List<GameObject> _roots = new List<GameObject>();
        readonly List<TowerDefinition> _definitions = new List<TowerDefinition>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _roots.Count; i++)
            {
                if (_roots[i] != null)
                    Object.DestroyImmediate(_roots[i]);
            }

            for (var i = 0; i < _definitions.Count; i++)
            {
                if (_definitions[i] != null)
                    Object.DestroyImmediate(_definitions[i]);
            }
        }

        [Test]
        public void BuildPatches_PathAndLockedCells_AreAbsent()
        {
            var root = CreateRoot();
            AddTile(root, 0, 0);
            AddTile(root, 1, 0);
            AddTile(root, 2, 0);

            var path = new bool[ChunkMask.CellCount];
            path[Index(1, 0)] = true;
            var locked = new bool[ChunkMask.CellCount];
            locked[Index(2, 0)] = true;
            var mask = new ChunkMask(path, -1, locked);
            var grid = new ChunkGrid(1, 1);
            var slot = PlaceChunk(grid, Vector2Int.zero, mask);
            var output = new List<GrassSurfacePatch>();

            ChunkGrassPatchAdapter.BuildPatches(
                root.transform,
                Vector2Int.zero,
                in slot,
                grid,
                null,
                0f,
                output);

            Assert.AreEqual(1, output.Count);
            Assert.AreEqual(new Vector3(0.5f, 0.5f, 0.5f), output[0].LocalCenter);
        }

        [Test]
        public void BuildPatches_UsesRaisedPadTopY()
        {
            var root = CreateRoot();
            AddTile(root, 0, 0, yPosition: 0.05f, scaleY: 0.1f);
            var mask = AllEligible();
            var grid = new ChunkGrid(1, 1);
            var slot = PlaceChunk(grid, Vector2Int.zero, mask);
            var heights = new TileHeightMap(ChunkMask.Size, ChunkMask.Size);
            heights.Set(0, 0, 2);
            var output = new List<GrassSurfacePatch>();

            ChunkGrassPatchAdapter.BuildPatches(
                root.transform,
                Vector2Int.zero,
                in slot,
                grid,
                heights,
                0.03f,
                output);

            Assert.AreEqual(1, output.Count);
            Assert.That(output[0].LocalCenter.y, Is.EqualTo(1.03f).Within(0.0001f));
        }

        [Test]
        public void BuildPatches_PathNeighbor_MarksInsetEdge()
        {
            var root = CreateRoot();
            AddTile(root, 3, 3);
            var path = new bool[ChunkMask.CellCount];
            path[Index(3, 4)] = true;
            var mask = new ChunkMask(path);
            var grid = new ChunkGrid(1, 1);
            var slot = PlaceChunk(grid, Vector2Int.zero, mask);
            var output = new List<GrassSurfacePatch>();

            ChunkGrassPatchAdapter.BuildPatches(
                root.transform,
                Vector2Int.zero,
                in slot,
                grid,
                null,
                0f,
                output);

            Assert.AreEqual(1, output.Count);
            Assert.IsTrue((output[0].InsetEdges & GrassPatchEdges.North) != 0);
            Assert.IsFalse((output[0].CliffEdges & GrassPatchEdges.North) != 0);
        }

        [Test]
        public void BuildPatches_MissingNeighborChunk_MarksCliffEdge()
        {
            var root = CreateRoot();
            AddTile(root, 0, 3);
            var mask = AllEligible();
            var grid = new ChunkGrid(2, 1);
            var slot = PlaceChunk(grid, Vector2Int.zero, mask);
            var output = new List<GrassSurfacePatch>();

            ChunkGrassPatchAdapter.BuildPatches(
                root.transform,
                Vector2Int.zero,
                in slot,
                grid,
                null,
                0f,
                output);

            Assert.AreEqual(1, output.Count);
            Assert.IsTrue((output[0].CliffEdges & GrassPatchEdges.West) != 0);
        }

        [Test]
        public void BuildPatches_AdjacentGrassChunk_DoesNotMarkCliffSeam()
        {
            var root = CreateRoot();
            AddTile(root, ChunkMask.Size - 1, 3);
            var mask = AllEligible();
            var grid = new ChunkGrid(2, 1);
            var slot = PlaceChunk(grid, Vector2Int.zero, mask);
            PlaceChunk(grid, new Vector2Int(1, 0), mask);
            var output = new List<GrassSurfacePatch>();

            ChunkGrassPatchAdapter.BuildPatches(
                root.transform,
                Vector2Int.zero,
                in slot,
                grid,
                null,
                0f,
                output);

            Assert.AreEqual(1, output.Count);
            Assert.IsFalse((output[0].CliffEdges & GrassPatchEdges.East) != 0);
            Assert.IsFalse((output[0].InsetEdges & GrassPatchEdges.East) != 0);
        }

        [Test]
        public void BuildPatches_RotatedChunk_PreservesWorldCellStableKey()
        {
            var mask = AllEligible();
            var rotatedRoot = CreateRoot();
            AddTile(rotatedRoot, 1, 2);
            var rotatedGrid = new ChunkGrid(1, 1);
            var rotatedSlot = PlaceChunk(rotatedGrid, Vector2Int.zero, mask, 1);
            var rotatedOutput = new List<GrassSurfacePatch>();

            ChunkGrassPatchAdapter.BuildPatches(
                rotatedRoot.transform,
                Vector2Int.zero,
                in rotatedSlot,
                rotatedGrid,
                null,
                0f,
                rotatedOutput);

            var unrotatedRoot = CreateRoot();
            AddTile(unrotatedRoot, 2, ChunkMask.Size - 1 - 1);
            var unrotatedGrid = new ChunkGrid(1, 1);
            var unrotatedSlot = PlaceChunk(unrotatedGrid, Vector2Int.zero, mask);
            var unrotatedOutput = new List<GrassSurfacePatch>();

            ChunkGrassPatchAdapter.BuildPatches(
                unrotatedRoot.transform,
                Vector2Int.zero,
                in unrotatedSlot,
                unrotatedGrid,
                null,
                0f,
                unrotatedOutput);

            Assert.AreEqual(1, rotatedOutput.Count);
            Assert.AreEqual(1, unrotatedOutput.Count);
            Assert.AreEqual(rotatedOutput[0].StableKey, unrotatedOutput[0].StableKey);
        }

        [Test]
        public void BuildExclusions_OccupiedCell_UsesStyleClearanceRadius()
        {
            var root = CreateRoot();
            AddTile(root, 1, 2);
            var mask = AllEligible();
            var grid = new ChunkGrid(1, 1);
            var slot = PlaceChunk(grid, Vector2Int.zero, mask, 1);
            var worldCell = new Vector2Int(2, ChunkMask.Size - 1 - 1);
            var placement = CreatePlacement(worldCell);
            Assert.IsTrue(placement.TryPlace(
                _definitions[0],
                worldCell,
                RunStateId.Plan,
                1,
                out _));
            var output = new List<GrassExclusion>
            {
                GrassExclusion.Circle(Vector2.zero, 1f)
            };

            ChunkGrassPatchAdapter.BuildExclusions(
                root.transform,
                Vector2Int.zero,
                in slot,
                placement,
                0.42f,
                output);

            Assert.AreEqual(1, output.Count);
            Assert.AreEqual(GrassExclusionShape.Circle, output[0].Shape);
            Assert.AreEqual(new Vector2(1.5f, 2.5f), output[0].LocalCenter);
            Assert.That(output[0].Size.x, Is.EqualTo(0.42f).Within(0.0001f));
            Assert.That(output[0].Size.y, Is.EqualTo(0.42f).Within(0.0001f));
        }

        [Test]
        public void BuildExclusions_UnoccupiedChunk_ReturnsEmpty()
        {
            var root = CreateRoot();
            AddTile(root, 1, 2);
            var mask = AllEligible();
            var grid = new ChunkGrid(1, 1);
            var slot = PlaceChunk(grid, Vector2Int.zero, mask);
            var placement = CreatePlacement(new Vector2Int(6, 6));
            var output = new List<GrassExclusion>
            {
                GrassExclusion.Circle(Vector2.one, 1f)
            };

            ChunkGrassPatchAdapter.BuildExclusions(
                root.transform,
                Vector2Int.zero,
                in slot,
                placement,
                0.42f,
                output);

            Assert.Zero(output.Count);
        }

        [Test]
        public void CalculateLocalBounds_ContainsEveryPatchRectangle()
        {
            var patches = new List<GrassSurfacePatch>
            {
                new GrassSurfacePatch(
                    new Vector3(1f, 2f, -1f),
                    new Vector2(0.5f, 1f),
                    GrassPatchEdges.None,
                    GrassPatchEdges.None,
                    1),
                new GrassSurfacePatch(
                    new Vector3(-3f, 5f, 4f),
                    new Vector2(1.5f, 0.25f),
                    GrassPatchEdges.None,
                    GrassPatchEdges.None,
                    2)
            };

            var bounds = ChunkGrassPatchAdapter.CalculateLocalBounds(patches);

            Assert.That(bounds.min.x, Is.EqualTo(-4.5f).Within(0.0001f));
            Assert.That(bounds.max.x, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(bounds.min.y, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(bounds.max.y, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(bounds.min.z, Is.EqualTo(-2f).Within(0.0001f));
            Assert.That(bounds.max.z, Is.EqualTo(4.25f).Within(0.0001f));
        }

        GameObject CreateRoot()
        {
            var root = new GameObject("ChunkGrassAdapterTestRoot");
            _roots.Add(root);
            return root;
        }

        static GameObject AddTile(GameObject root, int x, int y, float yPosition = 0f, float scaleY = 1f)
        {
            var tile = new GameObject($"Tile_{x}_{y}");
            tile.transform.SetParent(root.transform, false);
            tile.transform.localPosition = new Vector3(x + 0.5f, yPosition, y + 0.5f);
            tile.transform.localScale = new Vector3(1f, scaleY, 1f);
            return tile;
        }

        static ChunkSlot PlaceChunk(ChunkGrid grid, Vector2Int coord, ChunkMask mask, int yaw = 0)
        {
            var slot = new ChunkSlot(null, yaw, mask);
            grid.Place(coord, slot);
            return slot;
        }

        TowerPlacementService CreatePlacement(Vector2Int buildableCell)
        {
            var board = new GridBoard(16, 16);
            var graph = new PathGraph(16, 16);
            graph.BindBoard(board);
            board.SetBuildable(buildableCell.x, buildableCell.y, true);
            var economy = new RunEconomy(100, 20);
            var placement = new TowerPlacementService(board, graph, economy);
            var definition = ScriptableObject.CreateInstance<TowerDefinition>();
            definition.DisplayName = "Grass Adapter Test Tower";
            definition.Cost = 1;
            definition.SocketCount = 0;
            _definitions.Add(definition);
            return placement;
        }

        static ChunkMask AllEligible()
        {
            return new ChunkMask(new bool[ChunkMask.CellCount]);
        }

        static int Index(int x, int y) => y * ChunkMask.Size + x;
    }
}
