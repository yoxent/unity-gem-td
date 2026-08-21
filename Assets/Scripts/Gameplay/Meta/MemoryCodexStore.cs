using System;

namespace GemTD.Gameplay.Meta
{
    public sealed class MemoryCodexStore : ICodexStore
    {
        CodexSaveDto _dto = new CodexSaveDto();

        public CodexSaveDto Load()
        {
            return new CodexSaveDto
            {
                UnlockedIds = _dto.UnlockedIds == null
                    ? Array.Empty<string>()
                    : (string[])_dto.UnlockedIds.Clone(),
            };
        }

        public void Save(CodexSaveDto dto)
        {
            _dto = dto != null
                ? new CodexSaveDto
                {
                    UnlockedIds = dto.UnlockedIds == null
                        ? Array.Empty<string>()
                        : (string[])dto.UnlockedIds.Clone(),
                }
                : new CodexSaveDto();
        }
    }
}