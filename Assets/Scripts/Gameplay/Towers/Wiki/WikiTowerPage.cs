namespace GemTD.Gameplay.Towers
{
    public struct WikiTowerLevelSnapshot
    {
        public int SourceLevel;
        public string Damage;
        public string TowerRadius;
        public string SplashRadius;
        public int ProjectileCount;
        public int ChainCount;
        public int ForkCount;
        public string AttackTime;
        public string AttackSpeed;
        public string CastTime;
        public string CastSpeed;
        public string ReservationPercent;
        public string FireInterval;
    }

    public sealed class WikiTowerPage
    {
        public string Slug;
        public string DisplayName;
        public string Description;
        public string CategoryName;
        public string CategoryFolder;
        public string StatusLabel;
        public bool InTowerCatalog;
        public string Tags;
        public int Cost;
        public int SocketCount;
        public string RoleKind;
        public string AimMode;
        public string DeliveryPattern;
        public string Mix;
        public string SpreadDegrees;
        public string SequentialIntervalSeconds;
        public int FirstSourceLevel;
        public int LastSourceLevel;
        public WikiTowerLevelSnapshot First;
        public WikiTowerLevelSnapshot Last;
        public string[] BaseModifiers;
        public string[] EffectLines;
        public string[] PayloadLines;
    }
}
