namespace GemTD.Gameplay.Combat
{
    public sealed class SlamEffectView : EffectView
    {
        public override bool IsSlamEffect => true;
        protected override bool SitsOnGround => true;
    }
}
