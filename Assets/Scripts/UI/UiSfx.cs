using GemTD.Core;

namespace GemTD.UI
{
    static class UiSfx
    {
        public static void Click() => GameEvents.RaisePlaySfx(SfxKeys.Click);

        public static void Close() => GameEvents.RaisePlaySfx(SfxKeys.PanelClose);
    }
}
