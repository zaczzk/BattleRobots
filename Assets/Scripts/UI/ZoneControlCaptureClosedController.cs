using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureClosedController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureClosedSO _closedSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onClosed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _homTermLabel;
        [SerializeField] private Text       _closeLabel;
        [SerializeField] private Slider     _homTermBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleClosedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleClosedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onClosed?.RegisterCallback(_handleClosedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onClosed?.UnregisterCallback(_handleClosedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_closedSO == null) return;
            int bonus = _closedSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_closedSO == null) return;
            _closedSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _closedSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_closedSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_homTermLabel != null)
                _homTermLabel.text = $"Hom-Terms: {_closedSO.HomTerms}/{_closedSO.HomTermsNeeded}";

            if (_closeLabel != null)
                _closeLabel.text = $"Closures: {_closedSO.CloseCount}";

            if (_homTermBar != null)
                _homTermBar.value = _closedSO.HomTermProgress;
        }

        public ZoneControlCaptureClosedSO ClosedSO => _closedSO;
    }
}
