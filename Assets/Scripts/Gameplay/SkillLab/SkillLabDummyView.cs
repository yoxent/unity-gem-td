using UnityEngine;

namespace GemTD.Gameplay.SkillLab
{
    public sealed class SkillLabDummyView : MonoBehaviour
    {
        static readonly Color HitColor = new Color(1f, 0.2f, 0.05f);
        const float HitFlashSeconds = 0.35f;

        [SerializeField] int index;

        MeshRenderer _renderer;
        MaterialPropertyBlock _block;
        float _hitFlashRemaining;

        public int Index => index;

        public void SetIndex(int value)
        {
            index = value;
        }

        public void FlashHit()
        {
            if (_renderer == null)
                _renderer = GetComponent<MeshRenderer>();
            if (_renderer == null)
                return;

            _block ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", HitColor);
            _block.SetColor("_Color", HitColor);
            _renderer.SetPropertyBlock(_block);
            _hitFlashRemaining = HitFlashSeconds;
        }

        public void ClearHitFlash()
        {
            _hitFlashRemaining = 0f;
            if (_renderer != null)
                _renderer.SetPropertyBlock(null);
        }

        void Update()
        {
            if (_hitFlashRemaining <= 0f)
                return;

            _hitFlashRemaining -= Time.unscaledDeltaTime;
            if (_hitFlashRemaining <= 0f)
                ClearHitFlash();
        }
    }
}
