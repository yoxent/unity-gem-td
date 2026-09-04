using System;
using GemTD.Core;

namespace GemTD.Gameplay.Run
{
    public sealed class RunEconomy
    {
        readonly Action<int> _onGoldEarned;

        public int Gold { get; private set; }
        public int Lives { get; private set; }
        public bool IsDefeated { get; private set; }
        public int LastEndWaveGold { get; private set; }

        public RunEconomy(int gold, int lives, Action<int> onGoldEarned = null)
        {
            Gold = gold;
            Lives = lives;
            _onGoldEarned = onGoldEarned;
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

        public void GrantKillGold(int amount) => EarnGold(amount);

        public void GrantEndWaveGold(int amount)
        {
            if (amount > 0)
                LastEndWaveGold = amount;
            EarnGold(amount);
        }

        public void GrantDraftSkipGold(int amount) => EarnGold(amount);

        public void RefundFull(int amount) => AddGold(amount);

        public static int ComputeSellRefund(int purchaseCost, int upgradeSpend) =>
            purchaseCost + upgradeSpend;

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

        void EarnGold(int amount)
        {
            if (amount <= 0)
                return;

            Gold += amount;
            _onGoldEarned?.Invoke(amount);
            GameEvents.RaiseGoldChanged(Gold);
        }
    }
}
