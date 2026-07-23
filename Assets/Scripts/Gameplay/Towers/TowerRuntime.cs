using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Towers
{
    public sealed class TowerRuntime
    {
        public Vector2Int Cell { get; }
        public TowerDefinition Def { get; }
        public GemDefinition[] Sockets { get; }
        public float Cooldown { get; set; }
        public TargetingMode TargetingMode { get; set; }
        public float OutgoingDamageMultiplier { get; set; }
        public int PurchaseCost { get; }
        public int UpgradeSpend { get; set; }

        public TowerRuntime(Vector2Int cell, TowerDefinition def)
        {
            Cell = cell;
            Def = def;
            PurchaseCost = def != null ? def.Cost : 0;
            UpgradeSpend = 0;
            OutgoingDamageMultiplier = 1f;
            var socketCount = def != null && def.SocketCount > 0 ? def.SocketCount : 1;
            Sockets = new GemDefinition[socketCount];
            TargetingMode = TargetingMode.First;
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

            for (var i = 0; i < Sockets.Length; i++)
            {
                var existing = Sockets[i];
                if (existing != null && existing.Id == gem.Id)
                    return false;
            }

            Sockets[index] = gem;
            return true;
        }

        public bool TryUnsocket(int index, out GemDefinition gem, bool allowSocket)
        {
            if (!allowSocket || index < 0 || index >= Sockets.Length)
            {
                gem = null;
                return false;
            }

            gem = Sockets[index];
            if (gem == null)
                return false;

            Sockets[index] = null;
            return true;
        }
    }
}
