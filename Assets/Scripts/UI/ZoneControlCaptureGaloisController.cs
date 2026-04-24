using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGaloisController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGaloisSO _galoisSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGaloisConnected;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _closureLabel;
        [SerializeField] private Text       _connectLabel;
        [SerializeField] private Slider     _closureBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConnectedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleConnectedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGaloisConnected?.RegisterCallback(_handleConnectedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGaloisConnected?.UnregisterCallback(_handleConnectedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_galoisSO == null) return;
            int bonus = _galoisSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_galoisSO == null) return;
            _galoisSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _galoisSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_galoisSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_closureLabel != null)
                _closureLabel.text = $"Closures: {_galoisSO.Closures}/{_galoisSO.ClosuresNeeded}";

            if (_connectLabel != null)
                _connectLabel.text = $"Connections: {_galoisSO.ConnectionCount}";

            if (_closureBar != null)
                _closureBar.value = _galoisSO.ClosureProgress;
        }

        public ZoneControlCaptureGaloisSO GaloisSO => _galoisSO;
    }
}
