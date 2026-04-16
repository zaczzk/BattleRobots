using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that maintains a ring buffer of per-match reward amounts.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="AddReward(int)"/> at match end to record the earned reward.
    ///   The buffer prunes the oldest entry once <see cref="Capacity"/> is exceeded.
    ///   <see cref="GetAverageReward"/> returns the mean reward across all entries.
    ///   Call <see cref="Reset"/> to clear the history.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset or restore from bootstrapper.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlRewardHistory.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlRewardHistory", order = 55)]
    public sealed class ZoneControlRewardHistorySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of match rewards to retain.")]
        [Min(1)]
        [SerializeField] private int _capacity = 5;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after each AddReward call.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly List<int> _rewards = new List<int>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of reward entries currently stored.</summary>
        public int EntryCount => _rewards.Count;

        /// <summary>Maximum number of entries retained before oldest is pruned.</summary>
        public int Capacity => _capacity;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the reward amount at <paramref name="index"/>.
        /// Returns 0 for out-of-range indices.
        /// </summary>
        public int GetReward(int index)
        {
            if (index < 0 || index >= _rewards.Count) return 0;
            return _rewards[index];
        }

        /// <summary>
        /// Appends <paramref name="amount"/> (clamped to ≥ 0) to the history.
        /// Prunes the oldest entry when the buffer exceeds <see cref="Capacity"/>.
        /// Fires <see cref="_onHistoryUpdated"/>.
        /// </summary>
        public void AddReward(int amount)
        {
            _rewards.Add(Mathf.Max(0, amount));
            int cap = Mathf.Max(1, _capacity);
            while (_rewards.Count > cap)
                _rewards.RemoveAt(0);
            _onHistoryUpdated?.Raise();
        }

        /// <summary>
        /// Returns the mean reward across all stored entries.
        /// Returns 0 when the history is empty.
        /// </summary>
        public float GetAverageReward()
        {
            if (_rewards.Count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < _rewards.Count; i++) sum += _rewards[i];
            return sum / _rewards.Count;
        }

        /// <summary>
        /// Clears the reward history silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _rewards.Clear();
        }

        private void OnValidate()
        {
            _capacity = Mathf.Max(1, _capacity);
        }
    }
}
