using System;
using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Receives imported FBX animation events on the same GameObject as the character Animator.
    /// TowerAnimatorView filters the source and forwards the execute marker to combat.
    /// </summary>
    public sealed class TowerAnimationEventRelay : MonoBehaviour
    {
        public const string ExecuteAction = "execute";

        public static event Action<TowerAnimationEventRelay, string> ActionRaised;

        /// <summary>
        /// Unity Animation Event entry point. Configure imported clips with:
        /// Function = OnCombatAction, String = execute.
        /// </summary>
        public void OnCombatAction(string action)
        {
            if (string.IsNullOrEmpty(action))
                return;

            ActionRaised?.Invoke(this, action);
        }
    }
}
