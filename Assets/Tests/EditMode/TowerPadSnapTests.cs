using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerPadSnapTests
    {
        [Test]
        public void FootLocalY_CubeCenteredAtOrigin_LiftsHalfHeight()
        {
            var parent = new GameObject("Pad");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                cube.transform.SetParent(parent.transform, false);
                cube.transform.localPosition = Vector3.zero;
                cube.transform.localScale = new Vector3(1f, 2f, 1f);

                var lift = TowerPadSnap.FootLocalY(cube.transform);

                Assert.AreEqual(1f, lift, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(cube);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void FootLocalY_MeshDroppedBelowPivot_CompensatesOffset()
        {
            var parent = new GameObject("Pad");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                cube.transform.SetParent(parent.transform, false);
                cube.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                cube.transform.localScale = Vector3.one;

                var lift = TowerPadSnap.FootLocalY(cube.transform);

                Assert.AreEqual(1f, lift, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(cube);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void FootLocalY_IgnoresParentWorldHeight()
        {
            var parent = new GameObject("Pad");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                parent.transform.position = new Vector3(2f, GemTD.Gameplay.Map.TileHeightVisual.TopY(2), 4f);
                cube.transform.SetParent(parent.transform, false);
                cube.transform.localPosition = Vector3.zero;
                cube.transform.localScale = new Vector3(0.7f, 1.1f, 0.7f);

                var lift = TowerPadSnap.FootLocalY(cube.transform);

                Assert.AreEqual(0.55f, lift, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(cube);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void ApplyFootOnParentOrigin_PutsMeshBottomOnLocalZero()
        {
            var parent = new GameObject("Pad");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                cube.transform.SetParent(parent.transform, false);
                cube.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                cube.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                TowerPadSnap.ApplyFootOnParentOrigin(cube.transform);

                var minY = cube.GetComponent<MeshRenderer>().bounds.min.y;
                Assert.AreEqual(parent.transform.position.y, minY, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(cube);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void UniformizeLocalScale_CopiesXToYZ()
        {
            var go = new GameObject("Occupant");
            try
            {
                go.transform.localScale = new Vector3(0.28f, 0.18f, 0.28f);
                TowerPadSnap.UniformizeLocalScale(go.transform);
                Assert.AreEqual(0.28f, go.transform.localScale.x, 1e-4f);
                Assert.AreEqual(0.28f, go.transform.localScale.y, 1e-4f);
                Assert.AreEqual(0.28f, go.transform.localScale.z, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
