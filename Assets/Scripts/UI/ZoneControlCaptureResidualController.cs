using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureResidualController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureResidualSO _residualSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onResiduated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _residualLabel;
        [SerializeField] private Text       _residuateCountLabel;
        [SerializeField] private Slider     _residualBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleResiduatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate      = HandlePlayerCaptured;
            _handleBotDelegate         = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleResiduatedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onResiduated?.RegisterCallback(_handleResiduatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onResiduated?.UnregisterCallback(_handleResiduatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_residualSO == null) return;
            int bonus = _residualSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_residualSO == null) return;
            _residualSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _residualSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_residualSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_residualLabel != null)
                _residualLabel.text = $"Residuals: {_residualSO.Residuals}/{_residualSO.ResidualsNeeded}";

            if (_residuateCountLabel != null)
                _residuateCountLabel.text = $"Residuations: {_residualSO.ResiduateCount}";

            if (_residualBar != null)
                _residualBar.value = _residualSO.ResiduateProgress;
        }

        public ZoneControlCaptureResidualSO ResidualSO => _residualSO;
    }
}
