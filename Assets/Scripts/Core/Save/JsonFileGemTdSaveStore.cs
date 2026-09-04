using System.IO;
using UnityEngine;

namespace GemTD.Core
{
    public sealed class JsonFileGemTdSaveStore : IGemTdSaveStore
    {
        readonly string _path;

        public JsonFileGemTdSaveStore(string path = null)
        {
            if (!string.IsNullOrEmpty(path))
            {
                _path = path;
                return;
            }

            var dir = Path.Combine(Application.persistentDataPath, "GemTD");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "save.json");
        }

        public GemTdSaveDto Load()
        {
            if (!File.Exists(_path))
                return new GemTdSaveDto();

            var json = File.ReadAllText(_path);
            if (string.IsNullOrEmpty(json))
                return new GemTdSaveDto();

            var dto = JsonUtility.FromJson<GemTdSaveDto>(json);
            return dto ?? new GemTdSaveDto();
        }

        public void Save(GemTdSaveDto dto)
        {
            if (dto == null)
                dto = new GemTdSaveDto();

            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(_path, JsonUtility.ToJson(dto));
        }
    }
}
