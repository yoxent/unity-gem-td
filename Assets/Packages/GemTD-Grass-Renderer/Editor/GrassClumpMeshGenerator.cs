using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GemTD.Grass;

namespace GemTD.Grass.Editor
{
    public readonly struct GrassBladeRecipe
    {
        public Vector3 BasePosition { get; }
        public float YawDegrees { get; }
        public float Height { get; }
        public float Width { get; }
        public float ForwardBend { get; }
        public float SideBend { get; }

        public GrassBladeRecipe(
            Vector3 basePosition,
            float yawDegrees,
            float height,
            float width,
            float forwardBend,
            float sideBend)
        {
            BasePosition = basePosition;
            YawDegrees = yawDegrees;
            Height = height;
            Width = width;
            ForwardBend = forwardBend;
            SideBend = sideBend;
        }
    }

    public sealed class GrassClumpRecipe
    {
        public string Name { get; }
        public IReadOnlyList<GrassProfileRow> ProfileRows { get; }
        public IReadOnlyList<GrassBladeRecipe> Blades { get; }

        public GrassClumpRecipe(string name, params GrassBladeRecipe[] blades)
            : this(name, GrassClumpMeshGenerator.ApprovedProfileRows, blades)
        {
        }

        public GrassClumpRecipe(string name, IReadOnlyList<GrassBladeRecipe> blades)
            : this(name, GrassClumpMeshGenerator.ApprovedProfileRows, blades)
        {
        }

        public GrassClumpRecipe(
            string name,
            IReadOnlyList<GrassProfileRow> profileRows,
            IReadOnlyList<GrassBladeRecipe> blades)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "GrassClump" : name;
            ProfileRows = profileRows ?? throw new ArgumentNullException(nameof(profileRows));
            Blades = blades ?? throw new ArgumentNullException(nameof(blades));
        }
    }

    public static class GrassClumpMeshGenerator
    {
        static readonly GrassProfileRow[] ApprovedProfile =
        {
            new GrassProfileRow(0f, 1f),
            new GrassProfileRow(0.2f, 0.94f),
            new GrassProfileRow(0.4f, 0.78f),
            new GrassProfileRow(0.62f, 0.56f),
            new GrassProfileRow(0.82f, 0.30f),
            new GrassProfileRow(1f, 0f)
        };

        public static GrassProfileRow[] ApprovedProfileRows =>
            (GrassProfileRow[])ApprovedProfile.Clone();

        public static Mesh CreateClump(GrassClumpRecipe recipe)
        {
            ValidateRecipe(recipe);
            return CreateMesh(recipe.Name, recipe.ProfileRows, ConvertBlades(recipe.Blades));
        }

        public static Mesh CreateClump(GrassClumpDefinition definition)
        {
            ValidateDefinition(definition);
            return CreateMesh(definition.name, definition.ProfileRows, definition.Blades);
        }

        public static void BakeDefinition(GrassClumpDefinition definition)
        {
            ValidateDefinition(definition);
            if (definition.OutputMesh == null)
                throw new InvalidOperationException(
                    $"Grass clump definition '{definition.name}' has no output Mesh.");

            Undo.RegisterCompleteObjectUndo(definition.OutputMesh, "Bake Grass Clump Mesh");
            WriteMesh(definition.OutputMesh, definition.ProfileRows, definition.Blades);
            EditorUtility.SetDirty(definition.OutputMesh);
        }

        public static bool TryValidate(GrassClumpDefinition definition, out string error)
        {
            try
            {
                ValidateDefinition(definition);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        static Mesh CreateMesh(
            string meshName,
            IReadOnlyList<GrassProfileRow> profileRows,
            IReadOnlyList<GrassBladeDefinition> blades)
        {
            var mesh = new Mesh { name = meshName };
            WriteMesh(mesh, profileRows, blades);
            return mesh;
        }

        static void WriteMesh(
            Mesh mesh,
            IReadOnlyList<GrassProfileRow> profileRows,
            IReadOnlyList<GrassBladeDefinition> blades)
        {
            var bladeCount = blades.Count;
            var rowCount = profileRows.Count;
            var verticesPerBlade = rowCount * 2;
            var indicesPerBlade = (rowCount - 1) * 6;
            var vertices = new Vector3[bladeCount * verticesPerBlade];
            var normals = new Vector3[bladeCount * verticesPerBlade];
            var uvs = new Vector2[bladeCount * verticesPerBlade];
            var triangles = new int[bladeCount * indicesPerBlade];

            for (var bladeIndex = 0; bladeIndex < bladeCount; bladeIndex++)
                WriteBlade(blades[bladeIndex], profileRows, bladeIndex, vertices, normals, uvs, triangles);

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
        }

        public static GrassClumpRecipe CreateDefaultA()
        {
            return new GrassClumpRecipe(
                "GrassClump_A",
                new GrassBladeRecipe(new Vector3(-0.166f, 0f, -0.08f), 338f, 0.44f, 0.102f, 0.13f, -0.045f),
                new GrassBladeRecipe(new Vector3(-0.05f, 0f, 0.096f), 24f, 0.48f, 0.09f, 0.16f, -0.02f),
                new GrassBladeRecipe(new Vector3(0.02f, 0f, -0.01f), 92f, 0.46f, 0.108f, 0.11f, 0.035f),
                new GrassBladeRecipe(new Vector3(0.14f, 0f, 0.08f), 176f, 0.38f, 0.084f, 0.07f, 0.05f),
                new GrassBladeRecipe(new Vector3(0.09f, 0f, -0.14f), 262f, 0.41f, 0.096f, 0.09f, -0.03f),
                new GrassBladeRecipe(new Vector3(-0.15f, 0f, 0.08f), 54f, 0.42f, 0.09f, 0.1f, 0.025f),
                new GrassBladeRecipe(new Vector3(-0.07f, 0f, -0.07f), 218f, 0.40f, 0.086f, 0.08f, -0.04f));
        }

        public static GrassClumpRecipe CreateDefaultB()
        {
            return new GrassClumpRecipe(
                "GrassClump_B",
                new GrassBladeRecipe(new Vector3(-0.21f, 0f, -0.06f), 350f, 0.28f, 0.072f, 0.05f, -0.04f),
                new GrassBladeRecipe(new Vector3(-0.12f, 0f, 0.17f), 28f, 0.33f, 0.084f, 0.08f, -0.015f),
                new GrassBladeRecipe(new Vector3(-0.01f, 0f, -0.16f), 74f, 0.40f, 0.096f, 0.11f, 0.025f),
                new GrassBladeRecipe(new Vector3(0.04f, 0f, 0.03f), 128f, 0.36f, 0.084f, 0.09f, 0.05f),
                new GrassBladeRecipe(new Vector3(0.13f, 0f, 0.18f), 196f, 0.31f, 0.078f, 0.07f, 0.03f),
                new GrassBladeRecipe(new Vector3(0.22f, 0f, -0.02f), 252f, 0.37f, 0.09f, 0.08f, -0.045f),
                new GrassBladeRecipe(new Vector3(0.1f, 0f, -0.2f), 306f, 0.34f, 0.084f, 0.06f, -0.02f),
                new GrassBladeRecipe(new Vector3(-0.08f, 0f, 0.01f), 164f, 0.38f, 0.088f, 0.1f, 0.04f),
                new GrassBladeRecipe(new Vector3(0.11f, 0f, -0.09f), 320f, 0.30f, 0.08f, 0.055f, -0.035f));
        }

        static void ValidateRecipe(GrassClumpRecipe recipe)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            ValidateProfile(recipe.ProfileRows);
            ValidateBlades(recipe.Blades);
        }

        static void ValidateDefinition(GrassClumpDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            ValidateProfile(definition.ProfileRows);
            ValidateBlades(definition.Blades);
        }

        static void ValidateProfile(IReadOnlyList<GrassProfileRow> profileRows)
        {
            if (profileRows == null || profileRows.Count < 2)
                throw new ArgumentException("Grass blade profile must contain at least two rows.", nameof(profileRows));

            for (var i = 0; i < profileRows.Count; i++)
            {
                var row = profileRows[i];
                ValidateFinite(row.NormalizedHeight, $"Profile row {i} height");
                ValidateFinite(row.WidthFactor, $"Profile row {i} width factor");

                if (row.WidthFactor < 0f)
                    throw new ArgumentOutOfRangeException(
                        nameof(profileRows),
                        row.WidthFactor,
                        $"Grass profile row {i} width factor must be nonnegative.");

                if (i > 0 && row.NormalizedHeight <= profileRows[i - 1].NormalizedHeight)
                    throw new ArgumentException(
                        $"Grass profile row heights must be strictly increasing (row {i}).",
                        nameof(profileRows));
            }

            if (profileRows[0].NormalizedHeight != 0f)
                throw new ArgumentException("Grass profile first height must be 0.", nameof(profileRows));

            if (profileRows[profileRows.Count - 1].NormalizedHeight != 1f)
                throw new ArgumentException("Grass profile last height must be 1.", nameof(profileRows));
        }

        static void ValidateBlades(IReadOnlyList<GrassBladeRecipe> blades)
        {
            if (blades == null || blades.Count == 0)
                throw new ArgumentException("Grass clump recipe must contain at least one blade.", nameof(blades));

            for (var i = 0; i < blades.Count; i++)
                ValidateBlade(
                    blades[i].BasePosition,
                    blades[i].YawDegrees,
                    blades[i].Height,
                    blades[i].Width,
                    blades[i].ForwardBend,
                    blades[i].SideBend);
        }

        static void ValidateBlades(IReadOnlyList<GrassBladeDefinition> blades)
        {
            if (blades == null || blades.Count == 0)
                throw new ArgumentException("Grass clump definition must contain at least one blade.", nameof(blades));

            for (var i = 0; i < blades.Count; i++)
            {
                var blade = blades[i];
                ValidateBlade(
                    blade.BasePosition,
                    blade.YawDegrees,
                    blade.Height,
                    blade.Width,
                    blade.ForwardBend,
                    blade.SideBend);
            }
        }

        static void ValidateBlade(
            Vector3 basePosition,
            float yawDegrees,
            float height,
            float width,
            float forwardBend,
            float sideBend)
        {
            ValidateFinite(basePosition.x, "BasePosition.x");
            ValidateFinite(basePosition.y, "BasePosition.y");
            ValidateFinite(basePosition.z, "BasePosition.z");
            ValidateFinite(yawDegrees, "YawDegrees");
            ValidateFinite(height, "Height");
            ValidateFinite(width, "Width");
            ValidateFinite(forwardBend, "ForwardBend");
            ValidateFinite(sideBend, "SideBend");

            if (height <= 0f)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Grass blade Height must be greater than zero.");

            if (width <= 0f)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Grass blade Width must be greater than zero.");
        }

        static void ValidateFinite(float value, string fieldName)
        {
            if (!IsFinite(value))
                throw new ArgumentException($"Grass blade {fieldName} must be finite.", fieldName);
        }

        static void WriteBlade(
            GrassBladeDefinition blade,
            IReadOnlyList<GrassProfileRow> profileRows,
            int bladeIndex,
            Vector3[] vertices,
            Vector3[] normals,
            Vector2[] uvs,
            int[] triangles)
        {
            var rotation = Quaternion.Euler(0f, blade.YawDegrees, 0f);
            var rightAxis = rotation * Vector3.right;
            var forwardAxis = rotation * Vector3.forward;
            var rowCount = profileRows.Count;
            var centers = new Vector3[rowCount];

            for (var row = 0; row < rowCount; row++)
            {
                var height01 = profileRows[row].NormalizedHeight;
                var curveWeight = height01 * height01;
                centers[row] = blade.BasePosition +
                               Vector3.up * (blade.Height * height01) +
                               forwardAxis * (blade.ForwardBend * curveWeight) +
                               rightAxis * (blade.SideBend * curveWeight);
            }

            for (var row = 0; row < rowCount; row++)
            {
                var halfWidth = blade.Width * profileRows[row].WidthFactor * 0.5f;
                var center = centers[row];
                var tangent = CalculateCenterTangent(centers, row);
                var normal = Vector3.Cross(tangent, rightAxis);
                if (normal.sqrMagnitude <= 0.000001f)
                    normal = Vector3.Cross(Vector3.up, rightAxis);
                normal.Normalize();

                var vertexOffset = bladeIndex * rowCount * 2 + (row * 2);
                vertices[vertexOffset] = center - (rightAxis * halfWidth);
                vertices[vertexOffset + 1] = center + (rightAxis * halfWidth);
                normals[vertexOffset] = normal;
                normals[vertexOffset + 1] = normal;
                uvs[vertexOffset] = new Vector2(0f, profileRows[row].NormalizedHeight);
                uvs[vertexOffset + 1] = new Vector2(1f, profileRows[row].NormalizedHeight);
            }

            var triangleOffset = bladeIndex * (rowCount - 1) * 6;
            for (var segment = 0; segment < rowCount - 1; segment++)
            {
                var rowOffset = bladeIndex * rowCount * 2 + (segment * 2);
                triangles[triangleOffset++] = rowOffset;
                triangles[triangleOffset++] = rowOffset + 2;
                triangles[triangleOffset++] = rowOffset + 1;
                triangles[triangleOffset++] = rowOffset + 1;
                triangles[triangleOffset++] = rowOffset + 2;
                triangles[triangleOffset++] = rowOffset + 3;
            }
        }

        static Vector3 CalculateCenterTangent(Vector3[] centers, int row)
        {
            if (row == 0)
                return centers[1] - centers[0];

            if (row == centers.Length - 1)
                return centers[row] - centers[row - 1];

            return centers[row + 1] - centers[row - 1];
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        static GrassBladeDefinition[] ConvertBlades(IReadOnlyList<GrassBladeRecipe> blades)
        {
            var converted = new GrassBladeDefinition[blades.Count];
            for (var i = 0; i < blades.Count; i++)
            {
                var blade = blades[i];
                converted[i] = new GrassBladeDefinition(
                    blade.BasePosition,
                    blade.YawDegrees,
                    blade.Height,
                    blade.Width,
                    blade.ForwardBend,
                    blade.SideBend);
            }

            return converted;
        }
    }
}
