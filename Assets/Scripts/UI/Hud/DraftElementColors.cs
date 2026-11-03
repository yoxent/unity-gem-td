using System;
using GemTD.Gameplay.Gems;
using UnityEngine;

namespace GemTD.UI
{
    /// <summary>
    /// Initial rows for tag chips and card chrome. Both use one formula:
    /// border is a lighter tint, dimmed is lighter still. Card chrome is a
    /// brighter palette. Values saved on the asset stay as edited.
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
            public Color border;
        }

        [Header("Tag chips")]
        public Palette area = Row(0x212F67);
        public Palette attack = Row(0xA32F2F);
        public Palette aura = Row(0x43226B);
        public Palette bow = Row(0xA32F2F);
        public Palette channeling = Row(0xE08A2C);
        public Palette curse = Row(0x572750);
        public Palette duration = Row(0x43226B);
        public Palette melee = Row(0xA32F2F);
        public Palette mine = Row(0x864C08);
        public Palette projectile = Row(0x3D8BFF);
        public Palette spell = Row(0xA32F2F);
        public Palette summon = Row(0x43226B);
        public Palette trap = Row(0x864C08);

        [Header("Card chrome")]
        public Palette chaos = Row(0x78C850);
        public Palette cold = Row(0x4AA0F0);
        public Palette coldChaos = Row(0x61B4A0);
        public Palette coldLightning = Row(0x3EC8DC);
        public Palette fire = Row(0xF05C48);
        public Palette fireChaos = Row(0xB4924C);
        public Palette fireCold = Row(0x9D7E9C);
        public Palette fireLightning = Row(0xF09249);
        public Palette lightning = Row(0xF0C84A);
        public Palette lightningChaos = Row(0xB4C84D);
        public Palette nonElemental = Row(0xC0C8D0);
        public Palette threeElements = Row(0xB99781);

        public Palette Resolve(GemTag tags)
        {
            switch (tags & ElementMask)
            {
                case GemTag.None:
                    return nonElemental;
                case GemTag.Chaos:
                    return chaos;
                case GemTag.Cold:
                    return cold;
                case GemTag.Cold | GemTag.Chaos:
                    return coldChaos;
                case GemTag.Cold | GemTag.Lightning:
                    return coldLightning;
                case GemTag.Fire:
                    return fire;
                case GemTag.Fire | GemTag.Chaos:
                    return fireChaos;
                case GemTag.Fire | GemTag.Cold:
                    return fireCold;
                case GemTag.Fire | GemTag.Lightning:
                    return fireLightning;
                case GemTag.Lightning:
                    return lightning;
                case GemTag.Lightning | GemTag.Chaos:
                    return lightningChaos;
                default:
                    return threeElements;
            }
        }

        public Palette ForTagLabel(GemTag tag)
        {
            if (tag == GemTag.Fire || tag == GemTag.Cold || tag == GemTag.Lightning || tag == GemTag.Chaos)
                return Resolve(tag);
            return ResolveTag(tag);
        }

        public Palette ResolveTag(GemTag tag)
        {
            if (tag == GemTag.Hex || tag == GemTag.Mark)
                tag = GemTag.Curse;
            else if (IsSummon(tag))
                tag = GemTag.Minion;

            switch (tag)
            {
                case GemTag.Aoe:
                    return area;
                case GemTag.Attack:
                    return attack;
                case GemTag.Aura:
                    return aura;
                case GemTag.Bow:
                    return bow;
                case GemTag.Channeling:
                    return channeling;
                case GemTag.Curse:
                    return curse;
                case GemTag.Duration:
                    return duration;
                case GemTag.Melee:
                    return melee;
                case GemTag.Mine:
                    return mine;
                case GemTag.Projectile:
                    return projectile;
                case GemTag.Spell:
                    return spell;
                case GemTag.Minion:
                    return summon;
                case GemTag.Trap:
                    return trap;
                default:
                    return nonElemental;
            }
        }

        static bool IsSummon(GemTag tag)
        {
            return tag == GemTag.Minion
                || tag == GemTag.Totem
                || tag == GemTag.Brand
                || tag == GemTag.Golem;
        }

        static Palette Row(uint normal)
        {
            return new Palette
            {
                normal = Rgb(normal),
                dimmed = Rgb(Lighten(normal, 0.82f)),
                border = Rgb(Lighten(normal, 0.42f))
            };
        }

        static uint Lighten(uint rgb, float towardWhite)
        {
            var r = (rgb >> 16) & 255;
            var g = (rgb >> 8) & 255;
            var b = rgb & 255;
            return (Mix(r, towardWhite) << 16)
                | (Mix(g, towardWhite) << 8)
                | Mix(b, towardWhite);
        }

        static uint Mix(uint value, float towardWhite)
        {
            return (uint)(value + (255 - value) * towardWhite + 0.5f);
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
