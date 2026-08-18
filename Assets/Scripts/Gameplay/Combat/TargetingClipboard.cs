namespace GemTD.Gameplay.Combat
{
    public sealed class TargetingClipboard
    {
        public bool Has { get; private set; }
        public TargetingRecipe Recipe { get; private set; }

        public void Copy(TargetingRecipe recipe)
        {
            Recipe = recipe;
            Has = true;
        }

        public bool TryGet(out TargetingRecipe recipe)
        {
            recipe = Recipe;
            return Has;
        }

        public void Clear()
        {
            Has = false;
            Recipe = default;
        }
    }
}
