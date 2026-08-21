using System.IO;
using UnityEngine;

namespace GemTD.Gameplay.Meta
{
    public sealed class JsonFileCodexStore : ICodexStore
    {
        readonly string _path;

        public JsonFileCodexStore(string path = null)
        {
            if (!string.IsNullOrEmpty(path))
            {
                _path = path;
                return;
            }

            var dir = Path.Combine(Application.persistentDataPath, "GemTD");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "codex.json");
        }

        public CodexSaveDto Load()
        {
            if (!File.Exists(_path))
                return new CodexSaveDto();

            var json = File.ReadAllText(_path);
            if (string.IsNullOrEmpty(json))
                return new CodexSaveDto();

            var dto = JsonUtility.FromJson<CodexSaveDto>(json);
            return dto ?? new CodexSaveDto();
        }

        public void Save(CodexSaveDto dto)
        {
            if (dto == null)
                dto = new CodexSaveDto();

            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(_path, JsonUtility.ToJson(dto));
        }
    }
}
