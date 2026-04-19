using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchPivotController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchPivotSO _pivotSO;
        [SerializeField] private PlayerWalletSO          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMajorityAchieved;
        [SerializeField] private VoidGameEvent _onMajorityLost;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPivotAchieved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pivotLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMajorityAchievedDelegate;
        private Action _handleMajorityLostDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePivotAchievedDelegate;

        private void Awake()
        {
            _handleMajorityAchievedDelegate = HandleMajorityAchieved;
            _handleMajorityLostDelegate     = HandleMajorityLost;
            _handleMatchStartedDelegate     = HandleMatchStarted;
            _handlePivotAchievedDelegate    = HandlePivotAchieved;
        }

        private void OnEnable()
        {
            _onMajorityAchieved?.RegisterCallback(_handleMajorityAchievedDelegate);
            _onMajorityLost?.RegisterCallback(_handleMajorityLostDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPivotAchieved?.RegisterCallback(_handlePivotAchievedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMajorityAchieved?.UnregisterCallback(_handleMajorityAchievedDelegate);
            _onMajorityLost?.UnregisterCallback(_handleMajorityLostDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPivotAchieved?.UnregisterCallback(_handlePivotAchievedDelegate);
        }

        private void HandleMajorityAchieved()
        {
            if (_pivotSO == null) return;
            _pivotSO.RecordLeadState(true);
            Refresh();
        }

        private void HandleMajorityLost()
        {
            if (_pivotSO == null) return;
            _pivotSO.RecordLeadState(false);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pivotSO?.Reset();
            Refresh();
        }

        private void HandlePivotAchieved()
        {
            if (_pivotSO == null) return;
            _wallet?.AddFunds(_pivotSO.BonusPerPivot);
            Refresh();
        }

        public void Refresh()
        {
            if (_pivotSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pivotLabel != null)
                _pivotLabel.text = $"Pivots: {_pivotSO.PivotCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Pivot Bonus: {_pivotSO.TotalBonusAwarded}";
        }

        public ZoneControlMatchPivotSO PivotSO => _pivotSO;
    }
}
