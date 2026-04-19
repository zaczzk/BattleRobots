using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureChronoController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureChronoSO _chronoSO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onChronoRecord;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bestGapLabel;
        [SerializeField] private Text       _chronoBonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleChronoRecordDelegate;

        private void Awake()
        {
            _handleCaptureDelegate       = HandleZoneCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleChronoRecordDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onChronoRecord?.RegisterCallback(_handleChronoRecordDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onChronoRecord?.UnregisterCallback(_handleChronoRecordDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_chronoSO == null) return;
            int bonus = _chronoSO.RecordCapture(Time.time);
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _chronoSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_chronoSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bestGapLabel != null)
                _bestGapLabel.text = $"Best Gap: {_chronoSO.BestGap:F1}s";

            if (_chronoBonusLabel != null)
                _chronoBonusLabel.text = $"Chrono Bonus: {_chronoSO.TotalChronoBonus}";
        }

        public ZoneControlCaptureChronoSO ChronoSO => _chronoSO;
    }
}
