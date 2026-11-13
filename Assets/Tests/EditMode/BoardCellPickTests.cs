using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Map;

namespace GemTD.Tests.EditMode
{
    public sealed class BoardCellPickTests
    {
        // Run camera: orthographic, 40° pitch, 45° yaw. See RunCameraController.
        static Ray AimedAt(Vector3 point)
        {
            var dir = Quaternion.Euler(40f, 45f, 0f) * Vector3.forward;
            return new Ray(point - dir * 40f, dir);
        }

        static Vector2Int GroundPlaneCell(Ray ray)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            Assert.IsTrue(plane.Raycast(ray, out var enter));
            var point = ray.GetPoint(enter);
            return new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.z));
        }

        static TileHeightMap MapWith(int x, int z, byte layer)
        {
            var map = new TileHeightMap(21, 21);
            map.Set(x, z, layer);
            return map;
        }

        [Test]
        public void TryPick_PitchedRayOnHighPadTop_ReturnsThatCell()
        {
            var map = MapWith(10, 10, 2);
            var ray = AimedAt(new Vector3(10.5f, TileHeightVisual.TopY(2), 10.5f));

            Assert.IsTrue(BoardCellPick.TryPick(ray, 1f, map, out var cell));
            Assert.AreEqual(new Vector2Int(10, 10), cell);
            Assert.AreNotEqual(cell, GroundPlaneCell(ray));
        }

        [Test]
        public void TryPick_PitchedRayOnHighPadSide_ReturnsThatCell()
        {
            var map = MapWith(10, 10, 2);
            var ray = AimedAt(new Vector3(10.05f, 0.6f, 10.5f));

            Assert.IsTrue(BoardCellPick.TryPick(ray, 1f, map, out var cell));
            Assert.AreEqual(new Vector2Int(10, 10), cell);
            Assert.AreNotEqual(cell, GroundPlaneCell(ray));
        }

        [Test]
        public void TryPick_PitchedRayOnLowPadInFrontOfHighPad_ReturnsLowCell()
        {
            var map = MapWith(10, 10, 2);
            var ray = AimedAt(new Vector3(9.5f, TileHeightVisual.TopY(0), 9.5f));

            Assert.IsTrue(BoardCellPick.TryPick(ray, 1f, map, out var cell));
            Assert.AreEqual(new Vector2Int(9, 9), cell);
        }

        [Test]
        public void TryPick_RayMissesBoard_ReturnsFalse()
        {
            var map = new TileHeightMap(21, 21);
            var ray = new Ray(new Vector3(-10f, 5f, -10f), Vector3.down);

            Assert.IsFalse(BoardCellPick.TryPick(ray, 1f, map, out _));
        }
    }
}
