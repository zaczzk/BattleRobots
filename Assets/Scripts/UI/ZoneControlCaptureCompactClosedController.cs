using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCompactClosedController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCompactClosedSO _compactClosedSO;
        [SerializeField] private PlayerWallet                      _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCompacted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _cupLabel;
        [SerializeField] private Text       _compactLabel;
        [SerializeField] private Slider     _cupBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompactedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompactedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCompacted?.RegisterCallback(_handleCompactedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCompacted?.UnregisterCallback(_handleCompactedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_compactClosedSO == null) return;
            int bonus = _compactClosedSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_compactClosedSO == null) return;
            _compactClosedSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _compactClosedSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_compactClosedSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_cupLabel != null)
                _cupLabel.text = $"Cups: {_compactClosedSO.Cups}/{_compactClosedSO.CupsNeeded}";

            if (_compactLabel != null)
                _compactLabel.text = $"Compactions: {_compactClosedSO.CompactCount}";

            if (_cupBar != null)
                _cupBar.value = _compactClosedSO.CupProgress;
        }

        public ZoneControlCaptureCompactClosedSO CompactClosedSO => _compactClosedSO;
    }
}
