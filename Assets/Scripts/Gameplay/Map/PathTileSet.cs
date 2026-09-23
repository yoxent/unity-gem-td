using System;
using UnityEngine;

namespace GemTD.Gameplay.Map
{
    [Serializable]
    public sealed class PathTilePiece
    {
        public GameObject Prefab;
        public EdgeFlags OpenEdges;
    }

    /// <summary>
    /// One swappable path kit. OpenEdges are the edges open at identity rotation.
    /// An editor apply writes the pieces into chunk prefabs.
    /// </summary>
    [CreateAssetMenu(menuName = "Gem TD/Path Tile Set")]
    public sealed class PathTileSet : ScriptableObject
    {
        [SerializeField] ChunkCatalog catalog;
        [SerializeField, Min(0.01f)] float uniformScale = 1f;
        [SerializeField] PathTilePiece bend;
        [SerializeField] PathTilePiece cross;
        [SerializeField] PathTilePiece end;
        [SerializeField] PathTilePiece split;
        [SerializeField] PathTilePiece straight;
        [Header("Tower pads — height 1, 2, 3")]
        [SerializeField] GameObject padHeight1;
        [SerializeField] GameObject padHeight2;
        [SerializeField] GameObject padHeight3;

        public ChunkCatalog Catalog => catalog;
        public float UniformScale => uniformScale < 0.01f ? 0.01f : uniformScale;

        public bool TryResolve(EdgeFlags required, out GameObject prefab, out int yaw)
        {
            prefab = null;
            yaw = 0;
            var openings = new EdgeFlags[5];
            FillOpenings(openings);
            if (!PathTileOrientation.TryResolve(required, openings, out var index, out yaw))
                return false;

            var piece = GetPiece(index);
            if (piece == null || piece.Prefab == null)
                return false;

            prefab = piece.Prefab;
            return true;
        }

        public void FillOpenings(EdgeFlags[] into)
        {
            if (into == null || into.Length < 5)
                return;
            into[0] = OpenOf(bend);
            into[1] = OpenOf(cross);
            into[2] = OpenOf(end);
            into[3] = OpenOf(split);
            into[4] = OpenOf(straight);
        }

        public GameObject PadPrefab(int height)
        {
            switch (height)
            {
                case 1: return padHeight1;
                case 2: return padHeight2;
                case 3: return padHeight3;
                default: return null;
            }
        }

        public PathTilePiece GetPiece(int index)
        {
            switch (index)
            {
                case 0: return bend;
                case 1: return cross;
                case 2: return end;
                case 3: return split;
                case 4: return straight;
                default: return null;
            }
        }

        static EdgeFlags OpenOf(PathTilePiece piece) =>
            piece != null ? piece.OpenEdges : EdgeFlags.None;

        void OnEnable() => EnsurePieces();

        void Reset()
        {
            uniformScale = 1f;
            bend = Piece(EdgeFlags.South | EdgeFlags.East);
            cross = Piece(EdgeFlags.North | EdgeFlags.East | EdgeFlags.South | EdgeFlags.West);
            end = Piece(EdgeFlags.South);
            split = Piece(EdgeFlags.West | EdgeFlags.East | EdgeFlags.South);
            straight = Piece(EdgeFlags.North | EdgeFlags.South);
        }

        void EnsurePieces()
        {
            if (bend == null) bend = Piece(EdgeFlags.South | EdgeFlags.East);
            if (cross == null) cross = Piece(EdgeFlags.North | EdgeFlags.East | EdgeFlags.South | EdgeFlags.West);
            if (end == null) end = Piece(EdgeFlags.South);
            if (split == null) split = Piece(EdgeFlags.West | EdgeFlags.East | EdgeFlags.South);
            if (straight == null) straight = Piece(EdgeFlags.North | EdgeFlags.South);
        }

        static PathTilePiece Piece(EdgeFlags open) => new PathTilePiece { OpenEdges = open };
    }
}
