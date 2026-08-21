namespace GemTD.Gameplay.Combat
{
    public static class TargetingScopeRequests
    {
        public static TargetingApplyScope Next(TargetingApplyScope current) =>
            (TargetingApplyScope)(((int)current + 1) % 3);

        public static bool NeedsAllConfirm(TargetingApplyScope current, TargetingApplyScope requested) =>
            requested == TargetingApplyScope.AllTowers && current != TargetingApplyScope.AllTowers;
    }
}
