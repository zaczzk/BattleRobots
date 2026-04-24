using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHomotopyGroupController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHomotopyGroupSO _homotopyGroupSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHomotopyGroupComputed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _loopLabel;
        [SerializeField] private Text       _computeLabel;
        [SerializeField] private Slider     _loopBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComputedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleComputedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHomotopyGroupComputed?.RegisterCallback(_handleComputedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHomotopyGroupComputed?.UnregisterCallback(_handleComputedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_homotopyGroupSO == null) return;
            int bonus = _homotopyGroupSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_homotopyGroupSO == null) return;
            _homotopyGroupSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _homotopyGroupSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_homotopyGroupSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_loopLabel != null)
                _loopLabel.text = $"Loops: {_homotopyGroupSO.Loops}/{_homotopyGroupSO.LoopsNeeded}";

            if (_computeLabel != null)
                _computeLabel.text = $"Computes: {_homotopyGroupSO.ComputeCount}";

            if (_loopBar != null)
                _loopBar.value = _homotopyGroupSO.LoopProgress;
        }

        public ZoneControlCaptureHomotopyGroupSO HomotopyGroupSO => _homotopyGroupSO;
    }
}
