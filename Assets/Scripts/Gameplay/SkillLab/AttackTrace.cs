using System.Collections.Generic;
using UnityEngine;

namespace GemTD.Gameplay.SkillLab
{
    public enum AttackTraceKind
    {
        Primary = 0,
        HydraHead = 1,
        Pierce = 2,
        Fork = 3,
        Chain = 4,
        Aoe = 5
    }

    public struct AttackTraceSegment
    {
        public Vector3 From;
        public Vector3 To;
        public AttackTraceKind Kind;
        public float Damage;
    }

    public struct AttackTraceDisc
    {
        public Vector3 Center;
        public float Radius;
        public AttackTraceKind Kind;
    }

    public sealed class AttackTrace
    {
        public const int MaxSegments = 256;
        public readonly List<AttackTraceSegment> Segments = new List<AttackTraceSegment>(32);
        public readonly List<AttackTraceDisc> Discs = new List<AttackTraceDisc>(8);
        public bool Truncated;
        public bool HasTarget;
    }
}
