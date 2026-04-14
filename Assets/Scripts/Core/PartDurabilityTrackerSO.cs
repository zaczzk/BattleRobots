using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks per-part durability for an assembled robot.
    ///
    /// Each part is registered by its ID string with a maximum durability.  Damage is
    /// applied per-part, independently.  The SO fires <see cref="_onDurabilityChanged"/>
    /// on every mutation so HUD controllers can refresh reactively.
    ///
    /// ── Usage ───────────────────────────────────────────────────────────────────
    ///   1. Call <see cref="Clear"/> then <see cref="AddPart"/> for each equipped part
    ///      when a loadout is applied (match start or loadout change).
    ///   2. Call <see cref="ApplyDamage"/> from physics/damage events.
    ///   3. Call <see cref="Reset"/> to restore all parts to full durability.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Dictionary look-ups on the hot path are O(1) average; only string keys.
    ///   - Event fired on data mutations; no Update/FixedUpdate allocation.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ PartDurabilityTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/PartDurabilityTracker")]
    public sealed class PartDurabilityTrackerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after every AddPart, ApplyDamage, Reset, and Clear call. " +
                 "Wire to PartDurabilityHUDController.Refresh().")]
        [SerializeField] private VoidGameEvent _onDurabilityChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private struct Entry
        {
            public float max;
            public float current;
        }

        private readonly Dictionary<string, Entry> _parts = new Dictionary<string, Entry>();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of parts currently registered.</summary>
        public int PartCount => _parts.Count;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _parts.Clear();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a part with the given <paramref name="partId"/> and maximum durability.
        /// If the part is already registered its durability is reset to <paramref name="maxDurability"/>.
        /// Values below 1 are clamped to 1.
        /// Fires <see cref="_onDurabilityChanged"/>.
        /// </summary>
        public void AddPart(string partId, float maxDurability)
        {
            if (string.IsNullOrEmpty(partId)) return;

            float max = Mathf.Max(1f, maxDurability);
            _parts[partId] = new Entry { max = max, current = max };
            _onDurabilityChanged?.Raise();
        }

        /// <summary>
        /// Reduces the durability of <paramref name="partId"/> by <paramref name="amount"/>,
        /// clamped to zero.
        /// No-op when the part is not registered or amount is ≤ 0.
        /// Fires <see cref="_onDurabilityChanged"/>.
        /// </summary>
        public void ApplyDamage(string partId, float amount)
        {
            if (string.IsNullOrEmpty(partId) || amount <= 0f) return;
            if (!_parts.TryGetValue(partId, out Entry entry)) return;

            entry.current = Mathf.Max(0f, entry.current - amount);
            _parts[partId] = entry;
            _onDurabilityChanged?.Raise();
        }

        /// <summary>
        /// Returns the current durability of <paramref name="partId"/>.
        /// Returns 0 when the part is not registered.
        /// </summary>
        public float GetDurability(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return 0f;
            return _parts.TryGetValue(partId, out Entry e) ? e.current : 0f;
        }

        /// <summary>
        /// Returns the maximum durability of <paramref name="partId"/>.
        /// Returns 0 when the part is not registered.
        /// </summary>
        public float GetMaxDurability(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return 0f;
            return _parts.TryGetValue(partId, out Entry e) ? e.max : 0f;
        }

        /// <summary>
        /// Returns the durability ratio [0, 1] for <paramref name="partId"/>.
        /// Returns 0 when the part is not registered or max is zero.
        /// </summary>
        public float GetDurabilityRatio(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return 0f;
            if (!_parts.TryGetValue(partId, out Entry e) || e.max <= 0f) return 0f;
            return Mathf.Clamp01(e.current / e.max);
        }

        /// <summary>
        /// Returns true when the registered part's current durability has reached zero.
        /// Returns false when the part is not registered.
        /// </summary>
        public bool IsDestroyed(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return false;
            return _parts.TryGetValue(partId, out Entry e) && e.current <= 0f;
        }

        /// <summary>
        /// Restores every registered part to its maximum durability.
        /// Fires <see cref="_onDurabilityChanged"/>.
        /// </summary>
        public void Reset()
        {
            var keys = new List<string>(_parts.Keys);
            foreach (var key in keys)
            {
                Entry e = _parts[key];
                e.current   = e.max;
                _parts[key] = e;
            }
            _onDurabilityChanged?.Raise();
        }

        /// <summary>
        /// Removes all registered parts.
        /// Fires <see cref="_onDurabilityChanged"/>.
        /// </summary>
        public void Clear()
        {
            _parts.Clear();
            _onDurabilityChanged?.Raise();
        }

        /// <summary>
        /// Returns an enumerator over currently registered part IDs.
        /// Intended for UI row-building — not called on the hot path.
        /// </summary>
        public IEnumerable<string> GetPartIds() => _parts.Keys;
    }
}
