#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace GemTD.Gameplay.Run
{
    /// <summary>Dev-only OnGUI hint. Not included in release builds.</summary>
    public sealed class RunStateDebugHud : MonoBehaviour
    {
        void OnGUI()
        {
            var root = GameCompositionRoot.Instance;
            if (root == null || root.States == null) return;

            var label = $"State: {root.States.Current}   |  HUD Start Wave / build bar / place   |  F5 debug advance   |  F6 fill bag   |  WASD pan  MMB drag  scroll zoom  Q/E rotate  R targeting  Ctrl+C/V copy-paste";
            GUI.Label(new Rect(12f, 12f, 1100f, 28f), label);
        }
    }
}
#endif
