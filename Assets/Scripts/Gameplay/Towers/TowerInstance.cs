using UnityEngine;
using GemTD.Gameplay.Combat;
using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Towers
{
    public sealed class TowerInstance
    {
        public const int DefaultLevel = 1;
        public const int MaxLevel = 10;

        public Vector2Int Cell { get; }
        public TowerDefinition Def { get; }
        public GemInstance[] Sockets { get; }
        public float Cooldown { get; set; }
        public TargetingRecipe Targeting { get; set; }
        public int PurchaseCost { get; }
        public int UpgradeSpend { get; set; }
        public int FireGeneration { get; private set; }
        public Vector3 LastAimPoint { get; private set; }
        /// <summary>Resolved attack/cast interval for the current volley. Animation stretches clips to fill this window.</summary>
        public float CurrentFireInterval { get; set; }
        /// <summary>0–1 normalized action point used when imported event timing is disabled. Default 1 (end of interval).</summary>
        public float StrikeNormalized { get; set; } = 1f;
        /// <summary>When true, combat waits exclusively for OnCombatAction("execute").</summary>
        public bool UsesAnimationActionEvent { get; set; }
        /// <summary>World Y added to pad-top muzzle so bolts/warp/nova start at the character, not the tile.</summary>
        public float MuzzleLocalY { get; set; }
        int _levelIndex;

        /// <summary>0-based draft progression. Combat uses Level, which is this index + 1 clamped to 1–10 (role Levels[] snapshot).</summary>
        public int LevelIndex
        {
            get => _levelIndex;
            set
            {
                _levelIndex = Mathf.Max(0, value);
                SetLevel(_levelIndex + 1);
            }
        }
        public int Level { get; private set; }

        public TowerInstance(Vector2Int cell, TowerDefinition def, int purchaseCost = -1)
        {
            Cell = cell;
            Def = def;
            PurchaseCost = purchaseCost >= 0 ? purchaseCost : (def != null ? def.Cost : 0);
            UpgradeSpend = 0;
            _levelIndex = 0;
            Level = DefaultLevel;
            var socketCount = def != null && def.SocketCount > 0 ? def.SocketCount : 1;
            Sockets = new GemInstance[socketCount];
            Targeting = TargetingRecipe.Default;
        }

        public void SetLevel(int sourceLevel)
        {
            Level = Mathf.Clamp(sourceLevel, DefaultLevel, MaxLevel);
        }

        public bool HasSocketedGems
        {
            get
            {
                for (var i = 0; i < Sockets.Length; i++)
                {
                    if (!Sockets[i].IsEmpty)
                        return true;
                }

                return false;
            }
        }

        public bool TrySocket(GemInstance gem, int index, bool allowSocket)
        {
            if (!allowSocket || gem.IsEmpty || index < 0 || index >= Sockets.Length)
                return false;

            if (!Sockets[index].IsEmpty)
                return false;

            if (!GemTags.CanSocket(Def, gem))
                return false;

            for (var i = 0; i < Sockets.Length; i++)
            {
                var existing = Sockets[i];
                if (existing.IsEmpty)
                    continue;
                if (gem.Id != GemId.None && existing.Id == gem.Id)
                    return false;
            }

            Sockets[index] = gem;
            return true;
        }

        public bool TrySocket(GemDefinition gem, int index, bool allowSocket)
        {
            return TrySocket(GemInstance.FromDefinition(gem), index, allowSocket);
        }

        public bool TryUnsocket(int index, out GemInstance gem, bool allowSocket, bool ignoreHydraLock = false)
        {
            if (!allowSocket || index < 0 || index >= Sockets.Length)
            {
                gem = default;
                return false;
            }

            gem = Sockets[index];
            if (gem.IsEmpty)
                return false;

            if (!ignoreHydraLock && EvolutionEvaluator.IsHydraTower(this))
            {
                gem = default;
                return false;
            }

            Sockets[index] = default;
            return true;
        }

        public void NotifyFired(Vector3 aimPoint)
        {
            FireGeneration++;
            LastAimPoint = aimPoint;
        }
    }
}
