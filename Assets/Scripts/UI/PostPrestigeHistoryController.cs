using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that maintains and displays a scrollable timeline of past
    /// prestige events sourced from <see cref="PrestigeHistorySO"/>.
    ///
    /// ── Row layout convention ─────────────────────────────────────────────────
    ///   Each instantiated row prefab must have at least two
    ///   <see cref="UnityEngine.UI.Text"/> children (via GetComponentsInChildren):
    ///     [0] Rank label  — e.g. "Bronze I"
    ///     [1] Count label — e.g. "Prestige 1"
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _onPrestigeDelegate.
    ///   OnEnable  → subscribes _onPrestige → OnPrestige(); Refresh().
    ///   OnDisable → unsubscribes.
    ///   OnPrestige() → AddEntry to PrestigeHistorySO; then Refresh().
    ///   Refresh() → destroys old row GOs; spawns new rows (newest first);
    ///               shows/hides _emptyLabel.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one history panel per canvas.
    ///   • All UI fields are optional — assign only those present in the scene.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _history         → shared PrestigeHistorySO ring-buffer.
    ///   _prestigeSystem  → shared PrestigeSystemSO for current count + rank label.
    ///   _onPrestige      → VoidGameEvent raised by PrestigeSystemSO on each prestige.
    ///   _listContainer   → Transform parent for row prefab instances.
    ///   _rowPrefab       → Prefab with ≥2 child Text components: [0] rank, [1] count.
    ///   _emptyLabel      → GameObject shown when no entries exist; hidden otherwise.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PostPrestigeHistoryController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Ring-buffer SO that stores recent prestige events. " +
                 "Leave null to disable history recording.")]
        [SerializeField] private PrestigeHistorySO _history;

        [Tooltip("Runtime prestige SO. Provides the current count and rank label " +
                 "at the moment of each prestige event. Leave null to skip recording.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        // ── Inspector — Event Channel ─────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised each time the player earns a new prestige rank. " +
                 "Triggers AddEntry + Refresh().")]
        [SerializeField] private VoidGameEvent _onPrestige;

        // ── Inspector — UI (optional) ─────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("Parent transform under which row prefabs are instantiated.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab with ≥2 child Text components: [0] rank label, [1] 'Prestige N'.")]
        [SerializeField] private GameObject _rowPrefab;

        [Tooltip("GameObject shown when the history ring-buffer is empty.")]
        [SerializeField] private GameObject _emptyLabel;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _onPrestigeDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onPrestigeDelegate = OnPrestige;
        }

        private void OnEnable()
        {
            _onPrestige?.RegisterCallback(_onPrestigeDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPrestige?.UnregisterCallback(_onPrestigeDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Called when the player achieves a new prestige rank.
        /// Records the event in <see cref="_history"/> then rebuilds the row list.
        /// Silently returns when either <see cref="_history"/> or
        /// <see cref="_prestigeSystem"/> is null.
        /// </summary>
        private void OnPrestige()
        {
            if (_history == null || _prestigeSystem == null) return;

            int    count     = _prestigeSystem.PrestigeCount;
            string rankLabel = _prestigeSystem.GetRankLabel();
            _history.AddEntry(count, rankLabel);
            Refresh();
        }

        /// <summary>
        /// Rebuilds the history row list from the current <see cref="_history"/> buffer
        /// (newest entry first).  Shows <see cref="_emptyLabel"/> when the buffer is
        /// empty or null.  Fully null-safe on all optional UI refs.
        /// </summary>
        public void Refresh()
        {
            bool hasEntries = _history != null && _history.Count > 0;

            _emptyLabel?.SetActive(!hasEntries);

            if (_listContainer != null)
            {
                // Destroy stale rows.
                for (int i = _listContainer.childCount - 1; i >= 0; i--)
                    Destroy(_listContainer.GetChild(i).gameObject);

                if (hasEntries && _rowPrefab != null)
                {
                    for (int i = 0; i < _history.Count; i++)
                    {
                        PrestigeHistoryEntry? entry = _history.GetEntry(i);
                        if (!entry.HasValue) continue;

                        GameObject row  = Instantiate(_rowPrefab, _listContainer);
                        Text[]     texts = row.GetComponentsInChildren<Text>(true);

                        if (texts.Length > 0)
                            texts[0].text = entry.Value.rankLabel;
                        if (texts.Length > 1)
                            texts[1].text = $"Prestige {entry.Value.prestigeCount}";
                    }
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="PrestigeHistorySO"/>. May be null.</summary>
        public PrestigeHistorySO History => _history;

        /// <summary>The assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;
    }
}
