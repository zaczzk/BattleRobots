using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// ScriptableObject catalog of <see cref="ZoneControlPowerUpSO"/> entries used
    /// to randomly select which power-up type spawns during a match.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="SelectRandom"/> picks a random entry from <c>_powerUps</c>,
    ///   stores the index in <see cref="LastSelectedIndex"/>, and fires
    ///   <c>_onSpawnRequested</c>.  Returns <c>null</c> when the array is empty.
    ///   <see cref="GetPowerUp"/> provides safe, bounds-checked index access.
    ///   <see cref="Reset"/> clears the last-selected index silently.
    ///   <c>OnEnable</c> calls <see cref="Reset"/> so runtime state never leaks
    ///   across play-mode sessions.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets via <c>OnEnable</c>.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlPowerUpCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlPowerUpCatalog", order = 67)]
    public sealed class ZoneControlPowerUpCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Power-Up Entries")]
        [SerializeField] private ZoneControlPowerUpSO[] _powerUps;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised each time a random power-up is selected.")]
        [SerializeField] private VoidGameEvent _onSpawnRequested;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _lastSelectedIndex = -1;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of entries in the catalog.</summary>
        public int EntryCount => _powerUps?.Length ?? 0;

        /// <summary>Index of the most recently selected power-up, or -1 when none.</summary>
        public int LastSelectedIndex => _lastSelectedIndex;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="ZoneControlPowerUpSO"/> at <paramref name="index"/>,
        /// or <c>null</c> when out of range or the array is null.
        /// </summary>
        public ZoneControlPowerUpSO GetPowerUp(int index)
        {
            if (_powerUps == null || index < 0 || index >= _powerUps.Length)
                return null;
            return _powerUps[index];
        }

        /// <summary>
        /// Picks a random entry from <c>_powerUps</c>, records the index, fires
        /// <c>_onSpawnRequested</c>, and returns the selected SO.
        /// Returns <c>null</c> when the array is null or empty.  Zero allocation.
        /// </summary>
        public ZoneControlPowerUpSO SelectRandom()
        {
            if (_powerUps == null || _powerUps.Length == 0)
                return null;

            _lastSelectedIndex = Random.Range(0, _powerUps.Length);
            _onSpawnRequested?.Raise();
            return _powerUps[_lastSelectedIndex];
        }

        /// <summary>
        /// Resets <see cref="LastSelectedIndex"/> to -1 silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _lastSelectedIndex = -1;
        }

        private void OnValidate()
        {
            if (_powerUps != null)
            {
                for (int i = 0; i < _powerUps.Length; i++)
                {
                    if (_powerUps[i] == null)
                        Debug.LogWarning($"[ZoneControlPowerUpCatalogSO] Entry [{i}] is null.", this);
                }
            }
        }
    }
}
