namespace GemTD.Gameplay.Towers
{
    public enum WikiTowerCatalogCategory
    {
        Attack,
        Spell,
        Curse,
        Aura
    }

    public enum WikiTowerImportStatus
    {
        ProofSet,
        GameplayComplete
    }

    public readonly struct WikiTowerCatalogEntry
    {
        public readonly string Slug;
        public readonly WikiTowerCatalogCategory Category;
        public readonly WikiTowerImportStatus Status;
        public readonly bool InTowerCatalog;

        public WikiTowerCatalogEntry(
            string slug,
            WikiTowerCatalogCategory category,
            WikiTowerImportStatus status,
            bool inTowerCatalog)
        {
            Slug = slug;
            Category = category;
            Status = status;
            InTowerCatalog = inTowerCatalog;
        }

        public string CategoryName => Category.ToString();

        public string CategoryFolder
        {
            get
            {
                switch (Category)
                {
                    case WikiTowerCatalogCategory.Attack: return "attack";
                    case WikiTowerCatalogCategory.Spell: return "spell";
                    case WikiTowerCatalogCategory.Curse: return "curse";
                    case WikiTowerCatalogCategory.Aura: return "aura";
                    default: return "other";
                }
            }
        }

        public string StatusLabel =>
            Status == WikiTowerImportStatus.ProofSet ? "Proof set" : "Gameplay complete";
    }

    /// <summary>
    /// Gameplay-complete towers for wiki export. Other curse/aura SOs may have
    /// filled data but are not ready for gameplay and are omitted.
    /// </summary>
    public static class WikiTowerCatalogSets
    {
        public static readonly WikiTowerCatalogEntry[] Completed =
        {
            Attack("Molten_Strike"),
            Attack("Earthquake"),
            Attack("Lightning_Arrow"),
            Attack("Burning_Arrow"),
            Attack("Heavy_Strike"),
            Attack("Split_Arrow"),
            Attack("Barrage"),
            Attack("Ice_Shot"),
            Attack("Cobra_Lash"),
            Attack("Cleave"),
            Spell("Frostbolt"),
            Spell("Firestorm"),
            Spell("Ice_Nova"),
            Spell("Arc"),
            Spell("Fireball"),
            Curse("Elemental_Weakness"),
            Curse("Conductivity"),
            Curse("Flammability"),
            Curse("Temporal_Chains"),
            Curse("Frostbite"),
            Curse("Vulnerability"),
            Curse("Despair"),
            Aura("Anger"),
            Aura("Wrath"),
            Aura("Envy"),
            Aura("Hatred"),
            Aura("Haste"),
            Aura("Precision"),
            Aura("Malevolence")
        };

        public static bool TryGet(string slug, out WikiTowerCatalogEntry entry)
        {
            for (var i = 0; i < Completed.Length; i++)
            {
                if (Completed[i].Slug == slug)
                {
                    entry = Completed[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        static WikiTowerCatalogEntry Attack(string slug) =>
            new WikiTowerCatalogEntry(slug, WikiTowerCatalogCategory.Attack, WikiTowerImportStatus.ProofSet, true);

        static WikiTowerCatalogEntry Spell(string slug) =>
            new WikiTowerCatalogEntry(slug, WikiTowerCatalogCategory.Spell, WikiTowerImportStatus.ProofSet, true);

        static WikiTowerCatalogEntry Curse(string slug) =>
            new WikiTowerCatalogEntry(slug, WikiTowerCatalogCategory.Curse, WikiTowerImportStatus.GameplayComplete, true);

        static WikiTowerCatalogEntry Aura(string slug) =>
            new WikiTowerCatalogEntry(slug, WikiTowerCatalogCategory.Aura, WikiTowerImportStatus.GameplayComplete, false);
    }
}
