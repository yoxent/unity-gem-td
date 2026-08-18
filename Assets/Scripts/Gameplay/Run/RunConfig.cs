using UnityEngine;
using GemTD.Gameplay.Gems;

namespace GemTD.Gameplay.Run
{
    [CreateAssetMenu(menuName = "Gem TD/Run Config", fileName = "RunConfig")]
    public sealed class RunConfig : ScriptableObject
    {
        public int StartingGold = 100;
        public int StartingLives = 20;
        public int EndWaveGold = 25;
        public int InventoryCapacity = 10;
        public int OpenArmCount = 1;
        public int ChunkGridWidth = 13;
        public int ChunkGridHeight = 13;
        public float SocketLockdownSeconds = 3f;
        public int DraftSkipGold = 75;
        public bool SeedHydraRecipeGems = true;
        public GemDefinition[] SeedGems;
    }
}
