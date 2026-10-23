using System;
using GemTD.Gameplay.Gems;
using UnityEngine;

namespace GemTD.UI
{
    /// <summary>
    /// Draft-card chrome colors. Rows are chosen from Fire, Cold, Lightning,
    /// and Chaos tags. Physical and every other tag count as non-elemental.
    /// </summary>
    [CreateAssetMenu(menuName = "Gem TD/Draft Element Colors", fileName = "DraftElementColors")]
    public sealed class DraftElementColors : ScriptableObject
    {
        public const GemTag ElementMask =
            GemTag.Fire | GemTag.Cold | GemTag.Lightning | GemTag.Chaos;

        [Serializable]
        public struct Palette
        {
            public Color normal;
            public Color dimmed;
        }

        public Palette nonElemental = Row(0x8B9AAB);
        public Palette fire = Row(0xE85D04);
        public Palette cold = Row(0x4A9BF0, 0xDEEDFC);
        public Palette lightning = Row(0xFFA62B, 0xFFEFD9);
        public Palette chaos = Row(0x9B5DE5);
        public Palette fireCold = Row(0xC45C7A);
        public Palette fireLightning = Row(0xFF9F1C);
        public Palette fireChaos = Row(0xC43C6E);
        public Palette coldLightning = Row(0x7AD7FF);
        public Palette coldChaos = Row(0x6C6CFF);
        public Palette lightningChaos = Row(0xC084FC);
        public Palette threeElements = Row(0xE7D48A);

        public Palette Resolve(GemTag tags)
        {
            switch (tags & ElementMask)
            {
                case GemTag.None:
                    return nonElemental;
                case GemTag.Fire:
                    return fire;
                case GemTag.Cold:
                    return cold;
                case GemTag.Lightning:
                    return lightning;
                case GemTag.Chaos:
                    return chaos;
                case GemTag.Fire | GemTag.Cold:
                    return fireCold;
                case GemTag.Fire | GemTag.Lightning:
                    return fireLightning;
                case GemTag.Fire | GemTag.Chaos:
                    return fireChaos;
                case GemTag.Cold | GemTag.Lightning:
                    return coldLightning;
                case GemTag.Cold | GemTag.Chaos:
                    return coldChaos;
                case GemTag.Lightning | GemTag.Chaos:
                    return lightningChaos;
                default:
                    return threeElements;
            }
        }

        static Palette Row(uint normal)
        {
            return Row(normal, Dim(normal));
        }

        static Palette Row(uint normal, uint dimmed)
        {
            return new Palette
            {
                normal = Rgb(normal),
                dimmed = Rgb(dimmed)
            };
        }

        /// <summary>Same mix as the authored cold and lightning dimmed colors: 82% toward white.</summary>
        static uint Dim(uint rgb)
        {
            var r = (rgb >> 16) & 255;
            var g = (rgb >> 8) & 255;
            var b = rgb & 255;
            return (Channel(r) << 16) | (Channel(g) << 8) | Channel(b);
        }

        static uint Channel(uint value)
        {
            return (uint)(value + (255 - value) * 0.82f + 0.5f);
        }

        static Color Rgb(uint rgb)
        {
            return new Color32(
                (byte)((rgb >> 16) & 255),
                (byte)((rgb >> 8) & 255),
                (byte)(rgb & 255),
                255);
        }
    }
}
