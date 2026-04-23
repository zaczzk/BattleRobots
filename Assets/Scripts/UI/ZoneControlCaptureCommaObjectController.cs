using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCommaObjectController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCommaObjectSO _commaObjectSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCommaObjectFormed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _arcLabel;
        [SerializeField] private Text       _commaLabel;
        [SerializeField] private Slider     _arcBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFormedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFormedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCommaObjectFormed?.RegisterCallback(_handleFormedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCommaObjectFormed?.UnregisterCallback(_handleFormedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_commaObjectSO == null) return;
            int bonus = _commaObjectSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_commaObjectSO == null) return;
            _commaObjectSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _commaObjectSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_commaObjectSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_arcLabel != null)
                _arcLabel.text = $"Arcs: {_commaObjectSO.Arcs}/{_commaObjectSO.ArcsNeeded}";

            if (_commaLabel != null)
                _commaLabel.text = $"Commas: {_commaObjectSO.CommaCount}";

            if (_arcBar != null)
                _arcBar.value = _commaObjectSO.ArcProgress;
        }

        public ZoneControlCaptureCommaObjectSO CommaObjectSO => _commaObjectSO;
    }
}
