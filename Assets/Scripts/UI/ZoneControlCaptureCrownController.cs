using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCrownController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCrownSO _crownSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCrownCoronated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _jewelLabel;
        [SerializeField] private Text       _coronationLabel;
        [SerializeField] private Slider     _jewelBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCoronatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCoronatedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCrownCoronated?.RegisterCallback(_handleCoronatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCrownCoronated?.UnregisterCallback(_handleCoronatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_crownSO == null) return;
            int bonus = _crownSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_crownSO == null) return;
            _crownSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _crownSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_crownSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_jewelLabel != null)
                _jewelLabel.text = $"Jewels: {_crownSO.Jewels}/{_crownSO.JewelsNeeded}";

            if (_coronationLabel != null)
                _coronationLabel.text = $"Coronations: {_crownSO.CoronationCount}";

            if (_jewelBar != null)
                _jewelBar.value = _crownSO.JewelProgress;
        }

        public ZoneControlCaptureCrownSO CrownSO => _crownSO;
    }
}
