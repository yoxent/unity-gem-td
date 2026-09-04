namespace GemTD.Gameplay.Combat
{
    public sealed class AftershockEffectView : EffectView
    {
        public override bool IsAftershockEffect => true;
        protected override bool SitsOnGround => true;
    }
}
