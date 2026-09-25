namespace GemTD.Core
{
    /// <summary>
    /// Catalog <c>eventKey</c> strings for <see cref="GameEvents.RaisePlaySfx"/>.
    /// Unknown keys warn in the Editor; null clips are a silent no-op.
    /// </summary>
    public static class SfxKeys
    {
        public const string Click = "Click";
        public const string Drop = "Drop";
        public const string PanelClose = "PanelClose";
        public const string TowerShoot = "TowerShoot";
        public const string HitEnemy = "HitEnemy";
        public const string HitEnvironment = "HitEnvironment";
        public const string EnemyDeath = "EnemyDeath";
        public const string PlaceTower = "PlaceTower";
        public const string SellTower = "SellTower";
        public const string Invalid = "Invalid";
        public const string DraftPick = "DraftPick";
        public const string Combine = "Combine";
        public const string ChunkExpand = "ChunkExpand";
        public const string Leak = "Leak";
    }
}
