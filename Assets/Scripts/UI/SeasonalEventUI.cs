using System.Collections;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the current seasonal event: event name, player score, time remaining,
    /// and the reward tier list. Provides Claim buttons per tier.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   On <c>OnEnable</c> the panel rebuilds and starts a coroutine that refreshes
    ///   the countdown label once per second (avoids per-frame string allocation).
    ///   On <c>OnDisable</c> the coroutine stops.
    ///
    /// ── Architecture ─────────────────────────────────────────────────────────
    ///   • BattleRobots.UI — no Physics references.
    ///   • Reads <see cref="SeasonalEventProgressSO"/> for runtime state.
    ///   • Tier rows are instantiated from <c>_tierRowPrefab</c> into <c>_tierContainer</c>.
    ///
    /// Scene setup:
    ///   1. Assign _progress (SeasonalEventProgressSO SO asset).
    ///   2. Assign _wallet (PlayerWallet SO asset) for tier claims.
    ///   3. Set up Text/panel references and the _tierRowPrefab prefab.
    ///   4. Wire VoidGameEventListener on this GO →
    ///      _progress._onScoreChanged → SeasonalEventUI.Refresh
    /// </summary>
    public sealed class SeasonalEventUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [SerializeField] private SeasonalEventProgressSO _progress;
        [SerializeField] private PlayerWallet _wallet;

        [Header("Labels")]
        [SerializeField] private Text _eventNameLabel;
        [SerializeField] private Text _scoreLabel;
        [SerializeField] private Text _countdownLabel;

        [Header("Panels")]
        [Tooltip("Shown when the seasonal event is currently active.")]
        [SerializeField] private GameObject _activePanel;

        [Tooltip("Shown when no event is active (off-season).")]
        [SerializeField] private GameObject _inactivePanel;

        [Header("Tier List")]
        [SerializeField] private Transform _tierContainer;
        [SerializeField] private SeasonalEventTierRowUI _tierRowPrefab;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Coroutine _countdownCoroutine;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            Refresh();
            _countdownCoroutine = StartCoroutine(CountdownCoroutine());
        }

        private void OnDisable()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the entire panel: event name, score, tier rows, active/inactive panels.
        /// Called from <c>OnEnable</c> and wired to the <c>_onScoreChanged</c> VoidGameEvent.
        /// </summary>
        public void Refresh()
        {
            bool isActive = _progress != null && _progress.IsActive;

            if (_activePanel  != null) _activePanel.SetActive(isActive);
            if (_inactivePanel != null) _inactivePanel.SetActive(!isActive);

            if (_progress == null) return;

            var def = _progress.Definition;

            if (_eventNameLabel != null)
                _eventNameLabel.text = def != null ? def.EventName : string.Empty;

            if (_scoreLabel != null)
                _scoreLabel.text = _progress.Score.ToString();

            RebuildTierRows();
            RefreshCountdown();
        }

        /// <summary>
        /// Called by each tier row's Claim button onClick event.
        /// Pass the zero-based tier index as the argument.
        /// </summary>
        public void OnClaimTierClicked(int tierIndex)
        {
            if (_progress == null) return;
            _progress.TryClaimTier(tierIndex, _wallet);
            Refresh();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void RebuildTierRows()
        {
            if (_tierContainer == null || _tierRowPrefab == null || _progress == null) return;

            // Destroy existing rows.
            for (int i = _tierContainer.childCount - 1; i >= 0; i--)
                Destroy(_tierContainer.GetChild(i).gameObject);

            var def = _progress.Definition;
            if (def == null) return;

            for (int i = 0; i < def.TierCount; i++)
            {
                SeasonalEventRewardTier tier = def.GetTier(i);
                SeasonalEventTierRowUI row =
                    Instantiate(_tierRowPrefab, _tierContainer);
                row.Setup(
                    tierName:       tier.tierName,
                    requiredScore:  tier.requiredScore,
                    rewardCurrency: tier.rewardCurrency,
                    isReached:      _progress.IsTierReached(i),
                    isClaimed:      _progress.IsTierClaimed(i));

                // Wire claim button to this UI's handler.
                int capturedIndex = i;
                var claimButton = row.GetComponentInChildren<UnityEngine.UI.Button>(includeInactive: true);
                if (claimButton != null)
                    claimButton.onClick.AddListener(() => OnClaimTierClicked(capturedIndex));
            }
        }

        private void RefreshCountdown()
        {
            if (_countdownLabel == null || _progress == null) return;

            var def = _progress.Definition;
            if (def == null || !def.IsActive())
            {
                _countdownLabel.text = string.Empty;
                return;
            }

            System.TimeSpan remaining = def.TimeRemaining();
            _countdownLabel.text = FormatTimeSpan(remaining);
        }

        // Coroutine refreshes the countdown label once per second.
        // WaitForSeconds is allocated once here and reused — no per-frame alloc.
        private IEnumerator CountdownCoroutine()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
                RefreshCountdown();
                yield return wait;
            }
        }

        /// <summary>
        /// Formats a <see cref="System.TimeSpan"/> into a compact human-readable string.
        /// Public static for unit-test coverage without MonoBehaviour instantiation.
        /// </summary>
        public static string FormatTimeSpan(System.TimeSpan ts)
        {
            if (ts <= System.TimeSpan.Zero) return "Ended";
            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}d {ts.Hours}h";
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{ts.Minutes}m {ts.Seconds}s";
        }
    }
}
