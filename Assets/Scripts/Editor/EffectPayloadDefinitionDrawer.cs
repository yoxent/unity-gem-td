using UnityEditor;
using UnityEngine;
using GemTD.Gameplay.Towers;

namespace GemTD.Editor
{
    [CustomPropertyDrawer(typeof(EffectPayloadDefinition))]
    public sealed class EffectPayloadDefinitionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var trigger = TriggerOf(property);
            var travel = TravelOf(property);
            var scatter = ScatterOf(property);
            var gap = EditorGUIUtility.standardVerticalSpacing;
            var height = 0f;
            height += FieldHeight(property, "Trigger", gap);
            height += FieldHeight(property, "Anchor", gap);
            height += FieldHeight(property, "TravelPattern", gap);
            if (ShowsScatter(travel))
                height += FieldHeight(property, "ScatterPattern", gap);
            height += FieldHeight(property, "HitPolicy", gap);
            height += FieldHeight(property, "tags", gap);
            height += FieldHeight(property, "Count", gap);
            height += FieldHeight(property, "DamageMultiplier", gap);
            height += FieldHeight(property, "AoeRadius", gap);
            if (ShowsMinDistance(travel, scatter))
                height += FieldHeight(property, "MinDistance", gap);
            if (ShowsMaxDistance(travel, scatter))
                height += FieldHeight(property, "MaxDistance", gap);
            if (ShowsArcHeight(travel))
                height += FieldHeight(property, "ArcHeight", gap);
            if (ShowsDelay(trigger))
                height += FieldHeight(property, "DelaySeconds", gap);
            if (ShowsInterval(trigger, travel))
                height += FieldHeight(property, "IntervalSeconds", gap);
            if (ShowsRepeat(trigger))
                height += FieldHeight(property, "RepeatCount", gap);
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var gap = EditorGUIUtility.standardVerticalSpacing;
            var y = position.y;
            var x = position.x;
            var width = position.width;

            var trigger = TriggerOf(property);
            var travel = TravelOf(property);
            var scatter = ScatterOf(property);

            Draw(property, "Trigger", null, ref y, x, width, gap);
            Draw(property, "Anchor", null, ref y, x, width, gap);
            Draw(property, "TravelPattern", null, ref y, x, width, gap);
            if (ShowsScatter(travel))
                Draw(property, "ScatterPattern", null, ref y, x, width, gap);
            Draw(property, "HitPolicy", null, ref y, x, width, gap);
            Draw(property, "tags", null, ref y, x, width, gap);
            Draw(property, "Count", null, ref y, x, width, gap);
            Draw(property, "DamageMultiplier", null, ref y, x, width, gap);
            Draw(property, "AoeRadius", null, ref y, x, width, gap);

            if (ShowsMinDistance(travel, scatter))
                Draw(property, "MinDistance", null, ref y, x, width, gap);
            if (ShowsMaxDistance(travel, scatter))
            {
                var maxLabel = travel == EffectPayloadTravelPattern.FallFromSky
                    ? new GUIContent("Storm Radius", "Scatter radius of later rain landings around the aim point.")
                    : null;
                Draw(property, "MaxDistance", maxLabel, ref y, x, width, gap);
            }

            if (ShowsArcHeight(travel))
            {
                var arcLabel = travel == EffectPayloadTravelPattern.FallFromSky
                    ? new GUIContent("Drop Height", "How far above the landing the bolt starts.")
                    : new GUIContent("Arc Height");
                Draw(property, "ArcHeight", arcLabel, ref y, x, width, gap);
            }

            if (ShowsDelay(trigger))
                Draw(property, "DelaySeconds", null, ref y, x, width, gap);
            if (ShowsInterval(trigger, travel))
                Draw(property, "IntervalSeconds", null, ref y, x, width, gap);
            if (ShowsRepeat(trigger))
                Draw(property, "RepeatCount", null, ref y, x, width, gap);

            EditorGUI.EndProperty();
        }

        static float FieldHeight(SerializedProperty property, string field, float gap)
        {
            var relative = property.FindPropertyRelative(field);
            if (relative == null)
                return 0f;
            return EditorGUI.GetPropertyHeight(relative, true) + gap;
        }

        static void Draw(
            SerializedProperty property,
            string field,
            GUIContent label,
            ref float y,
            float x,
            float width,
            float gap)
        {
            var relative = property.FindPropertyRelative(field);
            if (relative == null)
                return;

            var height = EditorGUI.GetPropertyHeight(relative, true);
            var rect = new Rect(x, y, width, height);
            if (label != null)
                EditorGUI.PropertyField(rect, relative, label, true);
            else
                EditorGUI.PropertyField(rect, relative, true);
            y += height + gap;
        }

        static EffectPayloadTrigger TriggerOf(SerializedProperty property)
        {
            return (EffectPayloadTrigger)property.FindPropertyRelative("Trigger").enumValueIndex;
        }

        static EffectPayloadTravelPattern TravelOf(SerializedProperty property)
        {
            return (EffectPayloadTravelPattern)property.FindPropertyRelative("TravelPattern").enumValueIndex;
        }

        static EffectPayloadScatterPattern ScatterOf(SerializedProperty property)
        {
            return (EffectPayloadScatterPattern)property.FindPropertyRelative("ScatterPattern").enumValueIndex;
        }

        static bool ShowsScatter(EffectPayloadTravelPattern travel)
        {
            return travel != EffectPayloadTravelPattern.FallFromSky;
        }

        static bool ShowsMinDistance(
            EffectPayloadTravelPattern travel,
            EffectPayloadScatterPattern scatter)
        {
            if (travel == EffectPayloadTravelPattern.FallFromSky)
                return false;
            return scatter == EffectPayloadScatterPattern.FixedRadial
                || scatter == EffectPayloadScatterPattern.RandomRing;
        }

        static bool ShowsMaxDistance(
            EffectPayloadTravelPattern travel,
            EffectPayloadScatterPattern scatter)
        {
            if (travel == EffectPayloadTravelPattern.FallFromSky)
                return true;
            return scatter == EffectPayloadScatterPattern.FixedRadial
                || scatter == EffectPayloadScatterPattern.RandomRing;
        }

        static bool ShowsArcHeight(EffectPayloadTravelPattern travel)
        {
            return travel == EffectPayloadTravelPattern.Fountain
                || travel == EffectPayloadTravelPattern.FallFromSky;
        }

        static bool ShowsDelay(EffectPayloadTrigger trigger)
        {
            return trigger == EffectPayloadTrigger.AfterDelay;
        }

        static bool ShowsInterval(EffectPayloadTrigger trigger, EffectPayloadTravelPattern travel)
        {
            return trigger == EffectPayloadTrigger.RepeatingPulse
                || travel == EffectPayloadTravelPattern.FallFromSky;
        }

        static bool ShowsRepeat(EffectPayloadTrigger trigger)
        {
            return trigger == EffectPayloadTrigger.RepeatingPulse;
        }
    }
}
