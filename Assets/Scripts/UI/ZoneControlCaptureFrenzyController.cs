using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that bridges zone-capture events into
    /// <see cref="ZoneControlCaptureFrenzySO"/>, awards the frenzy bonus to the
    /// player's wallet when frenzy starts, and displays the current status.
    ///
    /// <c>_onZoneCaptured</c>: records a capture + Refresh.
    /// <c>_onMatchStarted</c>: resets the frenzy SO + Refresh.
    /// <c>_onFrenzyStarted</c>: awards wallet bonus + Refresh.
    /// <c>_onFrenzyEnded</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFrenzyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFrenzySO _frenzySO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFrenzyStarted;
        [SerializeField] private VoidGameEvent _onFrenzyEnded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _captureLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFrenzyStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate  = HandleZoneCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleFrenzyStartedDelegate = HandleFrenzyStarted;
            _refreshDelegate             = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFrenzyStarted?.RegisterCallback(_handleFrenzyStartedDelegate);
            _onFrenzyEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFrenzyStarted?.UnregisterCallback(_handleFrenzyStartedDelegate);
            _onFrenzyEnded?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _frenzySO?.Tick(Time.time);
        }

        private void HandleZoneCaptured()
        {
            _frenzySO?.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _frenzySO?.Reset();
            Refresh();
        }

        private void HandleFrenzyStarted()
        {
            if (_wallet != null && _frenzySO != null)
                _wallet.AddFunds(_frenzySO.FrenzyBonus);
            Refresh();
        }

        public void Refresh()
        {
            if (_frenzySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _frenzySO.IsFrenzy ? "FRENZY!" : "Normal";

            if (_captureLabel != null)
                _captureLabel.text = $"Captures: {_frenzySO.CaptureCount}";
        }

        public ZoneControlCaptureFrenzySO FrenzySO => _frenzySO;
    }
}
