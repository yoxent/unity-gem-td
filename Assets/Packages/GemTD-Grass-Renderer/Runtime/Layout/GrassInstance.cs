using UnityEngine;

namespace GemTD.Grass
{
    public readonly struct GrassInstance
    {
        public readonly Vector3 LocalPosition;
        public readonly float YawDegrees;
        public readonly float UniformScale;
        public readonly int VariantIndex;

        public GrassInstance(Vector3 localPosition, float yawDegrees, float uniformScale, int variantIndex)
        {
            LocalPosition = localPosition;
            YawDegrees = yawDegrees;
            UniformScale = uniformScale;
            VariantIndex = variantIndex;
        }
    }
}
