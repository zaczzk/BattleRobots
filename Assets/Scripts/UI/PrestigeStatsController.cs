using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the player's current prestige rank, prestige count,
    /// and overall prestige progress in a compact read-only stats panel sourced from
    /// <see cref="PrestigeSystemSO"/>.
    ///
    /// ── Difference from <see cref="PrestigeController"/> ─────────────────────────
    ///   <see cref="PrestigeController"/> owns the prestige-button flow and contextual
    ///   hint text.  <c>PrestigeStatsController</c> is a lightweight read-only display
    ///   intended for career stats screens — it adds a <see cref="Slider"/> progress bar
    ///   showing overall prestige advancement and a plain count label, without any
    ///   button or info-text logic.
    ///
    /// ── Display ──────────────────────────────────────────────────────────────────
    ///   When <see cref="_prestigeSystem"/> is null:
    ///   • <c>_rankLabel</c>    → "—"
    ///   • <c>_countLabel</c>   → "Prestige 0"
    ///   • <c>_progressBar</c>  → 0
    ///
    ///   When assigned:
    ///   • <c>_rankLabel</c>    → <see cref="PrestigeSystemSO.GetRankLabel"/> (e.g. "Gold I")
    ///   • <c>_countLabel</c>   → "Prestige N" (e.g. "Prestige 7")
    ///   • <c>_progressBar</c>  → Clamp01(PrestigeCount / MaxPrestigeRank)
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (zero alloc after init).
    ///   OnEnable  → subscribes _onPrestige → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes.
    ///   Refresh() → reads PrestigeSystemSO; updates labels and bar. Null-safe.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one prestige stats panel per canvas.
    ///   • All UI fields are optional — assign only those present in the scene.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _prestigeSystem → shared PrestigeSystemSO.
    ///   _onPrestige     → same VoidGameEvent as PrestigeSystemSO._onPrestige.
    ///   _rankLabel      → Text that receives GetRankLabel() (e.g. "Gold I").
    ///   _countLabel     → Text that receives "Prestige N".
    ///   _progressBar    → Slider in [0, 1] tracking prestige count / max rank.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrestigeStatsController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime prestige SO. Provides prestige count, max rank, and rank label. " +
                 "Leave null to show default zero-state values.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        // ── Inspector — Event Channel ─────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised each time the player earns a new prestige rank. " +
                 "Triggers Refresh(). Leave null to disable auto-refresh.")]
        [SerializeField] private VoidGameEvent _onPrestige;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("Text label that receives the human-readable rank (e.g. 'Gold I'). " +
                 "Shows '—' when _prestigeSystem is null.")]
        [SerializeField] private Text _rankLabel;

        [Tooltip("Text label that receives 'Prestige N' (e.g. 'Prestige 3'). " +
                 "Shows 'Prestige 0' when _prestigeSystem is null.")]
        [SerializeField] private Text _countLabel;

        [Tooltip("Slider bar whose value is PrestigeCount / MaxPrestigeRank in [0, 1]. " +
                 "Shows 0 when _prestigeSystem is null.")]
        [SerializeField] private Slider _progressBar;

        // ── Cached state ──────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPrestige?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPrestige?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current prestige state and updates all wired UI elements.
        /// Null <see cref="_prestigeSystem"/> results in "—" rank, "Prestige 0" count,
        /// and 0 progress bar — fully null-safe.
        /// </summary>
        public void Refresh()
        {
            if (_prestigeSystem == null)
            {
                if (_rankLabel   != null) _rankLabel.text    = "—";
                if (_countLabel  != null) _countLabel.text   = "Prestige 0";
                if (_progressBar != null) _progressBar.value = 0f;
                return;
            }

            int   count    = _prestigeSystem.PrestigeCount;
            int   maxRank  = _prestigeSystem.MaxPrestigeRank;
            float progress = maxRank > 0
                ? Mathf.Clamp01((float)count / maxRank)
                : 0f;

            if (_rankLabel   != null) _rankLabel.text    = _prestigeSystem.GetRankLabel();
            if (_countLabel  != null) _countLabel.text   = $"Prestige {count}";
            if (_progressBar != null) _progressBar.value = progress;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;
    }
}
