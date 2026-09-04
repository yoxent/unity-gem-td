using System;
using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>Tower mesh bound to a <see cref="TowerInstance"/>. Pivot is snapped so the structure sits on the pad.</summary>
    public sealed class TowerView : MonoBehaviour
    {
        [SerializeField] TowerAnimatorView animatorView;
        [Tooltip("World Y above pad top where bolts, warp, and caster nova originate.")]
        [SerializeField] [Min(0f)] float muzzleLocalY = DefaultMuzzleLocalY;

        public const float DefaultMuzzleLocalY = 1.2f;

        public TowerInstance Runtime { get; private set; }

        public void Bind(TowerInstance runtime, Vector3 worldPosition)
        {
            Runtime = runtime;
            PlaceOnPad(worldPosition);
            if (runtime != null)
                runtime.MuzzleLocalY = muzzleLocalY < 0f ? 0f : muzzleLocalY;
            if (animatorView != null)
            {
                TowerPadSnap.UniformizeLocalScale(animatorView.OccupantRoot);
                animatorView.Bind(runtime);
            }
        }

        public void PlaceOnPad(Vector3 padTop)
        {
            TowerPadSnap.SitOnWorldPad(transform, padTop);
        }

        public void TickAnimator(float dt, float simSpeed)
        {
            if (animatorView != null)
                animatorView.Tick(dt, simSpeed);
        }

        public void SetCombatActionHandler(Action<TowerInstance, int, string> handler)
        {
            animatorView?.SetCombatActionHandler(handler);
        }

        /// <summary>Selection is the range cylinder, not an albedo swap — keep the authored tower material.</summary>
        public void SetSelected(bool selected)
        {
            _ = selected;
        }
    }
}
