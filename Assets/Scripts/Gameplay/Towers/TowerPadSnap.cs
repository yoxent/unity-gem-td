using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>Sit a tower mesh on a pad without assuming a centered greybox pivot.</summary>
    public static class TowerPadSnap
    {
        public static float FootLocalY(Transform structure)
        {
            if (structure == null)
                return 0f;

            var minLocalY = MinMeshY(structure, structure.parent, worldSpace: false);
            if (minLocalY >= float.MaxValue)
                return 0f;
            return -minLocalY;
        }

        public static void ApplyFootOnParentOrigin(Transform structure)
        {
            if (structure == null)
                return;

            var pos = structure.localPosition;
            pos.y += FootLocalY(structure);
            structure.localPosition = pos;
        }

        public static void SitOnWorldPad(Transform root, Vector3 padTop)
        {
            if (root == null)
                return;

            root.position = padTop;
            var minY = MinMeshY(root, null, worldSpace: true);
            if (minY >= float.MaxValue)
                return;
            root.position += Vector3.up * (padTop.y - minY);
        }

        public static void UniformizeLocalScale(Transform t)
        {
            if (t == null)
                return;

            var s = t.localScale;
            if (Mathf.Approximately(s.y, s.x) && Mathf.Approximately(s.z, s.x))
                return;
            t.localScale = new Vector3(s.x, s.x, s.x);
        }

        static float MinMeshY(Transform root, Transform space, bool worldSpace)
        {
            var min = float.MaxValue;
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < filters.Length; i++)
            {
                var filter = filters[i];
                if (filter == null || filter.sharedMesh == null)
                    continue;

                var b = filter.sharedMesh.bounds;
                var t = filter.transform;
                for (var x = 0; x < 2; x++)
                for (var y = 0; y < 2; y++)
                for (var z = 0; z < 2; z++)
                {
                    var local = new Vector3(
                        x == 0 ? b.min.x : b.max.x,
                        y == 0 ? b.min.y : b.max.y,
                        z == 0 ? b.min.z : b.max.z);
                    var world = t.TransformPoint(local);
                    var sample = worldSpace || space == null
                        ? world.y
                        : space.InverseTransformPoint(world).y;
                    if (sample < min)
                        min = sample;
                }
            }

            return min;
        }
    }
}
