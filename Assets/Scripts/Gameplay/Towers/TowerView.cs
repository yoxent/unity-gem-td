using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>Greybox tower mesh bound to a <see cref="TowerInstance"/>.</summary>
    public sealed class TowerView : MonoBehaviour
    {
        static readonly Color IdleColor = new Color(0.45f, 0.5f, 0.55f);
        static readonly Color SelectedColor = new Color(0.95f, 0.75f, 0.25f);

        [SerializeField] TowerAnimatorView animatorView;
        [Tooltip("World Y above pad top where bolts, warp, and caster nova originate.")]
        [SerializeField] [Min(0f)] float muzzleLocalY = DefaultMuzzleLocalY;

        MeshRenderer _renderer;
        MaterialPropertyBlock _block;

        public const float DefaultMuzzleLocalY = 1.2f;
        public const float GroundLift = 0.55f;

        public TowerInstance Runtime { get; private set; }

        public void Bind(TowerInstance runtime, Vector3 worldPosition)
        {
            Runtime = runtime;
            transform.position = worldPosition + Vector3.up * GroundLift;
            if (runtime != null)
                runtime.MuzzleLocalY = muzzleLocalY < 0f ? 0f : muzzleLocalY;
            if (_renderer == null)
                _renderer = GetComponentInChildren<MeshRenderer>();
            SetSelected(false);
            if (animatorView != null)
                animatorView.Bind(runtime);
        }

        public void TickAnimator(float dt, float simSpeed)
        {
            if (animatorView != null)
                animatorView.Tick(dt, simSpeed);
        }

        public void SetSelected(bool selected)
        {
            if (_renderer == null)
                _renderer = GetComponentInChildren<MeshRenderer>();
            if (_renderer == null)
                return;

            _block ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", selected ? SelectedColor : IdleColor);
            _block.SetColor("_Color", selected ? SelectedColor : IdleColor);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
