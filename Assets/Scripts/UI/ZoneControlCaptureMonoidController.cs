using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMonoidController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMonoidSO _monoidSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMonoidIdentified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _unitLabel;
        [SerializeField] private Text       _identityLabel;
        [SerializeField] private Slider     _unitBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMonoidIdentifiedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate           = HandlePlayerCaptured;
            _handleBotDelegate              = HandleBotCaptured;
            _handleMatchStartedDelegate     = HandleMatchStarted;
            _handleMonoidIdentifiedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMonoidIdentified?.RegisterCallback(_handleMonoidIdentifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMonoidIdentified?.UnregisterCallback(_handleMonoidIdentifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_monoidSO == null) return;
            int bonus = _monoidSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_monoidSO == null) return;
            _monoidSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _monoidSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_monoidSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_unitLabel != null)
                _unitLabel.text = $"Units: {_monoidSO.Units}/{_monoidSO.UnitsNeeded}";

            if (_identityLabel != null)
                _identityLabel.text = $"Identities: {_monoidSO.IdentityCount}";

            if (_unitBar != null)
                _unitBar.value = _monoidSO.UnitProgress;
        }

        public ZoneControlCaptureMonoidSO MonoidSO => _monoidSO;
    }
}
