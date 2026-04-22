using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRegisterController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRegisterSO _registerSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRegisterWritten;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _wordLabel;
        [SerializeField] private Text       _writeLabel;
        [SerializeField] private Slider     _wordBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleWrittenDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleWrittenDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRegisterWritten?.RegisterCallback(_handleWrittenDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRegisterWritten?.UnregisterCallback(_handleWrittenDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_registerSO == null) return;
            int bonus = _registerSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_registerSO == null) return;
            _registerSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _registerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_registerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_wordLabel != null)
                _wordLabel.text = $"Words: {_registerSO.Words}/{_registerSO.WordsNeeded}";

            if (_writeLabel != null)
                _writeLabel.text = $"Writes: {_registerSO.WriteCount}";

            if (_wordBar != null)
                _wordBar.value = _registerSO.WordProgress;
        }

        public ZoneControlCaptureRegisterSO RegisterSO => _registerSO;
    }
}
