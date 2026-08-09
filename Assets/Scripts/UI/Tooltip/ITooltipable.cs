namespace GemTD.UI
{
    /// <summary>Implemented by any GameObject that should show a tooltip on hover.</summary>
    public interface ITooltipable
    {
        string TooltipText { get; }
    }
}