namespace GemTD.Gameplay.Meta
{
    public interface ICodexStore
    {
        CodexSaveDto Load();
        void Save(CodexSaveDto dto);
    }
}
