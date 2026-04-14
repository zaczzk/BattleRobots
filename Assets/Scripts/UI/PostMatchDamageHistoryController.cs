using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Post-match panel controller that displays the rolling average damage dealt per
    /// <see cref="DamageType"/> across recent matches.
    ///
    /// ── Display ───────────────────────────────────────────────────────────────────
    ///   <see cref="Refresh"/> builds one row per <see cref="DamageType"/>
    ///   (Physical / Energy / Thermal / Shock) inside <c>_listContainer</c>.
    ///   Each row's first <see cref="Text"/> receives the damage-type name and the
    ///   second Text receives the rolling average rounded to the nearest integer
    ///   ("Avg: N").
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate.
    ///   OnEnable  → subscribes _onMatchEnded and _onHistoryUpdated → Refresh;
    ///               calls Refresh().
    ///   OnDisable → unsubscribes both channels.
    ///   Refresh() → destroys old rows; rebuilds four rows; hides panel when
    ///               HistorySystem is null.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • <see cref="DisallowMultipleComponent"/> — one panel per canvas.
    ///   • All UI fields optional — assign only what the scene has.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────────
    ///   _historySystem     → shared MatchDamageHistorySO (ring-buffer data source).
    ///   _onMatchEnded      → same VoidGameEvent as MatchManager._onMatchEnded.
    ///   _onHistoryUpdated  → same VoidGameEvent as MatchDamageHistorySO._onHistoryUpdated.
    ///   _historyPanel      → root panel GameObject shown when history is non-empty.
    ///   _listContainer     → ScrollRect Content Transform for per-type rows.
    ///   _rowPrefab         → Prefab with ≥ 2 Text children (type name + "Avg: N").
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PostMatchDamageHistoryController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("MatchDamageHistorySO ring-buffer containing per-type damage totals " +
                 "from the last N matches. Leave null to disable the panel.")]
        [SerializeField] private MatchDamageHistorySO _historySystem;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Raised by MatchManager when a match ends. " +
                 "Triggers Refresh() to show the latest rolling averages.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Raised by MatchDamageHistorySO after each AddEntry call. " +
                 "Triggers Refresh() for live updates. Leave null to disable.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("Root panel shown when the history system is assigned and active. " +
                 "Hidden when _historySystem is null. Leave null to skip.")]
        [SerializeField] private GameObject _historyPanel;

        [Tooltip("Parent Transform for the per-type history rows. " +
                 "Requires _rowPrefab. Leave null to skip row generation.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Row prefab instantiated once per DamageType. " +
                 "First Text child receives the damage-type name; " +
                 "second Text child receives the rolling average (\"Avg: N\").")]
        [SerializeField] private GameObject _rowPrefab;

        // ── Private state ─────────────────────────────────────────────────────

        private static readonly DamageType[] s_allTypes =
        {
            DamageType.Physical,
            DamageType.Energy,
            DamageType.Thermal,
            DamageType.Shock,
        };

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
            _onHistoryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
            _onHistoryUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Hides the panel when <c>_historySystem</c> is null; otherwise destroys
        /// existing rows and rebuilds one row per <see cref="DamageType"/> showing
        /// the rolling average damage.
        ///
        /// <para>Safe to call at any time — fully null-safe.</para>
        /// </summary>
        public void Refresh()
        {
            if (_historySystem == null)
            {
                _historyPanel?.SetActive(false);
                return;
            }

            _historyPanel?.SetActive(true);

            if (_listContainer == null || _rowPrefab == null) return;

            // Destroy stale rows.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);

            // Rebuild one row per DamageType.
            for (int i = 0; i < s_allTypes.Length; i++)
            {
                DamageType type    = s_allTypes[i];
                float      average = _historySystem.GetRollingAverage(type);

                GameObject row   = Instantiate(_rowPrefab, _listContainer);
                Text[]     texts = row.GetComponentsInChildren<Text>(true);

                if (texts.Length > 0) texts[0].text = type.ToString();
                if (texts.Length > 1) texts[1].text = $"Avg: {Mathf.RoundToInt(average)}";
            }
        }

        // ── Public properties ─────────────────────────────────────────────────

        /// <summary>The currently assigned <see cref="MatchDamageHistorySO"/>. May be null.</summary>
        public MatchDamageHistorySO HistorySystem => _historySystem;
    }
}
