using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFusionController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFusionSO _fusionSO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFusion;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chargeLabel;
        [SerializeField] private Text       _fusionCountLabel;
        [SerializeField] private Slider     _chargeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFusionDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFusionDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFusion?.RegisterCallback(_handleFusionDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFusion?.UnregisterCallback(_handleFusionDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_fusionSO == null) return;
            int prev  = _fusionSO.FusionCount;
            int bonus = _fusionSO.RecordPlayerCapture();
            if (_fusionSO.FusionCount > prev)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_fusionSO == null) return;
            _fusionSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _fusionSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_fusionSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chargeLabel != null)
                _chargeLabel.text = $"Charge: {_fusionSO.BotChargeCount}/{_fusionSO.ChargeThreshold}";

            if (_fusionCountLabel != null)
                _fusionCountLabel.text = $"Fusions: {_fusionSO.FusionCount}";

            if (_chargeBar != null)
                _chargeBar.value = _fusionSO.ChargeProgress;
        }

        public ZoneControlCaptureFusionSO FusionSO => _fusionSO;
    }
}
