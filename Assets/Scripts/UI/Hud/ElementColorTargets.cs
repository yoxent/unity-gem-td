using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    /// <summary>
    /// One graphic and one TextMesh Pro label painted with the same color.
    /// </summary>
    [Serializable]
    public struct ElementColorTarget
    {
        public Graphic graphic;
        public TMP_Text text;
    }

    /// <summary>
    /// Slots painted from one <see cref="DraftElementColors.Palette"/>.
    /// Put each pair in <see cref="dimmed"/>, <see cref="normal"/>, or <see cref="border"/>.
    /// <see cref="selectedHighlight"/> uses the normal color until a card is selected.
    /// </summary>
    [Serializable]
    public struct ElementColorTargets
    {
        public ElementColorTarget[] dimmed;
        public ElementColorTarget[] normal;
        public ElementColorTarget[] border;
        public ElementColorTarget selectedHighlight;

        public bool Apply(DraftElementColors.Palette palette)
        {
            var painted = Paint(dimmed, palette.dimmed);
            painted |= Paint(normal, palette.normal);
            painted |= Paint(border, palette.border);
            painted |= PaintOne(selectedHighlight, palette.normal);
            return painted;
        }

        public void PaintSelectedHighlight(Color color)
        {
            PaintOne(selectedHighlight, color);
        }

        public bool HasSelectedHighlight =>
            selectedHighlight.graphic != null || selectedHighlight.text != null;

        static bool Paint(ElementColorTarget[] targets, Color color)
        {
            if (targets == null || targets.Length == 0)
                return false;

            var painted = false;
            for (var i = 0; i < targets.Length; i++)
                painted |= PaintOne(targets[i], color);

            return painted;
        }

        static bool PaintOne(ElementColorTarget target, Color color)
        {
            var painted = false;
            if (target.graphic != null)
            {
                if (target.graphic.color != color)
                    target.graphic.color = color;
                painted = true;
            }

            if (target.text != null)
            {
                if (target.text.color != color)
                    target.text.color = color;
                painted = true;
            }

            return painted;
        }
    }
}
