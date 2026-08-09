using System;

namespace GemTD.Gameplay.Meta
{
    public sealed class CodexProgress
    {
        public const string CrypticHydraHint = "Three jaws share one quarrelsome appetite.";
        public const string RevealedHydraText = "Hydra Ballista — Chain + Fork + Multiple Projectiles";

        readonly ICodexStore _store;
        bool _hydraUnlocked;

        public CodexProgress(ICodexStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            var dto = _store.Load() ?? new CodexSaveDto();
            _hydraUnlocked = dto.HydraUnlocked;
        }

        public bool IsHydraUnlocked => _hydraUnlocked;

        public string HydraHintOrReveal =>
            _hydraUnlocked ? RevealedHydraText : CrypticHydraHint;

        public void NotifyHydraFormed()
        {
            if (_hydraUnlocked)
                return;

            _hydraUnlocked = true;
            _store.Save(new CodexSaveDto { HydraUnlocked = true });
        }
    }
}
