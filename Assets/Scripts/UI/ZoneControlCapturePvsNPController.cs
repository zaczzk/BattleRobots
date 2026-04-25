using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePvsNPController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePvsNPSO _pvsNPSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPvsNPReduced;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _reductionLabel;
        [SerializeField] private Text       _reduceLabel;
        [SerializeField] private Slider     _reductionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleReducedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleReducedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPvsNPReduced?.RegisterCallback(_handleReducedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPvsNPReduced?.UnregisterCallback(_handleReducedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_pvsNPSO == null) return;
            int bonus = _pvsNPSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_pvsNPSO == null) return;
            _pvsNPSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pvsNPSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_pvsNPSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_reductionLabel != null)
                _reductionLabel.text = $"Reductions: {_pvsNPSO.Reductions}/{_pvsNPSO.ReductionsNeeded}";

            if (_reduceLabel != null)
                _reduceLabel.text = $"Reductions: {_pvsNPSO.ReductionCount}";

            if (_reductionBar != null)
                _reductionBar.value = _pvsNPSO.ReductionProgress;
        }

        public ZoneControlCapturePvsNPSO PvsNPSO => _pvsNPSO;
    }
}
