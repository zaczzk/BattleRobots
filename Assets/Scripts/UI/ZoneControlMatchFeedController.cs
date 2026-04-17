using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the in-match event feed driven by
    /// <see cref="ZoneControlMatchFeedSO"/>.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Subscribes to four event channels (zone captured, power-up collected,
    ///   bot capture, victory achieved) and appends a timestamped entry to the
    ///   feed SO on each.
    ///   On <c>_onMatchStarted</c>: resets the feed SO and refreshes the list.
    ///   On <c>_onFeedUpdated</c>: refreshes the displayed list.
    ///   <see cref="Refresh"/> destroys stale rows and rebuilds newest-first.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one feed controller per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchFeedController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchFeedSO _feedSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onPowerUpCollected;
        [SerializeField] private VoidGameEvent _onBotCapture;
        [SerializeField] private VoidGameEvent _onVictoryAchieved;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFeedUpdated;

        [Header("UI References (optional)")]
        [SerializeField] private Transform  _listContainer;
        [SerializeField] private GameObject _rowPrefab;
        [SerializeField] private Text       _emptyLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleZoneCapturedDelegate;
        private Action _handlePowerUpCollectedDelegate;
        private Action _handleBotCaptureDelegate;
        private Action _handleVictoryAchievedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleZoneCapturedDelegate    = HandleZoneCaptured;
            _handlePowerUpCollectedDelegate = HandlePowerUpCollected;
            _handleBotCaptureDelegate      = HandleBotCapture;
            _handleVictoryAchievedDelegate = HandleVictoryAchieved;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _refreshDelegate               = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onPowerUpCollected?.RegisterCallback(_handlePowerUpCollectedDelegate);
            _onBotCapture?.RegisterCallback(_handleBotCaptureDelegate);
            _onVictoryAchieved?.RegisterCallback(_handleVictoryAchievedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFeedUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onPowerUpCollected?.UnregisterCallback(_handlePowerUpCollectedDelegate);
            _onBotCapture?.UnregisterCallback(_handleBotCaptureDelegate);
            _onVictoryAchieved?.UnregisterCallback(_handleVictoryAchievedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFeedUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>Records a Zone Captured event in the feed.</summary>
        public void HandleZoneCaptured()
            => AddEntry(ZoneControlFeedEventType.ZoneCaptured, "Zone Captured");

        /// <summary>Records a Power-Up Collected event in the feed.</summary>
        public void HandlePowerUpCollected()
            => AddEntry(ZoneControlFeedEventType.PowerUpCollected, "Power-Up Collected");

        /// <summary>Records a Bot Capture event in the feed.</summary>
        public void HandleBotCapture()
            => AddEntry(ZoneControlFeedEventType.BotCapture, "Bot Captured Zone");

        /// <summary>Records a Victory Achieved event in the feed.</summary>
        public void HandleVictoryAchieved()
            => AddEntry(ZoneControlFeedEventType.VictoryAchieved, "Victory Achieved!");

        /// <summary>Resets the feed SO and refreshes the display.</summary>
        public void HandleMatchStarted()
        {
            _feedSO?.Reset();
            Refresh();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void AddEntry(ZoneControlFeedEventType type, string message)
        {
            _feedSO?.AddEntry(Time.time, type, message);
        }

        /// <summary>
        /// Rebuilds the displayed list from the feed SO, newest entry first.
        /// Hides the panel when <see cref="_feedSO"/> is null.
        /// </summary>
        public void Refresh()
        {
            if (_feedSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            // Destroy stale rows
            if (_listContainer != null)
            {
                for (int i = _listContainer.childCount - 1; i >= 0; i--)
                    Object.Destroy(_listContainer.GetChild(i).gameObject);
            }

            bool isEmpty = _feedSO.EntryCount == 0;
            if (_emptyLabel != null)
                _emptyLabel.gameObject.SetActive(isEmpty);

            if (isEmpty || _listContainer == null || _rowPrefab == null)
                return;

            var entries = _feedSO.Entries;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                var row   = Object.Instantiate(_rowPrefab, _listContainer);
                var texts = row.GetComponentsInChildren<Text>(true);
                if (texts.Length > 0) texts[0].text = $"{entry.Timestamp:F1}s";
                if (texts.Length > 1) texts[1].text = entry.Message;
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound feed SO (may be null).</summary>
        public ZoneControlMatchFeedSO FeedSO => _feedSO;
    }
}
