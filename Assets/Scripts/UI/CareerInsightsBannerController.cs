using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Post-session banner that surfaces the single most notable career-stat
    /// change after each match.
    ///
    /// ── Priority order ───────────────────────────────────────────────────────────
    ///   1. New prestige rank   ("New Prestige Rank: {label}!")
    ///   2. New mastery earned  ("Mastery Earned! {N} types mastered.")
    ///   3. New best streak     ("New Best Streak: {N} wins!")
    ///
    ///   If no notable change is detected the banner stays hidden.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _onMatchEndedDelegate.
    ///   OnEnable  → subscribes _onMatchEnded + snapshots current state.
    ///   OnDisable → unsubscribes; hides panel; resets timer.
    ///   Update    → Tick(Time.deltaTime): auto-hides panel when timer expires.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one insights banner per canvas.
    ///   • All UI and data fields are optional.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///   • s_allTypes is a static readonly array — allocated once.
    ///
    /// Scene wiring:
    ///   _winStreak      → WinStreakSO.
    ///   _prestigeSystem → PrestigeSystemSO.
    ///   _mastery        → DamageTypeMasterySO.
    ///   _onMatchEnded   → VoidGameEvent raised at match end.
    ///   _bannerPanel    → Root banner panel.
    ///   _insightLabel   → Text showing the insight message.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CareerInsightsBannerController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Runtime SO tracking current and best win streaks.")]
        [SerializeField] private WinStreakSO _winStreak;

        [Tooltip("Runtime SO tracking the player's prestige rank.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        [Tooltip("Runtime SO tracking per-type damage mastery.")]
        [SerializeField] private DamageTypeMasterySO _mastery;

        // ── Inspector — Event ────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised when the match ends. Triggers insight evaluation.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Root banner panel. Shown when a notable change is detected.")]
        [SerializeField] private GameObject _bannerPanel;

        [Tooltip("Text label displaying the insight message.")]
        [SerializeField] private Text _insightLabel;

        // ── Inspector — Settings ─────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("How long (seconds) to display the banner before auto-hiding.")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 3f;

        // ── Runtime state ────────────────────────────────────────────────────

        private float _displayTimer;
        private int   _prevBestStreak;
        private int   _prevPrestigeCount;
        private int   _prevMasteredCount;

        // ── Static helpers ───────────────────────────────────────────────────

        private static readonly DamageType[] s_allTypes =
        {
            DamageType.Physical,
            DamageType.Energy,
            DamageType.Thermal,
            DamageType.Shock,
        };

        // ── Cached delegate ──────────────────────────────────────────────────

        private Action _onMatchEndedDelegate;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _onMatchEndedDelegate = OnMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_onMatchEndedDelegate);
            SnapshotState();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_onMatchEndedDelegate);
            _bannerPanel?.SetActive(false);
            _displayTimer = 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the auto-hide timer.  Hides the panel when the timer reaches zero.
        /// No-op when the timer is already at or below zero.
        /// </summary>
        public void Tick(float dt)
        {
            if (_displayTimer <= 0f) return;
            _displayTimer -= dt;
            if (_displayTimer <= 0f)
                _bannerPanel?.SetActive(false);
        }

        private void SnapshotState()
        {
            _prevBestStreak    = _winStreak?.BestStreak ?? 0;
            _prevPrestigeCount = _prestigeSystem?.PrestigeCount ?? 0;
            _prevMasteredCount = GetMasteredCount();
        }

        private int GetMasteredCount()
        {
            if (_mastery == null) return 0;
            int count = 0;
            foreach (var t in s_allTypes)
                if (_mastery.IsTypeMastered(t)) count++;
            return count;
        }

        private void OnMatchEnded()
        {
            int    newBestStreak    = _winStreak?.BestStreak ?? 0;
            int    newPrestigeCount = _prestigeSystem?.PrestigeCount ?? 0;
            int    newMastered      = GetMasteredCount();
            string insight          = string.Empty;

            if (newPrestigeCount > _prevPrestigeCount)
            {
                string rankLabel = _prestigeSystem?.GetRankLabel() ?? "Unknown";
                insight = string.Format("New Prestige Rank: {0}!", rankLabel);
            }
            else if (newMastered > _prevMasteredCount)
            {
                insight = string.Format("Mastery Earned! {0} types mastered.", newMastered);
            }
            else if (newBestStreak > _prevBestStreak)
            {
                insight = string.Format("New Best Streak: {0} wins!", newBestStreak);
            }

            if (!string.IsNullOrEmpty(insight))
                ShowBanner(insight);

            SnapshotState();
        }

        private void ShowBanner(string message)
        {
            if (_insightLabel != null) _insightLabel.text = message;
            _bannerPanel?.SetActive(true);
            _displayTimer = _displayDuration;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="WinStreakSO"/>. May be null.</summary>
        public WinStreakSO WinStreak => _winStreak;

        /// <summary>The assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;

        /// <summary>The assigned <see cref="DamageTypeMasterySO"/>. May be null.</summary>
        public DamageTypeMasterySO Mastery => _mastery;

        /// <summary>How long (seconds) the banner displays before auto-hiding.</summary>
        public float DisplayDuration => _displayDuration;

        /// <summary>Remaining display time. 0 or negative means the panel is hidden.</summary>
        public float DisplayTimer => _displayTimer;
    }
}
