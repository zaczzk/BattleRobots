using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHomotopyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHomotopySO _homotopySO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHomotopyComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _deformLabel;
        [SerializeField] private Text       _homotopyLabel;
        [SerializeField] private Slider     _deformBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleHomotopyDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleHomotopyDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHomotopyComplete?.RegisterCallback(_handleHomotopyDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHomotopyComplete?.UnregisterCallback(_handleHomotopyDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_homotopySO == null) return;
            int bonus = _homotopySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_homotopySO == null) return;
            _homotopySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _homotopySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_homotopySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_deformLabel != null)
                _deformLabel.text = $"Deforms: {_homotopySO.Deformations}/{_homotopySO.DeformationsNeeded}";

            if (_homotopyLabel != null)
                _homotopyLabel.text = $"Homotopies: {_homotopySO.HomotopyCount}";

            if (_deformBar != null)
                _deformBar.value = _homotopySO.HomotopyProgress;
        }

        public ZoneControlCaptureHomotopySO HomotopySO => _homotopySO;
    }
}
