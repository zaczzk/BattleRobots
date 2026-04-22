using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCurryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCurrySO _currySO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCurryComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _argLabel;
        [SerializeField] private Text       _curryLabel;
        [SerializeField] private Slider     _argBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCurryCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCurryCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCurryComplete?.RegisterCallback(_handleCurryCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCurryComplete?.UnregisterCallback(_handleCurryCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_currySO == null) return;
            int bonus = _currySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_currySO == null) return;
            _currySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _currySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_currySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_argLabel != null)
                _argLabel.text = $"Args: {_currySO.Args}/{_currySO.ArgsNeeded}";

            if (_curryLabel != null)
                _curryLabel.text = $"Curries: {_currySO.CurryCount}";

            if (_argBar != null)
                _argBar.value = _currySO.ArgProgress;
        }

        public ZoneControlCaptureCurrySO CurrySO => _currySO;
    }
}
