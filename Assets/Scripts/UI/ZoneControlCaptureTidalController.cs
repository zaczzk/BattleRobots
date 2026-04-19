using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTidalController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTidalSO _tidalSO;
        [SerializeField] private PlayerWalletSO            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMajorityAchieved;
        [SerializeField] private VoidGameEvent _onMajorityLost;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTidalCycle;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _cycleLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMajorityAchievedDelegate;
        private Action _handleMajorityLostDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTidalCycleDelegate;

        private void Awake()
        {
            _handleMajorityAchievedDelegate = HandleMajorityAchieved;
            _handleMajorityLostDelegate     = HandleMajorityLost;
            _handleMatchStartedDelegate     = HandleMatchStarted;
            _handleTidalCycleDelegate       = HandleTidalCycle;
        }

        private void OnEnable()
        {
            _onMajorityAchieved?.RegisterCallback(_handleMajorityAchievedDelegate);
            _onMajorityLost?.RegisterCallback(_handleMajorityLostDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTidalCycle?.RegisterCallback(_handleTidalCycleDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMajorityAchieved?.UnregisterCallback(_handleMajorityAchievedDelegate);
            _onMajorityLost?.UnregisterCallback(_handleMajorityLostDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTidalCycle?.UnregisterCallback(_handleTidalCycleDelegate);
        }

        private void HandleMajorityAchieved()
        {
            if (_tidalSO == null) return;
            _tidalSO.RecordLeadState(true);
            Refresh();
        }

        private void HandleMajorityLost()
        {
            if (_tidalSO == null) return;
            _tidalSO.RecordLeadState(false);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _tidalSO?.Reset();
            Refresh();
        }

        private void HandleTidalCycle()
        {
            if (_tidalSO == null) return;
            _wallet?.AddFunds(_tidalSO.BonusPerCycle);
            Refresh();
        }

        public void Refresh()
        {
            if (_tidalSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_cycleLabel != null)
                _cycleLabel.text = $"Tidal Cycles: {_tidalSO.CycleCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Tidal Bonus: {_tidalSO.TotalBonusAwarded}";
        }

        public ZoneControlCaptureTidalSO TidalSO => _tidalSO;
    }
}
