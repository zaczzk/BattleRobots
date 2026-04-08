using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Post-match result panel displayed immediately after a match ends.
    ///
    /// Subscribes to the <c>_onMatchEnd</c> VoidGameEvent SO channel; when raised,
    /// reads the most-recently written <see cref="MatchRecord"/> from SaveSystem to
    /// populate win/loss status, damage stats, round time, and currency earned.
    ///
    /// Architecture rules observed:
    ///   • <c>BattleRobots.UI</c> namespace — no <c>BattleRobots.Physics</c> references.
    ///   • No <c>Update</c> or <c>FixedUpdate</c> override.
    ///   • SO event channels used for all incoming signals.
    ///   • The root <see cref="_panelRoot"/> hides the content until a match ends;
    ///     the MonoBehaviour itself stays enabled so <c>RegisterCallback</c> works.
    ///
    /// Inspector wiring checklist:
    ///   □ _panelRoot            → GameObject  (content container; hidden by Awake)
    ///   □ _winPanel             → GameObject  (shown on win)
    ///   □ _losePanel            → GameObject  (shown on loss/draw)
    ///   □ _durationLabel        → Text        "Time: mm:ss"
    ///   □ _damageDoneLabel      → Text        "Damage: N"
    ///   □ _damageTakenLabel     → Text        "Taken: N"
    ///   □ _currencyEarnedLabel  → Text        "+N cr"
    ///   □ _walletSnapshotLabel  → Text        "Wallet: N cr" (optional)
    ///   □ _difficultyLabel      → Text        difficulty name (optional)
    ///   □ _continueButton       → Button      hides panel and optionally raises an event
    ///   □ _onMatchEnd           → VoidGameEvent SO  (same as MatchManager._onMatchEnd)
    ///   □ _onContinue           → VoidGameEvent SO  (optional; raised when Continue pressed)
    /// </summary>
    public sealed class MatchEndScreenUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Panels")]
        [Tooltip("Root container of the end-screen content. Hidden in Awake; shown by HandleMatchEnd.")]
        [SerializeField] private GameObject _panelRoot;

        [Tooltip("Sub-panel shown only when the player wins.")]
        [SerializeField] private GameObject _winPanel;

        [Tooltip("Sub-panel shown only when the player loses or the match is a draw.")]
        [SerializeField] private GameObject _losePanel;

        [Header("Stat Labels")]
        [Tooltip("Displays the round duration as mm:ss.")]
        [SerializeField] private Text _durationLabel;

        [Tooltip("Displays damage the player robot dealt this match.")]
        [SerializeField] private Text _damageDoneLabel;

        [Tooltip("Displays damage the player robot received this match.")]
        [SerializeField] private Text _damageTakenLabel;

        [Tooltip("Displays currency earned this match (e.g. '+200 cr').")]
        [SerializeField] private Text _currencyEarnedLabel;

        [Tooltip("Optional — displays current wallet balance after reward.")]
        [SerializeField] private Text _walletSnapshotLabel;

        [Tooltip("Optional — displays the difficulty name used for this match.")]
        [SerializeField] private Text _difficultyLabel;

        [Header("Navigation")]
        [Tooltip("Hides _panelRoot and raises _onContinue when clicked.")]
        [SerializeField] private Button _continueButton;

        [Header("Event Channels — In")]
        [Tooltip("Raised by MatchManager at the end of every match. "
               + "Must be the same SO asset as MatchManager._onMatchEnd.")]
        [SerializeField] private VoidGameEvent _onMatchEnd;

        [Header("Event Channels — Out")]
        [Tooltip("Optional. Raised when the Continue button is pressed. "
               + "Wire to a SceneTransitionController or MainMenu event.")]
        [SerializeField] private VoidGameEvent _onContinue;

        // ── Testable state (set by callbacks; usable by tests without a real Canvas) ──

        /// <summary>True after <see cref="HandleMatchEnd"/> shows the panel; false after Continue.</summary>
        public bool IsPanelVisible { get; private set; }

        /// <summary>True if the last <see cref="HandleMatchEnd"/> displayed a win; null before first call.</summary>
        public bool? LastMatchWon { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Hide content immediately; subscribe while the MB is active.
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            _onMatchEnd?.RegisterCallback(HandleMatchEnd);

            if (_continueButton != null)
                _continueButton.onClick.AddListener(HandleContinueClicked);
        }

        private void OnDestroy()
        {
            _onMatchEnd?.UnregisterCallback(HandleMatchEnd);

            if (_continueButton != null)
                _continueButton.onClick.RemoveListener(HandleContinueClicked);
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        /// <summary>
        /// Called when _onMatchEnd fires. Reads the last MatchRecord from disk,
        /// populates all labels, and shows the panel.
        /// </summary>
        public void HandleMatchEnd()
        {
            SaveData data = SaveSystem.Load();
            MatchRecord last = data.matchHistory != null && data.matchHistory.Count > 0
                ? data.matchHistory[data.matchHistory.Count - 1]
                : null;

            Populate(last);

            IsPanelVisible = true;
            if (_panelRoot != null)
                _panelRoot.SetActive(true);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void Populate(MatchRecord record)
        {
            bool won = record != null && record.playerWon;
            LastMatchWon = won;

            if (_winPanel  != null) _winPanel.SetActive(won);
            if (_losePanel != null) _losePanel.SetActive(!won);

            if (record == null) return;

            // Duration — format as mm:ss
            int totalSecs = Mathf.Max(0, Mathf.RoundToInt(record.durationSeconds));
            if (_durationLabel != null)
                _durationLabel.text = $"Time: {totalSecs / 60}:{totalSecs % 60:D2}";

            if (_damageDoneLabel != null)
                _damageDoneLabel.text = $"Damage: {record.damageDone:F0}";

            if (_damageTakenLabel != null)
                _damageTakenLabel.text = $"Taken: {record.damageTaken:F0}";

            if (_currencyEarnedLabel != null)
                _currencyEarnedLabel.text = $"+{record.currencyEarned} cr";

            if (_walletSnapshotLabel != null)
                _walletSnapshotLabel.text = $"Wallet: {record.walletSnapshot} cr";

            if (_difficultyLabel != null)
            {
                bool hasDifficulty = !string.IsNullOrEmpty(record.difficultyName);
                _difficultyLabel.gameObject.SetActive(hasDifficulty);
                if (hasDifficulty)
                    _difficultyLabel.text = record.difficultyName;
            }
        }

        private void HandleContinueClicked()
        {
            IsPanelVisible = false;
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            _onContinue?.Raise();
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_panelRoot == null)
                Debug.LogWarning("[MatchEndScreenUI] _panelRoot not assigned.", this);
            if (_onMatchEnd == null)
                Debug.LogWarning("[MatchEndScreenUI] _onMatchEnd VoidGameEvent not assigned "
                    + "— panel will never show.", this);
        }
#endif
    }
}
