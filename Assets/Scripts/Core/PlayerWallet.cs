using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime-only SO representing the player's currency.
    /// Mutated only via AddFunds / Deduct so event channels fire consistently.
    /// Reset() must be called at game start to clear stale editor values.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Economy/PlayerWallet", order = 0)]
    public sealed class PlayerWallet : ScriptableObject
    {
        [SerializeField] private int _startingBalance = 500;

        /// <summary>Current spendable balance. Read-only from outside the wallet.</summary>
        public int Balance { get; private set; }

        /// <summary>Fired whenever the balance changes. Payload = new balance.</summary>
        [SerializeField] private IntGameEvent _onBalanceChanged;

        /// <summary>Call once at game/match start to initialise from the starting value.</summary>
        public void Reset()
        {
            Balance = _startingBalance;
            _onBalanceChanged?.Raise(Balance);
        }

        /// <summary>
        /// Adds funds unconditionally (e.g. match win reward).
        /// </summary>
        /// <param name="amount">Must be &gt; 0.</param>
        public void AddFunds(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[PlayerWallet] AddFunds called with non-positive amount: {amount}");
                return;
            }

            Balance += amount;
            _onBalanceChanged?.Raise(Balance);
        }

        /// <summary>
        /// Deducts funds if sufficient balance exists.
        /// </summary>
        /// <param name="amount">Must be &gt; 0.</param>
        /// <returns>True if the deduction succeeded; false if insufficient funds.</returns>
        public bool Deduct(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[PlayerWallet] Deduct called with non-positive amount: {amount}");
                return false;
            }

            if (Balance < amount)
                return false;

            Balance -= amount;
            _onBalanceChanged?.Raise(Balance);
            return true;
        }

        /// <summary>
        /// Restores balance from a persisted snapshot (called by SaveSystem after load).
        /// Does NOT fire the event — caller is responsible.
        /// </summary>
        internal void LoadSnapshot(int balance)
        {
            Balance = balance;
            _onBalanceChanged?.Raise(Balance);
        }
    }
}
