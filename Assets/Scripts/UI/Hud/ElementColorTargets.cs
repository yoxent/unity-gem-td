using System;
using UnityEngine;
using UnityEngine.UI;

namespace GemTD.UI
{
    /// <summary>
    /// Graphics painted from one <see cref="DraftElementColors.Palette"/>.
    /// Put each graphic in <see cref="dimmed"/> or <see cref="normal"/>.
    /// <see cref="selectedHighlight"/> uses the normal color until a card is selected.
    /// </summary>
    [Serializable]
    public struct ElementColorTargets
    {
        public Graphic[] dimmed;
        public Graphic[] normal;
        public Graphic selectedHighlight;

        public bool Apply(DraftElementColors.Palette palette)
        {
            var painted = Paint(dimmed, palette.dimmed);
            painted |= Paint(normal, palette.normal);
            painted |= PaintOne(selectedHighlight, palette.normal);
            return painted;
        }

        static bool Paint(Graphic[] graphics, Color color)
        {
            if (graphics == null || graphics.Length == 0)
                return false;

            var painted = false;
            for (var i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null)
                    continue;
                graphics[i].color = color;
                painted = true;
            }

            return painted;
        }

        static bool PaintOne(Graphic graphic, Color color)
        {
            if (graphic == null)
                return false;

            graphic.color = color;
            return true;
        }
    }
}
