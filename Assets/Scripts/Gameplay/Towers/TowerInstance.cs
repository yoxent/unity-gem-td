using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Towers
{
    public sealed class TowerInstance
    {
        public const int DefaultLevel = 20;

        public Vector2Int Cell { get; }
        public TowerDefinition Def { get; }
        public GemDefinition[] Sockets { get; }
        public float Cooldown { get; set; }
        public TargetingRecipe Targeting { get; set; }
        public int PurchaseCost { get; }
        public int UpgradeSpend { get; set; }
        /// <summary>0-based draft level. Combat stats do not read this.</summary>
        public int LevelIndex { get; set; }
        public int Level { get; private set; }

        public TowerInstance(Vector2Int cell, TowerDefinition def, int purchaseCost = -1)
        {
            Cell = cell;
            Def = def;
            PurchaseCost = purchaseCost >= 0 ? purchaseCost : (def != null ? def.Cost : 0);
            UpgradeSpend = 0;
            LevelIndex = 0;
            Level = DefaultLevel;
            var socketCount = def != null && def.SocketCount > 0 ? def.SocketCount : 1;
            Sockets = new GemDefinition[socketCount];
            Targeting = TargetingRecipe.Default;
        }

        public void SetLevel(int sourceLevel)
        {
            Level = Mathf.Max(1, sourceLevel);
        }

        public bool HasSocketedGems
        {
            get
            {
                for (var i = 0; i < Sockets.Length; i++)
                {
                    if (Sockets[i] != null)
                        return true;
                }

                return false;
            }
        }

        public bool TrySocket(GemDefinition gem, int index, bool allowSocket)
        {
            if (!allowSocket || gem == null || index < 0 || index >= Sockets.Length)
                return false;

            if (Sockets[index] != null)
                return false;

            if (!GemTags.CanSocket(Def, gem))
                return false;

            for (var i = 0; i < Sockets.Length; i++)
            {
                var existing = Sockets[i];
                if (existing != null && existing.Id == gem.Id)
                    return false;
            }

            Sockets[index] = gem;
            return true;
        }

        public bool TryUnsocket(int index, out GemDefinition gem, bool allowSocket, bool ignoreHydraLock = false)
        {
            if (!allowSocket || index < 0 || index >= Sockets.Length)
            {
                gem = null;
                return false;
            }

            gem = Sockets[index];
            if (gem == null)
                return false;

            if (!ignoreHydraLock && EvolutionEvaluator.IsHydraTower(this))
            {
                gem = null;
                return false;
            }

            Sockets[index] = null;
            return true;
        }
    }
}
