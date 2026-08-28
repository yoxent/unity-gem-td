using UnityEngine;

namespace GemTD.Gameplay.SkillLab
{
    public sealed class AttackOverlayView : MonoBehaviour
    {
        [SerializeField] Material lineMaterial;

        AttackTrace _trace;
        Vector3 _rangeCenter;
        float _range;

        public static Color ColorFor(AttackTraceKind kind)
        {
            switch (kind)
            {
                case AttackTraceKind.Primary: return Color.white;
                case AttackTraceKind.HydraHead: return Color.cyan;
                case AttackTraceKind.Pierce: return Color.yellow;
                case AttackTraceKind.Fork: return Color.magenta;
                case AttackTraceKind.Chain: return new Color(1f, 0.5f, 0f);
                case AttackTraceKind.Aoe: return Color.red;
                case AttackTraceKind.WarpRise: return Color.cyan;
                case AttackTraceKind.WarpDrop: return new Color(1f, 0.8f, 0.1f);
                case AttackTraceKind.Magma: return new Color(1f, 0.2f, 0.05f);
                default: return Color.white;
            }
        }

        public void SetTrace(AttackTrace trace)
        {
            _trace = trace;
        }

        public void SetRangeRing(Vector3 center, float radius)
        {
            _rangeCenter = center;
            _range = radius;
        }

        void LateUpdate()
        {
            if (_range > 0.01f)
                DrawCircleDebug(_rangeCenter, _range, new Color(0.2f, 1f, 0.2f), 48);

            if (_trace == null)
                return;

            for (var i = 0; i < _trace.Segments.Count; i++)
            {
                var s = _trace.Segments[i];
                var c = ColorFor(s.Kind);
                Debug.DrawLine(s.From + Vector3.up * 0.05f, s.To + Vector3.up * 0.05f, c, 0f, false);
            }

            for (var i = 0; i < _trace.Discs.Count; i++)
            {
                var d = _trace.Discs[i];
                DrawCircleDebug(d.Center, d.Radius, ColorFor(d.Kind), 32);
            }
        }

        void OnRenderObject()
        {
            if (!EnsureMaterial())
                return;

            lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);

            if (_range > 0.01f)
                DrawCircleGl(_rangeCenter, _range, new Color(0.2f, 1f, 0.2f), 48);

            if (_trace != null)
            {
                for (var i = 0; i < _trace.Segments.Count; i++)
                {
                    var s = _trace.Segments[i];
                    DrawLineGl(s.From, s.To, ColorFor(s.Kind));
                }

                for (var i = 0; i < _trace.Discs.Count; i++)
                {
                    var d = _trace.Discs[i];
                    DrawCircleGl(d.Center, d.Radius, ColorFor(d.Kind), 32);
                }
            }

            GL.PopMatrix();
        }

        bool EnsureMaterial()
        {
            if (lineMaterial != null)
                return true;

            var shader = Shader.Find("Hidden/Internal-Colored")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                return false;

            lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return true;
        }

        static void DrawLineGl(Vector3 a, Vector3 b, Color color)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);
            GL.Vertex(a + Vector3.up * 0.05f);
            GL.Vertex(b + Vector3.up * 0.05f);
            GL.End();
        }

        static void DrawCircleGl(Vector3 center, float radius, Color color, int steps)
        {
            GL.Begin(GL.LINE_STRIP);
            GL.Color(color);
            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps * Mathf.PI * 2f;
                GL.Vertex(center + new Vector3(Mathf.Cos(t) * radius, 0.05f, Mathf.Sin(t) * radius));
            }

            GL.End();
        }

        static void DrawCircleDebug(Vector3 center, float radius, Color color, int steps)
        {
            var prev = center + new Vector3(radius, 0.05f, 0f);
            for (var i = 1; i <= steps; i++)
            {
                var t = i / (float)steps * Mathf.PI * 2f;
                var next = center + new Vector3(Mathf.Cos(t) * radius, 0.05f, Mathf.Sin(t) * radius);
                Debug.DrawLine(prev, next, color, 0f, false);
                prev = next;
            }
        }
    }
}
