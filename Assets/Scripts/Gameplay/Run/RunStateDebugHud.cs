using UnityEngine;

namespace GemTD.Gameplay.Run
{
    /// <summary>
    /// Optional OnGUI hint. Space no longer advances phases — use HUD / F5 debug.
    /// </summary>
    public sealed class RunStateDebugHud : MonoBehaviour
    {
        void OnGUI()
        {
            var root = GameCompositionRoot.Instance;
            if (root == null || root.States == null) return;

            var label = $"State: {root.States.Current}   |  HUD Start Wave / click + / place   |  F5 debug advance   |  WASD pan  MMB drag  scroll zoom  Q/E rotate";
            GUI.Label(new Rect(12f, 12f, 1100f, 28f), label);
        }
    }
}
