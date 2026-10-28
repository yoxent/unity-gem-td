using NUnit.Framework;
using UnityEngine;
using GemTD.Gameplay.Towers;

namespace GemTD.Tests.EditMode
{
    public sealed class PlacementGhostViewTests
    {
        [Test]
        public void EnsureBuilt_KeepsPrefabLocalScale()
        {
            var source = NewTower("Src", new Vector3(0.7f, 0.7f, 0.7f));
            var ghostGo = new GameObject("Ghost");
            var ghost = ghostGo.AddComponent<PlacementGhostView>();
            try
            {
                ghost.EnsureBuilt(source, null);

                var visual = FindChild(ghostGo.transform, "GhostTowerVisual");
                Assert.IsNotNull(visual);
                Assert.AreEqual(0.7f, visual.localScale.x, 1e-4f);
                Assert.AreEqual(0.7f, visual.localScale.y, 1e-4f);
                Assert.AreEqual(0.7f, visual.localScale.z, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(ghostGo);
                Object.DestroyImmediate(source.gameObject);
            }
        }

        [Test]
        public void EnsureBuilt_HidesOccupantWithAnimator()
        {
            var source = NewTower("Src", Vector3.one);
            var occupant = new GameObject("Knight");
            occupant.AddComponent<Animator>();
            occupant.transform.SetParent(source.transform, false);
            var ghostGo = new GameObject("Ghost");
            var ghost = ghostGo.AddComponent<PlacementGhostView>();
            try
            {
                ghost.EnsureBuilt(source, null);

                var visual = FindChild(ghostGo.transform, "GhostTowerVisual");
                var ghostOccupant = FindChild(visual, "Knight");
                Assert.IsNotNull(ghostOccupant);
                Assert.IsFalse(ghostOccupant.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(ghostGo);
                Object.DestroyImmediate(source.gameObject);
            }
        }

        [Test]
        public void ShowAt_SitsMeshBottomOnPadTop()
        {
            var source = NewTower("Src", Vector3.one);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(source.transform, false);
            cube.transform.localPosition = new Vector3(0f, -0.5f, 0f);

            var ghostGo = new GameObject("Ghost");
            var ghost = ghostGo.AddComponent<PlacementGhostView>();
            try
            {
                ghost.EnsureBuilt(source, null);
                var padTop = new Vector3(3f, 1.25f, 5f);
                ghost.ShowAt(padTop, true);

                var visual = FindChild(ghostGo.transform, "GhostTowerVisual");
                var renderer = visual.GetComponentInChildren<MeshRenderer>();
                Assert.AreEqual(padTop.y, renderer.bounds.min.y, 1e-3f);
            }
            finally
            {
                Object.DestroyImmediate(ghostGo);
                Object.DestroyImmediate(source.gameObject);
            }
        }

        static TowerView NewTower(string name, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.localScale = scale;
            return go.AddComponent<TowerView>();
        }

        static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
