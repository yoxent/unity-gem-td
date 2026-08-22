namespace GemTD.Core
{
    public interface IGemTdSaveStore
    {
        GemTdSaveDto Load();
        void Save(GemTdSaveDto dto);
    }
}
