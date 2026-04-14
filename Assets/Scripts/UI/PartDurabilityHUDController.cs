using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// In-match HUD controller that displays a durability row for each registered part
    /// in a <see cref="PartDurabilityTrackerSO"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (Action).
    ///   OnEnable  → subscribes _onDurabilityChanged → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes.
    ///   Refresh() → clears and rebuilds _rowContainer rows from PartDurabilityTrackerSO;
    ///               each row: Texts[0] = partId; Sliders[0] = durability ratio;
    ///               _emptyLabel shown when no parts are registered.
    ///
    /// ── Row format ────────────────────────────────────────────────────────────
    ///   row Texts[0]   → part ID string.
    ///   row Sliders[0] → durability ratio [0, 1].
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • No Update / FixedUpdate — event-driven via VoidGameEvent channel.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one durability panel per HUD canvas.
    ///
    /// Scene wiring:
    ///   _tracker             → PartDurabilityTrackerSO managing all part durabilities.
    ///   _onDurabilityChanged → VoidGameEvent raised by PartDurabilityTrackerSO.
    ///   _rowContainer        → Transform parent for instantiated row prefabs.
    ///   _rowPrefab           → Prefab with ≥1 Text and ≥1 Slider child.
    ///   _emptyLabel          → GameObject shown when no parts are registered.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PartDurabilityHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("PartDurabilityTrackerSO holding all part durability state.")]
        [SerializeField] private PartDurabilityTrackerSO _tracker;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by PartDurabilityTrackerSO on any state change.")]
        [SerializeField] private VoidGameEvent _onDurabilityChanged;

        [Header("UI References (optional)")]
        [Tooltip("Parent Transform for instantiated row prefabs.")]
        [SerializeField] private Transform _rowContainer;

        [Tooltip("Row prefab with ≥1 Text child (Texts[0] = partId) " +
                 "and ≥1 Slider child (Sliders[0] = durability ratio).")]
        [SerializeField] private GameObject _rowPrefab;

        [Tooltip("Shown when no parts are registered; hidden when rows exist.")]
        [SerializeField] private GameObject _emptyLabel;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onDurabilityChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onDurabilityChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Clears stale rows and rebuilds one row per registered part.
        /// Shows <c>_emptyLabel</c> when no parts are registered or tracker is null.
        /// Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            bool hasTracker = _tracker != null;
            bool hasParts   = hasTracker && _tracker.PartCount > 0;

            _emptyLabel?.SetActive(!hasParts);

            if (_rowContainer == null) return;

            // Clear stale rows.
            for (int i = _rowContainer.childCount - 1; i >= 0; i--)
                Destroy(_rowContainer.GetChild(i).gameObject);

            if (!hasTracker || _rowPrefab == null) return;

            foreach (string partId in _tracker.GetPartIds())
            {
                var row    = Instantiate(_rowPrefab, _rowContainer);
                var texts  = row.GetComponentsInChildren<Text>();
                var sliders = row.GetComponentsInChildren<Slider>();

                if (texts.Length > 0)   texts[0].text   = partId;
                if (sliders.Length > 0) sliders[0].value = _tracker.GetDurabilityRatio(partId);
            }
        }

        /// <summary>The assigned <see cref="PartDurabilityTrackerSO"/>. May be null.</summary>
        public PartDurabilityTrackerSO Tracker => _tracker;
    }
}
