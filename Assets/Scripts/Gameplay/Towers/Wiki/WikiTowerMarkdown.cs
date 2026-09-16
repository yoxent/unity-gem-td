using System.Text;

namespace GemTD.Gameplay.Towers
{
    public static class WikiTowerMarkdown
    {
        public const string GeneratedMarker = "<!-- wiki-catalog:generated -->";
        public const string NotesStart = "<!-- wiki-catalog:notes-start -->";
        public const string NotesEnd = "<!-- wiki-catalog:notes-end -->";

        public static string FileNameFromSlug(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return "unknown.md";

            var chars = slug.ToCharArray();
            var sb = new StringBuilder(chars.Length + 3);
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (c == '_')
                    sb.Append('-');
                else
                    sb.Append(char.ToLowerInvariant(c));
            }

            sb.Append(".md");
            return sb.ToString();
        }

        public static string TowerPage(WikiTowerPage page)
        {
            var sb = new StringBuilder(2048);
            sb.AppendLine(GeneratedMarker);
            sb.AppendLine();
            sb.Append("# ");
            sb.AppendLine(page.DisplayName);
            sb.AppendLine();
            sb.AppendLine("| Field | Value |");
            sb.AppendLine("| --- | --- |");
            Row(sb, "Slug", "`" + page.Slug + "`");
            Row(sb, "Category", page.CategoryName);
            Row(sb, "Import", page.StatusLabel);
            Row(sb, "In TowerCatalog", page.InTowerCatalog ? "Yes" : "No");
            Row(sb, "Tags", page.Tags);
            Row(sb, "Cost", page.Cost.ToString());
            Row(sb, "Sockets", page.SocketCount.ToString());
            Row(sb, "Role", page.RoleKind);
            Row(sb, "Aim", page.AimMode);
            Row(sb, "Delivery", page.DeliveryPattern);
            Row(sb, "Mix", page.Mix);
            Row(sb, "Spread (°)", page.SpreadDegrees);
            Row(sb, "Sequential interval (s)", page.SequentialIntervalSeconds);
            sb.AppendLine();

            if (!string.IsNullOrEmpty(page.Description))
            {
                sb.AppendLine("## Description");
                sb.AppendLine();
                sb.AppendLine(page.Description.Trim());
                sb.AppendLine();
            }

            sb.AppendLine("## Combat");
            sb.AppendLine();
            sb.AppendLine("| Stat | L" + page.FirstSourceLevel + " | L" + page.LastSourceLevel + " |");
            sb.AppendLine("| --- | ---: | ---: |");
            CombatRow(sb, "Damage", page.First.Damage, page.Last.Damage);
            CombatRow(sb, "Tower radius", page.First.TowerRadius, page.Last.TowerRadius);
            CombatRow(sb, "Splash radius", page.First.SplashRadius, page.Last.SplashRadius);
            CombatRow(sb, "Projectiles", page.First.ProjectileCount.ToString(), page.Last.ProjectileCount.ToString());
            CombatRow(sb, "Chain", page.First.ChainCount.ToString(), page.Last.ChainCount.ToString());
            CombatRow(sb, "Fork", page.First.ForkCount.ToString(), page.Last.ForkCount.ToString());
            CombatRow(sb, "Attack time", page.First.AttackTime, page.Last.AttackTime);
            CombatRow(sb, "Attack speed", page.First.AttackSpeed, page.Last.AttackSpeed);
            CombatRow(sb, "Cast time", page.First.CastTime, page.Last.CastTime);
            CombatRow(sb, "Cast speed", page.First.CastSpeed, page.Last.CastSpeed);
            CombatRow(sb, "Reservation %", page.First.ReservationPercent, page.Last.ReservationPercent);
            CombatRow(sb, "Fire interval (s)", page.First.FireInterval, page.Last.FireInterval);
            sb.AppendLine();

            AppendBulletSection(sb, "Role modifiers (base)", page.BaseModifiers);
            AppendBulletSection(sb, "Effects", page.EffectLines);
            AppendBulletSection(sb, "Effect payloads", page.PayloadLines);

            sb.AppendLine("## Links");
            sb.AppendLine();
            sb.AppendLine("- [Category index](README.md)");
            sb.AppendLine("- [Tower catalog](../README.md)");
            sb.AppendLine("- [HOME](../../../../HOME.md)");
            sb.AppendLine("- Handoff: [`planning/handoff.md`](../../../../planning/handoff.md)");
            sb.AppendLine("- Design: [`GDD.md`](../../../../GDD.md) §5 Towers");
            sb.AppendLine();
            sb.AppendLine("## Notes");
            sb.AppendLine();
            sb.AppendLine(NotesStart);
            sb.AppendLine();
            sb.AppendLine(NotesEnd);
            return sb.ToString();
        }

        public static string CategoryIndex(string categoryName, WikiTowerPage[] pages)
        {
            var sb = new StringBuilder(1024);
            sb.AppendLine(GeneratedMarker);
            sb.AppendLine();
            sb.Append("# ");
            sb.Append(categoryName);
            sb.AppendLine(" towers");
            sb.AppendLine();
            sb.AppendLine("Generated from live ScriptableObjects. Gameplay-complete towers only.");
            sb.AppendLine();
            sb.AppendLine("| Tower | Slug | Delivery | Mix | Catalog |");
            sb.AppendLine("| --- | --- | --- | --- | --- |");
            for (var i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                sb.Append("| [");
                sb.Append(page.DisplayName);
                sb.Append("](");
                sb.Append(FileNameFromSlug(page.Slug));
                sb.Append(") | `");
                sb.Append(page.Slug);
                sb.Append("` | ");
                sb.Append(page.DeliveryPattern);
                sb.Append(" | ");
                sb.Append(page.Mix);
                sb.Append(" | ");
                sb.Append(page.InTowerCatalog ? "Yes" : "No");
                sb.AppendLine(" |");
            }

            sb.AppendLine();
            sb.AppendLine("[All categories](../README.md) · [HOME](../../../../HOME.md)");
            return sb.ToString();
        }

        public static string TowersRootIndex(int attack, int spell, int curse, int aura)
        {
            var sb = new StringBuilder(512);
            sb.AppendLine(GeneratedMarker);
            sb.AppendLine();
            sb.AppendLine("# Tower catalog");
            sb.AppendLine();
            sb.AppendLine("Gameplay-complete towers only. Regenerated by `Gem TD/Export Wiki Tower Catalog (Completed)`.");
            sb.AppendLine();
            sb.AppendLine("| Category | Pages |");
            sb.AppendLine("| --- | ---: |");
            sb.AppendLine("| [Attack](attack/README.md) | " + attack + " |");
            sb.AppendLine("| [Spell](spell/README.md) | " + spell + " |");
            sb.AppendLine("| [Curse](curse/README.md) | " + curse + " |");
            sb.AppendLine("| [Aura](aura/README.md) | " + aura + " |");
            sb.AppendLine();
            sb.AppendLine("Total **" + (attack + spell + curse + aura) + "**. Support gems are a later slice.");
            sb.AppendLine();
            sb.AppendLine("[Catalog README](../README.md) · [HOME](../../../HOME.md)");
            return sb.ToString();
        }

        public static string MergeNotes(string generated, string existing)
        {
            if (string.IsNullOrEmpty(existing))
                return generated;

            var notes = ExtractNotes(existing);
            return ReplaceNotes(generated, notes);
        }

        static string ExtractNotes(string markdown)
        {
            var start = markdown.IndexOf(NotesStart);
            var end = markdown.IndexOf(NotesEnd);
            if (start < 0 || end < 0 || end <= start)
                return "\n";

            start += NotesStart.Length;
            return markdown.Substring(start, end - start);
        }

        static string ReplaceNotes(string generated, string notes)
        {
            var start = generated.IndexOf(NotesStart);
            var end = generated.IndexOf(NotesEnd);
            if (start < 0 || end < 0 || end <= start)
                return generated;

            start += NotesStart.Length;
            return generated.Substring(0, start) + notes + generated.Substring(end);
        }

        static void Row(StringBuilder sb, string field, string value)
        {
            sb.Append("| ");
            sb.Append(field);
            sb.Append(" | ");
            sb.Append(string.IsNullOrEmpty(value) ? "—" : value.Replace("\n", " "));
            sb.AppendLine(" |");
        }

        static void CombatRow(StringBuilder sb, string stat, string first, string last)
        {
            sb.Append("| ");
            sb.Append(stat);
            sb.Append(" | ");
            sb.Append(first);
            sb.Append(" | ");
            sb.Append(last);
            sb.AppendLine(" |");
        }

        static void AppendBulletSection(StringBuilder sb, string heading, string[] lines)
        {
            sb.Append("## ");
            sb.AppendLine(heading);
            sb.AppendLine();
            if (lines == null || lines.Length == 0)
            {
                sb.AppendLine("None.");
                sb.AppendLine();
                return;
            }

            for (var i = 0; i < lines.Length; i++)
            {
                sb.Append("- ");
                sb.AppendLine(lines[i]);
            }

            sb.AppendLine();
        }
    }
}
