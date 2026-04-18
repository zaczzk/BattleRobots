using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchSummaryBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchSummaryBonusSO _summaryBonusSO;
        [SerializeField] private PlayerWallet                    _wallet;
        [SerializeField] private ZoneControlCaptureQuotaSO       _captureQuotaSO;
        [SerializeField] private ZoneControlCaptureAccuracySO    _accuracySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onBonusApplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private Text       _totalLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onBonusApplied?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onBonusApplied?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleMatchStarted()
        {
            _summaryBonusSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            if (_summaryBonusSO == null) return;

            int   captures   = _captureQuotaSO?.CaptureCount ?? 0;
            float efficiency  = _accuracySO?.Accuracy         ?? 0f;
            int   combos      = 0;

            int bonus = _summaryBonusSO.ApplySummaryBonus(captures, efficiency, combos);
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        public void Refresh()
        {
            if (_summaryBonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bonusLabel != null)
                _bonusLabel.text = $"Summary Bonus: {_summaryBonusSO.LastBonus}";

            if (_totalLabel != null)
                _totalLabel.text = $"Total: {_summaryBonusSO.TotalBonus}";
        }

        public ZoneControlMatchSummaryBonusSO SummaryBonusSO => _summaryBonusSO;
    }
}
