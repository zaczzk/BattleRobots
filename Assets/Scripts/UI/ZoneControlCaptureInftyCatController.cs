using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureInftyCatController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureInftyCatSO _inftyCatSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onInftyCatComposed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _homotopyLabel;
        [SerializeField] private Text       _composeLabel;
        [SerializeField] private Slider     _homotopyBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComposedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleComposedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onInftyCatComposed?.RegisterCallback(_handleComposedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onInftyCatComposed?.UnregisterCallback(_handleComposedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_inftyCatSO == null) return;
            int bonus = _inftyCatSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_inftyCatSO == null) return;
            _inftyCatSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _inftyCatSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_inftyCatSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_homotopyLabel != null)
                _homotopyLabel.text = $"Homotopies: {_inftyCatSO.Homotopies}/{_inftyCatSO.HomotopiesNeeded}";

            if (_composeLabel != null)
                _composeLabel.text = $"Compositions: {_inftyCatSO.ComposeCount}";

            if (_homotopyBar != null)
                _homotopyBar.value = _inftyCatSO.HomotopyProgress;
        }

        public ZoneControlCaptureInftyCatSO InftyCatSO => _inftyCatSO;
    }
}
