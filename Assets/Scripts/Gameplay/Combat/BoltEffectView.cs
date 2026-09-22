using UnityEngine;

namespace GemTD.Gameplay.Combat
{
    public sealed class BoltEffectView : EffectView
    {
        [SerializeField] ParticleSystem particles;
        [SerializeField] Renderer boltRenderer;
        [SerializeField] Color fireColor = new Color(1f, 0.25f, 0.05f, 1f);
        [SerializeField] Color iceColor = new Color(0.25f, 0.75f, 1f, 1f);
        [SerializeField] Color lightningColor = new Color(1f, 0.9f, 0.15f, 1f);
        [SerializeField] Color waterColor = new Color(0.1f, 0.45f, 1f, 1f);

        MaterialPropertyBlock _propertyBlock;

        protected override ParticleSystem AssignedParticles => particles;

        protected override void OnBind()
        {
            base.OnBind();
            ApplyElementColor();
        }

        protected override void OnClear()
        {
            base.OnClear();
            if (boltRenderer != null)
                boltRenderer.SetPropertyBlock(null);
        }

        void ApplyElementColor()
        {
            if (boltRenderer == null)
                return;

            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();

            var color = ResolveElementColor(BoltElementVisual.Resolve(ResolveHitSpec()));
            _propertyBlock.Clear();
            _propertyBlock.SetColor("_BaseColor", color);
            _propertyBlock.SetColor("_Color", color);
            boltRenderer.SetPropertyBlock(_propertyBlock);
        }

        SkillSpec ResolveHitSpec()
        {
            if (Runtime != null)
                return Runtime.HitSpec;
            if (Payload != null)
                return Payload.Plan.HitSpec;
            return default;
        }

        Color ResolveElementColor(BoltElement element)
        {
            switch (element)
            {
                case BoltElement.Fire:
                    return fireColor;
                case BoltElement.Ice:
                    return iceColor;
                case BoltElement.Lightning:
                    return lightningColor;
                case BoltElement.Water:
                default:
                    return waterColor;
            }
        }
    }
}
