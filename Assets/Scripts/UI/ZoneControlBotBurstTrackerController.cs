using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives <see cref="ZoneControlBotBurstTrackerSO"/> lifecycle
    /// and displays whether the bot is in burst mode.
    ///
    /// <c>_onBotZoneCaptured</c> (IntGameEvent): calls <c>RecordBotCapture(Time.time)</c> + Refresh.
    /// <c>_onMatchStarted</c>: resets the tracker + Refresh.
    /// <c>_onBotBurstStarted/_onBotBurstEnded</c>: Refresh.
    /// <see cref="Update"/> ticks the tracker each frame (window pruning).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlBotBurstTrackerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlBotBurstTrackerSO _botBurstSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private IntGameEvent  _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBotBurstStarted;
        [SerializeField] private VoidGameEvent _onBotBurstEnded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _captureLabel;
        [SerializeField] private GameObject _panel;

        private Action<int> _handleBotZoneCapturedDelegate;
        private Action      _handleMatchStartedDelegate;
        private Action      _refreshDelegate;

        private void Awake()
        {
            _handleBotZoneCapturedDelegate = HandleBotZoneCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _refreshDelegate               = Refresh;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBotBurstStarted?.RegisterCallback(_refreshDelegate);
            _onBotBurstEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBotBurstStarted?.UnregisterCallback(_refreshDelegate);
            _onBotBurstEnded?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _botBurstSO?.Tick(Time.time);
        }

        private void HandleBotZoneCaptured(int zoneIndex)
        {
            _botBurstSO?.RecordBotCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _botBurstSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_botBurstSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _botBurstSO.IsBotBursting ? "BOT BURST!" : "Normal";

            if (_captureLabel != null)
                _captureLabel.text = $"Bot Caps: {_botBurstSO.BotCaptureCount}";
        }

        public ZoneControlBotBurstTrackerSO BotBurstSO => _botBurstSO;
    }
}
