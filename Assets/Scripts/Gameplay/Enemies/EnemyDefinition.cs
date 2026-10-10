using UnityEngine;

namespace GemTD.Gameplay.Enemies
{
    [CreateAssetMenu(menuName = "Gem TD/Enemy Definition", fileName = "Enemy_")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Tooltip("Body name. Aura tags are written in front of this. Not shown in the game HUD.")]
        public string DisplayName = "Enemy";
        [Tooltip("Hierarchy id in Play Mode. Body name plus the aura tags that fit this rank. Not shown in the game HUD.")]
        public string ShownName = "Enemy";
        public float MaxHealth = 20f;
        public float MoveSpeed = 2f;
        public int Armor;
        public float ShieldMax;
        [Tooltip("Percent. No Physical resist.")]
        public int FireResistance;
        public int ColdResistance;
        public int LightningResistance;
        public int ChaosResistance;
        public int KillGold = 5;
        public int LeakDamage = 1;
        public EnemyRank Rank;
        [Tooltip("Stacking labels. Blink uses Blink Distance and Blink Interval. Bloody, Hunting, Ironclad, and Shaded turn on those auras, up to the rank's affix cap.")]
        public EnemyTag Tags;
        public bool IsBoss => Rank == EnemyRank.Boss;

        public EnemyAffix[] Affixes;
        public LocomotionStyle Locomotion = LocomotionStyle.Slide;
        public float HopHeight = 0.35f;
        public float HopPeriod = 0.4f;
        public float FlyHeight = 0.45f;
        public float FlyPeriod = 1.25f;
        public const float DefaultBlinkDistance = 2f;
        public const float DefaultBlinkInterval = 4f;

        [Tooltip("World units traveled along the path on each blink. Two tiles. The Blink tag turns the blink on. Zero or below snaps back to the default.")]
        public float BlinkDistance = DefaultBlinkDistance;
        [Tooltip("Seconds between blinks. The Blink tag turns the blink on. Zero or below snaps back to the default.")]
        public float BlinkInterval = DefaultBlinkInterval;
        [Tooltip("Optional EnemyView prefab. Empty uses the composition-root enemy prefab.")]
        public EnemyView ViewPrefab;

        public float ResolveBlinkDistance()
        {
            if (!EnemyTags.Has(Tags, EnemyTag.Blink))
                return 0f;
            return BlinkDistance > 0f ? BlinkDistance : DefaultBlinkDistance;
        }

        public float ResolveBlinkInterval()
        {
            if (!EnemyTags.Has(Tags, EnemyTag.Blink))
                return 0f;
            return BlinkInterval > 0f ? BlinkInterval : DefaultBlinkInterval;
        }

        public string PresentedName
        {
            get
            {
                var body = string.IsNullOrEmpty(DisplayName) ? "Enemy" : DisplayName;
                var affixes = Affixes;
                string prefix = null;
                for (var i = 0; i < EnemyTags.AuraOrder.Length; i++)
                {
                    if (!EnemyTags.TryGetAuraAffix(EnemyTags.AuraOrder[i], out var affix))
                        continue;
                    if (!EnemyAffixRules.Contains(affixes, affix))
                        continue;
                    var word = EnemyTags.AuraWord(affix);
                    prefix = prefix == null ? word : prefix + " " + word;
                }

                return prefix == null ? body : prefix + " " + body;
            }
        }

        public void EnforceAuraTags()
        {
            var authored = Affixes;
            var authoredCount = authored == null ? 0 : authored.Length;
            var nonAura = 0;
            for (var i = 0; i < authoredCount; i++)
            {
                if (!IsAuraAffix(authored[i]))
                    nonAura++;
            }

            var slots = EnemyRankRules.MaxAffixes(Rank) - nonAura;
            if (slots < 0)
                slots = 0;

            var tags = Tags;
            var accepted = 0;
            for (var i = 0; i < EnemyTags.AuraOrder.Length; i++)
            {
                var tag = EnemyTags.AuraOrder[i];
                if (!EnemyTags.Has(tags, tag))
                    continue;
                if (accepted < slots)
                    accepted++;
                else
                    tags &= ~tag;
            }

            if (tags != Tags)
                Tags = tags;

            var next = new EnemyAffix[nonAura + accepted];
            var count = 0;
            for (var i = 0; i < authoredCount; i++)
            {
                if (!IsAuraAffix(authored[i]))
                    next[count++] = authored[i];
            }

            for (var i = 0; i < EnemyTags.AuraOrder.Length; i++)
            {
                var tag = EnemyTags.AuraOrder[i];
                if (!EnemyTags.Has(tags, tag) || !EnemyTags.TryGetAuraAffix(tag, out var affix))
                    continue;
                next[count++] = affix;
            }

            if (!SameAffixes(authored, next))
                Affixes = next;
        }

        void OnValidate()
        {
            if (EnemyTags.Has(Tags, EnemyTag.Blink))
            {
                if (BlinkDistance <= 0f)
                    BlinkDistance = DefaultBlinkDistance;
                if (BlinkInterval <= 0f)
                    BlinkInterval = DefaultBlinkInterval;
            }

            EnforceAuraTags();
            var shown = PresentedName;
            if (ShownName != shown)
                ShownName = shown;
        }

        static bool IsAuraAffix(EnemyAffix affix)
        {
            return affix == EnemyAffix.Bloody
                || affix == EnemyAffix.Hunting
                || affix == EnemyAffix.Ironclad
                || affix == EnemyAffix.Shaded;
        }

        static bool SameAffixes(EnemyAffix[] current, EnemyAffix[] next)
        {
            var currentCount = current == null ? 0 : current.Length;
            if (currentCount != next.Length)
                return false;
            for (var i = 0; i < currentCount; i++)
            {
                if (current[i] != next[i])
                    return false;
            }

            return true;
        }
    }
}
