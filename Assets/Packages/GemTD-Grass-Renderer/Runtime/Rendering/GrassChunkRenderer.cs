using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GemTD.Grass
{
    public class GrassChunkRenderer : MonoBehaviour
    {
        const int MaxInstancesPerBatch = 1023;

        static readonly int GrassRootColorId = Shader.PropertyToID("_GrassRootColor");
        static readonly int GrassBodyColorId = Shader.PropertyToID("_GrassBodyColor");
        static readonly int GrassTipColorId = Shader.PropertyToID("_GrassTipColor");
        static readonly int GrassColorNoiseScaleId = Shader.PropertyToID("_GrassColorNoiseScale");
        static readonly int GrassColorNoiseStrengthId = Shader.PropertyToID("_GrassColorNoiseStrength");
        static readonly int GrassWindDirectionId = Shader.PropertyToID("_GrassWindDirection");
        static readonly int GrassWindScaleId = Shader.PropertyToID("_GrassWindScale");
        static readonly int GrassWindSpeedId = Shader.PropertyToID("_GrassWindSpeed");
        static readonly int GrassWindStrengthId = Shader.PropertyToID("_GrassWindStrength");
        static readonly int GrassReceiveShadowsId = Shader.PropertyToID("_GrassReceiveShadows");

        sealed class VariantGroup
        {
            public readonly Mesh Mesh;
            public readonly Matrix4x4[] Matrices;
            public readonly MaterialPropertyBlock Properties;
            public readonly int Count;
            public RenderParams RenderParams;

            public VariantGroup(
                Mesh mesh,
                int count,
                MaterialPropertyBlock properties,
                RenderParams renderParams)
            {
                Mesh = mesh;
                Matrices = new Matrix4x4[count];
                Properties = properties;
                Count = count;
                RenderParams = renderParams;
            }
        }

        VariantGroup[] _groups;
        int _instanceCount;
        int _drawGroupCount;
        int _drawBatchCount;

        bool _warnedMissingStyle;
        bool _warnedMissingMaterial;
        bool _warnedNoValidMeshes;
        bool _warnedEmptyInstances;
        bool _warnedInvalidVariant;
        bool _warnedNullMesh;
        bool _warnedInvalidInstance;

        public int InstanceCount => _instanceCount;
        public int DrawGroupCount => _drawGroupCount;
        public int DrawBatchCount => _drawBatchCount;

        public void Bind(
            GrassStyleDefinition style,
            IReadOnlyList<GrassInstance> instances,
            Bounds localBounds)
        {
            if (style == null)
            {
                Clear();
                WarnOnce(ref _warnedMissingStyle, "GrassChunkRenderer requires a GrassStyleDefinition before binding.");
                return;
            }

            if (style.Material == null)
            {
                Clear();
                WarnOnce(ref _warnedMissingMaterial, "GrassChunkRenderer style is missing its grass material.");
                return;
            }

            var variantCount = style.VariantCount;
            var validMeshCount = 0;
            var hasNullMesh = false;
            var maxMeshHorizontalRadius = 0f;
            var maxMeshVerticalExtent = 0f;
            for (var variantIndex = 0; variantIndex < variantCount; variantIndex++)
            {
                var mesh = style.GetClumpMesh(variantIndex);
                if (mesh == null)
                {
                    hasNullMesh = true;
                    continue;
                }

                validMeshCount++;
                var meshBounds = mesh.bounds;
                var meshCenter = meshBounds.center;
                var meshExtents = meshBounds.extents;
                var meshMin = meshBounds.min;
                var meshMax = meshBounds.max;
                var meshCenterRadius = Mathf.Sqrt(
                    meshCenter.x * meshCenter.x +
                    meshCenter.z * meshCenter.z);
                var meshExtentsRadius = Mathf.Sqrt(
                    meshExtents.x * meshExtents.x +
                    meshExtents.z * meshExtents.z);
                maxMeshHorizontalRadius = Mathf.Max(
                    maxMeshHorizontalRadius,
                    meshCenterRadius + meshExtentsRadius);
                maxMeshVerticalExtent = Mathf.Max(
                    maxMeshVerticalExtent,
                    Mathf.Abs(meshMin.y),
                    Mathf.Abs(meshMax.y));
            }

            if (validMeshCount == 0)
            {
                Clear();
                WarnOnce(ref _warnedNoValidMeshes, "GrassChunkRenderer style has no valid clump meshes.");
                return;
            }

            if (hasNullMesh)
                WarnOnce(ref _warnedNullMesh, "GrassChunkRenderer style contains a null grass mesh variant.");

            if (instances == null || instances.Count == 0)
            {
                Clear();
                WarnOnce(ref _warnedEmptyInstances, "GrassChunkRenderer received no grass instances to render.");
                return;
            }

            var instanceCounts = new int[variantCount];
            var totalInstanceCount = 0;
            var maxInstanceScale = 0f;
            for (var instanceIndex = 0; instanceIndex < instances.Count; instanceIndex++)
            {
                var instance = instances[instanceIndex];
                if (!IsFinite(instance))
                {
                    WarnOnce(ref _warnedInvalidInstance, "GrassChunkRenderer skipped a grass instance with non-finite transform data.");
                    continue;
                }

                var variantIndex = instance.VariantIndex;
                if (variantIndex < 0 || variantIndex >= variantCount)
                {
                    WarnOnce(ref _warnedInvalidVariant, "GrassChunkRenderer skipped an instance with an invalid grass mesh variant.");
                    continue;
                }

                if (style.GetClumpMesh(variantIndex) == null)
                {
                    WarnOnce(ref _warnedNullMesh, "GrassChunkRenderer skipped an instance whose grass mesh variant is null.");
                    continue;
                }

                instanceCounts[variantIndex]++;
                totalInstanceCount++;
                maxInstanceScale = Mathf.Max(maxInstanceScale, Mathf.Abs(instance.UniformScale));
            }

            if (totalInstanceCount == 0)
            {
                Clear();
                return;
            }

            var localBoundsWithMesh = localBounds;
            var meshHorizontalExpansion = maxMeshHorizontalRadius * maxInstanceScale;
            var meshVerticalExpansion = maxMeshVerticalExtent * maxInstanceScale;
            localBoundsWithMesh.Expand(new Vector3(
                meshHorizontalExpansion * 2f,
                meshVerticalExpansion * 2f,
                meshHorizontalExpansion * 2f));

            var localToWorld = transform.localToWorldMatrix;
            var worldBounds = TransformBounds(localBoundsWithMesh, localToWorld);
            var windExpansion = IsFinite(style.WindStrength) ? Mathf.Max(0f, style.WindStrength) : 0f;
            worldBounds.Expand(new Vector3(windExpansion * 2f, 0f, windExpansion * 2f));

            var groupsByVariant = new VariantGroup[variantCount];
            var groupCount = 0;
            var batchCount = 0;
            for (var variantIndex = 0; variantIndex < variantCount; variantIndex++)
            {
                var count = instanceCounts[variantIndex];
                if (count == 0)
                    continue;

                var properties = new MaterialPropertyBlock();
                SetStyleProperties(properties, style);
                var renderParams = new RenderParams(style.Material)
                {
                    worldBounds = worldBounds,
                    matProps = properties,
                    shadowCastingMode = style.ShadowCasting,
                    receiveShadows = style.ReceiveShadows,
                    layer = gameObject.layer
                };
                groupsByVariant[variantIndex] = new VariantGroup(
                    style.GetClumpMesh(variantIndex),
                    count,
                    properties,
                    renderParams);
                groupCount++;
                batchCount += (count + MaxInstancesPerBatch - 1) / MaxInstancesPerBatch;
            }

            var replacementGroups = new VariantGroup[groupCount];
            var replacementIndex = 0;
            for (var variantIndex = 0; variantIndex < groupsByVariant.Length; variantIndex++)
            {
                var group = groupsByVariant[variantIndex];
                if (group == null)
                    continue;

                replacementGroups[replacementIndex++] = group;
            }

            var writeIndices = new int[variantCount];
            for (var instanceIndex = 0; instanceIndex < instances.Count; instanceIndex++)
            {
                var instance = instances[instanceIndex];
                if (!IsFinite(instance) ||
                    instance.VariantIndex < 0 ||
                    instance.VariantIndex >= variantCount)
                {
                    continue;
                }

                var group = groupsByVariant[instance.VariantIndex];
                if (group == null)
                    continue;

                var matrixIndex = writeIndices[instance.VariantIndex]++;
                group.Matrices[matrixIndex] = localToWorld * Matrix4x4.TRS(
                    instance.LocalPosition,
                    Quaternion.Euler(0f, instance.YawDegrees, 0f),
                    Vector3.one * instance.UniformScale);
            }

            _groups = replacementGroups;
            _instanceCount = totalInstanceCount;
            _drawGroupCount = groupCount;
            _drawBatchCount = batchCount;
        }

        public void Clear()
        {
            _groups = null;
            _instanceCount = 0;
            _drawGroupCount = 0;
            _drawBatchCount = 0;
        }

        void LateUpdate()
        {
            if (_groups == null)
                return;

            for (var groupIndex = 0; groupIndex < _groups.Length; groupIndex++)
            {
                var group = _groups[groupIndex];
                for (var startInstance = 0; startInstance < group.Count; startInstance += MaxInstancesPerBatch)
                {
                    var instanceCount = Mathf.Min(MaxInstancesPerBatch, group.Count - startInstance);
                    Graphics.RenderMeshInstanced(
                        group.RenderParams,
                        group.Mesh,
                        0,
                        group.Matrices,
                        instanceCount,
                        startInstance);
                }
            }
        }

        void OnDisable()
        {
            Clear();
        }

        void OnDestroy()
        {
            Clear();
        }

        static void SetStyleProperties(MaterialPropertyBlock properties, GrassStyleDefinition style)
        {
            properties.SetColor(GrassRootColorId, style.RootColor);
            properties.SetColor(GrassBodyColorId, style.BodyColor);
            properties.SetColor(GrassTipColorId, style.TipColor);
            properties.SetFloat(GrassColorNoiseScaleId, style.ColorNoiseScale);
            properties.SetFloat(GrassColorNoiseStrengthId, style.ColorNoiseStrength);
            properties.SetVector(GrassWindDirectionId, new Vector4(
                style.WindDirection.x,
                style.WindDirection.y,
                0f,
                0f));
            properties.SetFloat(GrassWindScaleId, style.WindScale);
            properties.SetFloat(GrassWindSpeedId, style.WindSpeed);
            properties.SetFloat(GrassWindStrengthId, style.WindStrength);
            properties.SetFloat(GrassReceiveShadowsId, style.ReceiveShadows ? 1f : 0f);
        }

        void WarnOnce(ref bool hasWarned, string message)
        {
            if (hasWarned)
                return;

            hasWarned = true;
            Debug.LogWarning(message, this);
        }

        static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            var center = matrix.MultiplyPoint3x4(bounds.center);
            var extents = bounds.extents;
            var worldExtents = new Vector3(
                Mathf.Abs(matrix.m00) * extents.x +
                Mathf.Abs(matrix.m01) * extents.y +
                Mathf.Abs(matrix.m02) * extents.z,
                Mathf.Abs(matrix.m10) * extents.x +
                Mathf.Abs(matrix.m11) * extents.y +
                Mathf.Abs(matrix.m12) * extents.z,
                Mathf.Abs(matrix.m20) * extents.x +
                Mathf.Abs(matrix.m21) * extents.y +
                Mathf.Abs(matrix.m22) * extents.z);
            return new Bounds(center, worldExtents * 2f);
        }

        static bool IsFinite(GrassInstance instance)
        {
            return IsFinite(instance.LocalPosition.x) &&
                   IsFinite(instance.LocalPosition.y) &&
                   IsFinite(instance.LocalPosition.z) &&
                   IsFinite(instance.YawDegrees) &&
                   IsFinite(instance.UniformScale);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
