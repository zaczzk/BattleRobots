using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureNetController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureNetSO _netSO;
        [SerializeField] private PlayerWallet            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNetConverged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _termLabel;
        [SerializeField] private Text       _convergeLabel;
        [SerializeField] private Slider     _termBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConvergedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleConvergedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNetConverged?.RegisterCallback(_handleConvergedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNetConverged?.UnregisterCallback(_handleConvergedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_netSO == null) return;
            int bonus = _netSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_netSO == null) return;
            _netSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _netSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_netSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_termLabel != null)
                _termLabel.text = $"Terms: {_netSO.Terms}/{_netSO.TermsNeeded}";

            if (_convergeLabel != null)
                _convergeLabel.text = $"Convergences: {_netSO.ConvergenceCount}";

            if (_termBar != null)
                _termBar.value = _netSO.NetProgress;
        }

        public ZoneControlCaptureNetSO NetSO => _netSO;
    }
}
