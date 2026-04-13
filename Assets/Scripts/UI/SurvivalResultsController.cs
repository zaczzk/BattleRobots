using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that shows a post-survival results panel when the survival
    /// run ends (<c>_onSurvivalEnded</c> fires).
    ///
    /// ── Data displayed ────────────────────────────────────────────────────────
    ///   • Waves completed  — <see cref="WaveManagerSO.CurrentWave"/>
    ///   • Bots defeated    — <see cref="WaveManagerSO.TotalBotsDefeated"/>
    ///   • Credits earned   — wallet delta since wave 1 started
    ///   • New best badge   — shown when <see cref="WaveManagerSO.CurrentWave"/>
    ///                        exceeds the all-time best recorded at run start
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   OnEnable  → subscribe both channels; Hide().
    ///   _onWaveStarted fires (wave 1 only)
    ///             → snapshot <see cref="_walletBalanceAtStart"/> +
    ///               <see cref="_bestWaveAtStart"/>.
    ///   _onSurvivalEnded fires → <see cref="ShowResults"/>:
    ///             → <see cref="_resultsPanel"/>.SetActive(true)
    ///             → set label texts; show / hide <see cref="_newBestBadge"/>.
    ///   OnDisable → unsubscribe.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Assign _waveManager → the WaveManagerSO asset.
    ///   2. Assign _playerWallet → the PlayerWallet SO (leave null to omit
    ///      credits-earned display).
    ///   3. Assign _onSurvivalEnded → WaveManagerSO's survival-ended channel.
    ///   4. Assign _onWaveStarted   → WaveManagerSO's wave-started channel
    ///      (same channel as used by WaveController) so the wallet/best-wave
    ///      snapshot is taken the moment wave 1 begins.
    ///   5. Assign _resultsPanel → the root panel shown after survival ends.
    ///   6. Assign optional Text labels: _wavesCompletedText, _botsDefeatedText,
    ///      _creditsEarnedText.
    ///   7. Assign optional _newBestBadge → a badge GameObject activated when
    ///      the player surpasses their previous best wave.
    ///
    /// ── ARCHITECTURE RULES ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Delegates cached in Awake; zero alloc after initialisation.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • All fields optional and null-guarded.
    ///
    /// Create via Assets ▶ Add Component ▶ BattleRobots.UI ▶ SurvivalResultsController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SurvivalResultsController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime SO that holds wave state (CurrentWave, TotalBotsDefeated, BestWave). " +
                 "Assign the same asset used by WaveController and SurvivalMatchManager.")]
        [SerializeField] private WaveManagerSO _waveManager;

        [Header("Economy")]
        [Tooltip("Player wallet SO. Balance is snapshot at wave-1 start so credits " +
                 "earned this run can be calculated. Leave null to skip credits display.")]
        [SerializeField] private PlayerWallet _playerWallet;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised when the survival run ends. → ShowResults().")]
        [SerializeField] private VoidGameEvent _onSurvivalEnded;

        [Tooltip("VoidGameEvent raised when any wave starts. Used to snapshot the " +
                 "wallet balance and all-time best wave at the beginning of wave 1.")]
        [SerializeField] private VoidGameEvent _onWaveStarted;

        [Header("UI Refs (all optional)")]
        [Tooltip("Root panel shown after survival ends. Hidden on OnEnable.")]
        [SerializeField] private GameObject _resultsPanel;

        [Tooltip("Text showing the wave the player reached (e.g. 'Wave 5').")]
        [SerializeField] private Text _wavesCompletedText;

        [Tooltip("Text showing total bots defeated this run (e.g. '12 bots defeated').")]
        [SerializeField] private Text _botsDefeatedText;

        [Tooltip("Text showing credits earned this run (e.g. '+150 credits').")]
        [SerializeField] private Text _creditsEarnedText;

        [Tooltip("Badge GameObject activated when the player sets a new best wave.")]
        [SerializeField] private GameObject _newBestBadge;

        // ── Runtime snapshot state ─────────────────────────────────────────────

        private int _walletBalanceAtStart;
        private int _bestWaveAtStart;

        // ── Cached delegates (allocated once in Awake — zero alloc thereafter) ─

        private Action _showResultsDelegate;
        private Action _onWaveStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _showResultsDelegate   = ShowResults;
            _onWaveStartedDelegate = OnSurvivalWaveStarted;
        }

        private void OnEnable()
        {
            _onSurvivalEnded?.RegisterCallback(_showResultsDelegate);
            _onWaveStarted?.RegisterCallback(_onWaveStartedDelegate);
            Hide();
        }

        private void OnDisable()
        {
            _onSurvivalEnded?.UnregisterCallback(_showResultsDelegate);
            _onWaveStarted?.UnregisterCallback(_onWaveStartedDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Populates and shows the post-survival results panel.
        /// Called automatically when <c>_onSurvivalEnded</c> fires; may also be
        /// called manually (e.g. from a test or a debug button).
        /// </summary>
        public void ShowResults()
        {
            _resultsPanel?.SetActive(true);

            if (_waveManager != null)
            {
                if (_wavesCompletedText != null)
                    _wavesCompletedText.text = "Wave " + _waveManager.CurrentWave;

                if (_botsDefeatedText != null)
                    _botsDefeatedText.text = _waveManager.TotalBotsDefeated + " bots defeated";

                if (_newBestBadge != null)
                    _newBestBadge.SetActive(_waveManager.CurrentWave > _bestWaveAtStart);
            }

            if (_creditsEarnedText != null && _playerWallet != null)
            {
                int earned = Mathf.Max(0, _playerWallet.Balance - _walletBalanceAtStart);
                _creditsEarnedText.text = "+" + earned + " credits";
            }
        }

        /// <summary>Hides <see cref="_resultsPanel"/>. No-op when panel ref is null.</summary>
        public void Hide()
        {
            _resultsPanel?.SetActive(false);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Called when <c>_onWaveStarted</c> fires.
        /// Snapshots the wallet balance and all-time best wave only when wave 1
        /// starts (i.e. at the very beginning of a survival run), so subsequent
        /// wave-start events do not clobber the snapshot.
        /// </summary>
        private void OnSurvivalWaveStarted()
        {
            if (_waveManager == null || _waveManager.CurrentWave != 1) return;

            _walletBalanceAtStart = _playerWallet != null ? _playerWallet.Balance : 0;
            _bestWaveAtStart      = _waveManager.BestWave;
        }
    }
}
