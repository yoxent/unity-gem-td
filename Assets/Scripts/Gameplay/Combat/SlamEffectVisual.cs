using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    /// <summary>Slam VFX: scale uniformly to the AoE diameter and sit on the ground plane.</summary>
    public static class SlamEffectVisual
    {
        public static Vector3 ScaleToDiameter(float aoeRadius)
        {
            var radius = aoeRadius > 0.4f ? aoeRadius : 0.4f;
            var diameter = radius * 2f;
            return Vector3.one * diameter;
        }

        public static Vector3 SitOnGround(Vector3 groundPoint, float meshExtentsY, float scaleY)
        {
            var halfHeight = meshExtentsY > 0f ? meshExtentsY * scaleY : 0f;
            if (halfHeight < 0f)
                halfHeight = 0f;
            return new Vector3(groundPoint.x, groundPoint.y + halfHeight, groundPoint.z);
        }
    }
}
