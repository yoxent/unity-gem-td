using UnityEditor;
using UnityEngine;

namespace GemTD.Editor
{
    public static class GemTdEditorMenus
    {
        [MenuItem("Gem TD/Create Data Folders")]
        public static void EnsureDataFolders()
        {
            Ensure("Assets/Data/Towers");
            Ensure("Assets/Data/Gems");
            Ensure("Assets/Data/Enemies");
            Ensure("Assets/Data/Waves");
            Ensure("Assets/Data/Evolutions");
            AssetDatabase.Refresh();
            Debug.Log("[Gem TD] Data folders ready.");
        }

        static void Ensure(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
                var name = System.IO.Path.GetFileName(path);
                if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                    AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
