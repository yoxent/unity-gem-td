using System;
using UnityEngine;
using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Run
{
    [Serializable]
    public struct DifficultyModeRow
    {
        public int FirstSplitWave;
        public int CrossUnlockWave;
        public int TipCap;
        public float SplitP;
        public float HpMultiplier;
    }

    [CreateAssetMenu(menuName = "Gem TD/Run Config", fileName = "RunConfig")]
    public sealed class RunConfig : ScriptableObject
    {
        public int StartingGold = 100;
        public int StartingLives = 20;
        public int EndWaveGold = 50;
        public int EndWave = 50;
        public int InventoryCapacity = 10;
        public int OpenArmCount = 1;
        public int ChunkGridWidth = 13;
        public int ChunkGridHeight = 13;
        public float SocketLockdownSeconds = 0f;
        public int DraftSkipGold = 75;
        public bool SeedHydraRecipeGems = true;
        public GemDefinition[] SeedGems;

        public DifficultyModeRow[] DifficultyModes = CreateDefaultDifficultyModes();

        public int LaneCount => Mathf.Clamp(OpenArmCount, 1, 4);

        public DifficultyModeRow GetDifficultyMode()
        {
            EnsureDifficultyModes();
            return DifficultyModes[LaneCount - 1];
        }

        public int GetFirstSplitWave() => GetDifficultyMode().FirstSplitWave;

        public int GetCrossUnlockWave() => GetDifficultyMode().CrossUnlockWave;

        public int GetTipCap() => GetDifficultyMode().TipCap;

        public float GetSplitP() => GetDifficultyMode().SplitP;

        public float GetHpMultiplier() => GetDifficultyMode().HpMultiplier;

        void OnEnable()
        {
            if (EndWave <= 0)
                EndWave = 50;

            EnsureDifficultyModes();
        }

        void EnsureDifficultyModes()
        {
            if (DifficultyModes != null && DifficultyModes.Length == 4)
                return;

            DifficultyModes = CreateDefaultDifficultyModes();
        }

        static DifficultyModeRow[] CreateDefaultDifficultyModes()
        {
            return new[]
            {
                new DifficultyModeRow
                {
                    FirstSplitWave = 8,
                    CrossUnlockWave = 25,
                    TipCap = 4,
                    SplitP = 0.30f,
                    HpMultiplier = 1.0f
                },
                new DifficultyModeRow
                {
                    FirstSplitWave = 7,
                    CrossUnlockWave = 22,
                    TipCap = 6,
                    SplitP = 0.32f,
                    HpMultiplier = 1.1f
                },
                new DifficultyModeRow
                {
                    FirstSplitWave = 6,
                    CrossUnlockWave = 18,
                    TipCap = 8,
                    SplitP = 0.34f,
                    HpMultiplier = 1.25f
                },
                new DifficultyModeRow
                {
                    FirstSplitWave = 5,
                    CrossUnlockWave = 15,
                    TipCap = 10,
                    SplitP = 0.36f,
                    HpMultiplier = 1.4f
                }
            };
        }
    }
}
