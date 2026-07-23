using GemTD.Core;

namespace GemTD.Gameplay.Run
{
    public sealed class RunEconomy
    {
        public int Gold { get; private set; }
        public int Lives { get; private set; }
        public bool IsDefeated { get; private set; }

        public RunEconomy(int gold, int lives)
        {
            Gold = gold;
            Lives = lives;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || amount > Gold)
                return false;

            Gold -= amount;
            GameEvents.RaiseGoldChanged(Gold);
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            Gold += amount;
            GameEvents.RaiseGoldChanged(Gold);
        }

        public void GrantKillGold(int amount) => AddGold(amount);

        public void GrantEndWaveGold(int amount) => AddGold(amount);

        public void RefundFull(int amount) => AddGold(amount);

        public static int ComputeSellRefund(int purchaseCost, int upgradeSpend) =>
            (purchaseCost + upgradeSpend) / 2;

        public void LoseLife(int amount = 1)
        {
            if (amount <= 0 || IsDefeated)
                return;

            Lives -= amount;
            if (Lives < 0)
                Lives = 0;

            GameEvents.RaiseLivesChanged(Lives);

            if (Lives <= 0)
                IsDefeated = true;
        }
    }
}
