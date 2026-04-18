using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBalanceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBalanceSO _balanceSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBalancePositive;
        [SerializeField] private VoidGameEvent _onBalanceNegative;
        [SerializeField] private VoidGameEvent _onBalanceChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _balanceLabel;
        [SerializeField] private GameObject _positiveBadge;
        [SerializeField] private GameObject _negativeBadge;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRefreshDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleRefreshDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBalancePositive?.RegisterCallback(_handleRefreshDelegate);
            _onBalanceNegative?.RegisterCallback(_handleRefreshDelegate);
            _onBalanceChanged?.RegisterCallback(_handleRefreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBalancePositive?.UnregisterCallback(_handleRefreshDelegate);
            _onBalanceNegative?.UnregisterCallback(_handleRefreshDelegate);
            _onBalanceChanged?.UnregisterCallback(_handleRefreshDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_balanceSO == null) return;
            _balanceSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_balanceSO == null) return;
            _balanceSO.RecordBotRecapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _balanceSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_balanceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            int balance = _balanceSO.Balance;
            if (_balanceLabel != null)
                _balanceLabel.text = balance >= 0 ? $"Balance: +{balance}" : $"Balance: {balance}";

            bool positive = _balanceSO.IsPositive;
            _positiveBadge?.SetActive(positive);
            _negativeBadge?.SetActive(!positive && balance < 0);
        }

        public ZoneControlCaptureBalanceSO BalanceSO => _balanceSO;
    }
}
