namespace GemTD.Gameplay.Meta
{
    public sealed class MemoryCodexStore : ICodexStore
    {
        CodexSaveDto _dto = new CodexSaveDto();

        public CodexSaveDto Load()
        {
            return new CodexSaveDto { HydraUnlocked = _dto.HydraUnlocked };
        }

        public void Save(CodexSaveDto dto)
        {
            _dto = dto != null
                ? new CodexSaveDto { HydraUnlocked = dto.HydraUnlocked }
                : new CodexSaveDto();
        }
    }
}
