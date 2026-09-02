using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Map;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class TowerViewPlacementTests
    {
        [Test]
        public void Bind_SitsMeshBottomOnPadTop_EachHeightLayer()
        {
            for (byte layer = 0; layer < 3; layer++)
            {
                var go = new GameObject("Tower");
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                try
                {
                    cube.transform.SetParent(go.transform, false);
                    cube.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                    cube.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                    var view = go.AddComponent<TowerView>();
                    var padTop = new Vector3(1f, TileHeightVisual.TopY(layer), 2f);
                    view.Bind(null, padTop);

                    var minY = cube.GetComponent<MeshRenderer>().bounds.min.y;
                    Assert.AreEqual(padTop.y, minY, 1e-3f, $"layer {layer}");
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void Bind_DoesNotOverwriteAuthoredAlbedo()
        {
            var go = new GameObject("Tower");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Material mat = null;
            try
            {
                cube.transform.SetParent(go.transform, false);
                var shader = Shader.Find("Sprites/Default");
                Assert.IsNotNull(shader);
                mat = new Material(shader);
                var authored = new Color(0.31f, 0.72f, 0.44f, 1f);
                mat.SetColor("_Color", authored);
                cube.GetComponent<MeshRenderer>().sharedMaterial = mat;

                var view = go.AddComponent<TowerView>();
                view.Bind(null, Vector3.zero);
                view.SetSelected(true);
                view.SetSelected(false);

                var renderer = cube.GetComponent<MeshRenderer>();
                Assert.AreEqual(authored, renderer.sharedMaterial.GetColor("_Color"));
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.AreNotEqual(new Color(0.45f, 0.5f, 0.55f), block.GetColor("_BaseColor"));
                Assert.AreNotEqual(new Color(0.95f, 0.75f, 0.25f), block.GetColor("_BaseColor"));
            }
            finally
            {
                Object.DestroyImmediate(go);
                if (mat != null)
                    Object.DestroyImmediate(mat);
            }
        }
    }
}
