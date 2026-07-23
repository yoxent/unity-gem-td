using UnityEngine;

namespace GemTD.Gameplay.Run
{
    /// <summary>
    /// Phase 1 debug overlay so state cycling is visible without UI assembly work.
    /// </summary>
    public sealed class RunStateDebugHud : MonoBehaviour
    {
        void OnGUI()
        {
            var root = GameCompositionRoot.Instance;
            if (root == null || root.States == null) return;

            var label = $"State: {root.States.Current}   |  Space / F5 advance   |  WASD pan  MMB drag  scroll zoom  Q/E rotate";
            GUI.Label(new Rect(12f, 12f, 980f, 28f), label);
        }
    }
}
