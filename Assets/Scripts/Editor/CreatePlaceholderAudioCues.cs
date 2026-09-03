using System.IO;
using UnityEditor;
using UnityEngine;
using GemTD.Core;

namespace GemTD.Editor
{
    public static class CreatePlaceholderAudioCues
    {
        const string AudioDir = "Assets/Audio";
        const string CueDir = "Assets/Data/Audio";
        const string BgmWav = AudioDir + "/Placeholder_Bgm.wav";
        const string SfxWav = AudioDir + "/Placeholder_Sfx.wav";
        const string BgmCue = CueDir + "/Cue_Bgm.asset";
        const string SfxCue = CueDir + "/Cue_Sfx.asset";

        [MenuItem("Gem TD/Audio/Create Placeholder Cues")]
        public static void Create()
        {
            EnsureFolder("Assets/Audio");
            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Data/Audio");

            if (AssetDatabase.LoadAssetAtPath<AudioClip>(BgmWav) == null)
                WriteWav(BgmWav, 44100, 1.0f, 440f);

            if (AssetDatabase.LoadAssetAtPath<AudioClip>(SfxWav) == null)
                WriteWav(SfxWav, 44100, 0.1f, 880f);

            AssetDatabase.ImportAsset(BgmWav);
            AssetDatabase.ImportAsset(SfxWav);

            var bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmWav);
            var sfxClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxWav);

            if (AssetDatabase.LoadAssetAtPath<AudioCue>(BgmCue) == null)
            {
                var cue = ScriptableObject.CreateInstance<AudioCue>();
                cue.bus = AudioBus.Bgm;
                cue.volume = 1f;
                cue.loop = true;
                cue.bgmClip = bgmClip;
                cue.sfx = SfxData.Default;
                AssetDatabase.CreateAsset(cue, BgmCue);
            }

            if (AssetDatabase.LoadAssetAtPath<AudioCue>(SfxCue) == null)
            {
                var cue = ScriptableObject.CreateInstance<AudioCue>();
                cue.bus = AudioBus.Sfx;
                cue.volume = 1f;
                cue.loop = false;
                var sfx = SfxData.Default;
                sfx.clip = sfxClip;
                cue.sfx = sfx;
                AssetDatabase.CreateAsset(cue, SfxCue);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Placeholder audio cues ready at " + CueDir);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static void WriteWav(string assetPath, int sampleRate, float seconds, float hz)
        {
            var samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * seconds));
            var data = new byte[44 + samples * 2];
            WriteAscii(data, 0, "RIFF");
            WriteInt32(data, 4, 36 + samples * 2);
            WriteAscii(data, 8, "WAVE");
            WriteAscii(data, 12, "fmt ");
            WriteInt32(data, 16, 16);
            WriteInt16(data, 20, 1);
            WriteInt16(data, 22, 1);
            WriteInt32(data, 24, sampleRate);
            WriteInt32(data, 28, sampleRate * 2);
            WriteInt16(data, 32, 2);
            WriteInt16(data, 34, 16);
            WriteAscii(data, 36, "data");
            WriteInt32(data, 40, samples * 2);

            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var s = (short)(Mathf.Sin(2f * Mathf.PI * hz * t) * 8000f);
                var o = 44 + i * 2;
                data[o] = (byte)(s & 0xff);
                data[o + 1] = (byte)((s >> 8) & 0xff);
            }

            var full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, data);
        }

        static void WriteAscii(byte[] data, int offset, string text)
        {
            for (var i = 0; i < text.Length; i++)
                data[offset + i] = (byte)text[i];
        }

        static void WriteInt16(byte[] data, int offset, int value)
        {
            data[offset] = (byte)(value & 0xff);
            data[offset + 1] = (byte)((value >> 8) & 0xff);
        }

        static void WriteInt32(byte[] data, int offset, int value)
        {
            data[offset] = (byte)(value & 0xff);
            data[offset + 1] = (byte)((value >> 8) & 0xff);
            data[offset + 2] = (byte)((value >> 16) & 0xff);
            data[offset + 3] = (byte)((value >> 24) & 0xff);
        }
    }
}
