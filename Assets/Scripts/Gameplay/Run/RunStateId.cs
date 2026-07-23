namespace GemTD.Gameplay.Run
{
    public enum RunStateId
    {
        Boot = 0,
        Plan = 1,
        Expand = Plan,
        Build = 2,
        Combat = 3,
        Draft = 4,
        Boss = 5,
        Endless = 6,
        Defeat = 7,
        VictorySummary = 8
    }
}
