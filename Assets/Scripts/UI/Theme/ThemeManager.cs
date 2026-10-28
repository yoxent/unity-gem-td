using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GemTD.UI
{
    /// <summary>Style id typed on a theme row. Saved as lowercase.</summary>
    public sealed class ThemeIdAttribute : PropertyAttribute
    {
    }

    /// <summary>Marks a string field as a style id chosen from the Theme Applicator's theme asset.</summary>
    public sealed class ThemeStyleIdAttribute : PropertyAttribute
    {
    }

    /// <summary>Typography applied to one TextMeshPro label.</summary>
    [System.Serializable]
    public struct ThemeTextStyle
    {
        [ThemeId]
        [Tooltip("Key a Theme Applicator uses to find this style. Stored trimmed and in lowercase.")]
        public string id;
        public Color color;
        public float fontSize;
        public TMP_FontAsset font;

        public void Apply(TMP_Text text)
        {
            if (text == null)
            {
                Debug.LogError("ThemeTextStyle.Apply: text is null.");
                return;
            }

            if (text.color != color)
                text.color = color;
            if (fontSize > 0f && !Mathf.Approximately(text.fontSize, fontSize))
                text.fontSize = fontSize;
            if (font != null && text.font != font)
                text.font = font;
        }
    }

    public struct ThemeDuplicateNotice
    {
        public string fromId;
        public string toId;
    }

    /// <summary>Shared colorway and typography. Each row is one named text style.</summary>
    [CreateAssetMenu(menuName = "Gem TD/Theme", fileName = "Theme")]
    public sealed class ThemeManager : ScriptableObject
    {
        [SerializeField] ThemeTextStyle[] styles =
        {
            new ThemeTextStyle { id = "primary", color = Color.white, fontSize = 18f },
            new ThemeTextStyle { id = "secondary", color = Color.white, fontSize = 14f },
            new ThemeTextStyle { id = "header", color = Color.white, fontSize = 24f }
        };

        string[] _keys;
        [System.NonSerialized] List<ThemeDuplicateNotice> _duplicateNotices;

        public int DuplicateNoticeCount => _duplicateNotices == null ? 0 : _duplicateNotices.Count;

        public ThemeDuplicateNotice GetDuplicateNotice(int index) => _duplicateNotices[index];

        /// <summary>
        /// Trims ends and lowercases. Returns the same string when it is already in that form.
        /// </summary>
        public static string NormalizeId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return string.Empty;

            var start = 0;
            var end = id.Length;
            while (start < end && char.IsWhiteSpace(id[start]))
                start++;
            while (end > start && char.IsWhiteSpace(id[end - 1]))
                end--;

            if (start == end)
                return string.Empty;

            var needsLower = false;
            for (var i = start; i < end; i++)
            {
                if (char.ToLowerInvariant(id[i]) != id[i])
                {
                    needsLower = true;
                    break;
                }
            }

            if (start == 0 && end == id.Length && !needsLower)
                return id;

            var slice = start == 0 && end == id.Length ? id : id.Substring(start, end - start);
            return needsLower ? slice.ToLowerInvariant() : slice;
        }

        void OnValidate()
        {
            _keys = null;
            if (styles == null)
                return;

            for (var i = 0; i < styles.Length; i++)
            {
                var style = styles[i];
                var lower = NormalizeId(style.id);
                if (lower == style.id)
                    continue;

                style.id = lower;
                styles[i] = style;
            }

            ResolveDuplicateIds();
            PruneDuplicateNotices();
        }

        void ResolveDuplicateIds()
        {
            for (var i = 0; i < styles.Length; i++)
            {
                var id = styles[i].id;
                if (string.IsNullOrEmpty(id) || FirstIndex(id) == i)
                    continue;

                var style = styles[i];
                var renamed = NextFreeId(id);
                style.id = renamed;
                styles[i] = style;
                NoteDuplicate(id, renamed);
            }
        }

        int FirstIndex(string id)
        {
            for (var i = 0; i < styles.Length; i++)
            {
                if (styles[i].id == id)
                    return i;
            }

            return -1;
        }

        int CountId(string id)
        {
            var count = 0;
            for (var i = 0; i < styles.Length; i++)
            {
                if (styles[i].id == id)
                    count++;
            }

            return count;
        }

        string NextFreeId(string id)
        {
            for (var n = 1; n < 100; n++)
            {
                var candidate = id + "_" + n.ToString("00");
                if (CountId(candidate) == 0)
                    return candidate;
            }

            return id + "_99";
        }

        void NoteDuplicate(string fromId, string toId)
        {
            if (_duplicateNotices == null)
                _duplicateNotices = new List<ThemeDuplicateNotice>();

            for (var i = 0; i < _duplicateNotices.Count; i++)
            {
                if (_duplicateNotices[i].toId == toId)
                    return;
            }

            _duplicateNotices.Add(new ThemeDuplicateNotice { fromId = fromId, toId = toId });
        }

        void PruneDuplicateNotices()
        {
            if (_duplicateNotices == null)
                return;

            for (var i = _duplicateNotices.Count - 1; i >= 0; i--)
            {
                if (CountId(_duplicateNotices[i].toId) == 0)
                    _duplicateNotices.RemoveAt(i);
            }
        }

        public void Apply(TMP_Text text, string styleId, GameObject owner = null)
        {
            if (!TryGet(styleId, out var style))
            {
                var ownerName = owner != null ? owner.name : "unknown";
                var textName = text != null ? text.gameObject.name : "unknown";
                Debug.LogError(
                    $"ThemeManager '{name}' has no text style '{styleId}' on GameObject '{ownerName}' (text '{textName}').",
                    text != null ? text : owner);
                return;
            }

            style.Apply(text);
        }

        public bool TryGet(string styleId, out ThemeTextStyle style)
        {
            styleId = NormalizeId(styleId);
            if (styles != null && styleId.Length > 0)
            {
                EnsureKeys();
                var keys = _keys;
                for (var i = 0; i < styles.Length; i++)
                {
                    if (!string.Equals(keys[i], styleId, System.StringComparison.Ordinal))
                        continue;

                    style = styles[i];
                    return true;
                }
            }

            style = default;
            return false;
        }

        void EnsureKeys()
        {
            if (_keys != null && styles != null && _keys.Length == styles.Length)
                return;

            var count = styles == null ? 0 : styles.Length;
            _keys = new string[count];
            for (var i = 0; i < count; i++)
                _keys[i] = NormalizeId(styles[i].id);
        }
    }
}
