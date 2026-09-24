using System;
using System.Collections.Generic;
using UnityEngine;

namespace GemTD.Grass
{
    [Serializable]
    public struct GrassProfileRow
    {
        [SerializeField, Tooltip("Normalized vertical position from blade root (0) to tip (1).")]
        float normalizedHeight;
        [SerializeField, Tooltip("Blade width multiplier at this height. Must be nonnegative.")]
        float widthFactor;

        public float NormalizedHeight => normalizedHeight;
        public float WidthFactor => widthFactor;

        public GrassProfileRow(float normalizedHeight, float widthFactor)
        {
            this.normalizedHeight = normalizedHeight;
            this.widthFactor = widthFactor;
        }
    }

    [Serializable]
    public struct GrassBladeDefinition
    {
        [SerializeField, Tooltip("Local-space position of the blade root.")]
        Vector3 basePosition;
        [SerializeField, Tooltip("Blade rotation around the local up axis, in degrees.")]
        float yawDegrees;
        [SerializeField, Tooltip("Blade height in local metres. Must be greater than zero.")]
        float height;
        [SerializeField, Tooltip("Blade root width in local metres. Must be greater than zero.")]
        float width;
        [SerializeField, Tooltip("Tip displacement along the blade's local forward axis.")]
        float forwardBend;
        [SerializeField, Tooltip("Tip displacement along the blade's local right axis.")]
        float sideBend;

        public Vector3 BasePosition => basePosition;
        public float YawDegrees => yawDegrees;
        public float Height => height;
        public float Width => width;
        public float ForwardBend => forwardBend;
        public float SideBend => sideBend;

        public GrassBladeDefinition(
            Vector3 basePosition,
            float yawDegrees,
            float height,
            float width,
            float forwardBend,
            float sideBend)
        {
            this.basePosition = basePosition;
            this.yawDegrees = yawDegrees;
            this.height = height;
            this.width = width;
            this.forwardBend = forwardBend;
            this.sideBend = sideBend;
        }
    }

    [CreateAssetMenu(
        fileName = "GrassClump_Definition",
        menuName = "Gem TD/Grass/Clump Definition")]
    public sealed class GrassClumpDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Ordered root-to-tip blade profile. List length controls vertical mesh smoothness.")]
        List<GrassProfileRow> profileRows = new List<GrassProfileRow>();
        [SerializeField, Tooltip("Ordered blades in this clump. List length controls blade count.")]
        List<GrassBladeDefinition> blades = new List<GrassBladeDefinition>();
        [SerializeField, Tooltip("Package-owned mesh updated in place by the editor baker. Runtime uses only this baked mesh.")]
        Mesh outputMesh;

        public IReadOnlyList<GrassProfileRow> ProfileRows => profileRows;
        public IReadOnlyList<GrassBladeDefinition> Blades => blades;
        public Mesh OutputMesh => outputMesh;
    }
}
