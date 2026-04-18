using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureQuotaController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureQuotaSO _quotaSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onQuotaMet;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _quotaLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleQuotaMetDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleQuotaMetDelegate     = HandleQuotaMet;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onQuotaMet?.RegisterCallback(_handleQuotaMetDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onQuotaMet?.UnregisterCallback(_handleQuotaMetDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_quotaSO == null) return;
            _quotaSO.RecordCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _quotaSO?.Reset();
            Refresh();
        }

        private void HandleQuotaMet()
        {
            _wallet?.AddFunds(_quotaSO?.BonusOnCompletion ?? 0);
            Refresh();
        }

        public void Refresh()
        {
            if (_quotaSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_quotaLabel != null)
                _quotaLabel.text = $"Quota: {_quotaSO.CaptureCount}/{_quotaSO.QuotaTarget}";

            if (_bonusLabel != null)
                _bonusLabel.text = _quotaSO.QuotaMet
                    ? $"Bonus: {_quotaSO.BonusOnCompletion}"
                    : "Bonus: Pending";
        }

        public ZoneControlCaptureQuotaSO QuotaSO => _quotaSO;
    }
}
