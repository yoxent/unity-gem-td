using UnityEngine;

namespace GemTD.Gameplay.Towers
{
    /// <summary>Greybox tower mesh bound to a <see cref="TowerRuntime"/>.</summary>
    public sealed class TowerView : MonoBehaviour
    {
        static readonly Color IdleColor = new Color(0.45f, 0.5f, 0.55f);
        static readonly Color SelectedColor = new Color(0.95f, 0.75f, 0.25f);

        MeshRenderer _renderer;
        MaterialPropertyBlock _block;

        public TowerRuntime Runtime { get; private set; }

        public void Bind(TowerRuntime runtime, Vector3 worldPosition)
        {
            Runtime = runtime;
            transform.position = worldPosition + Vector3.up * 0.55f;
            if (_renderer == null)
                _renderer = GetComponentInChildren<MeshRenderer>();
            SetSelected(false);
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
