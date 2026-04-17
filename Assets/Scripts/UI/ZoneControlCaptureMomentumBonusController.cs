using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that bridges <see cref="ZoneControlMomentumTrackerSO"/> burst
    /// state to per-capture wallet bonuses via <see cref="ZoneControlCaptureMomentumBonusSO"/>.
    ///
    /// <c>_onZoneCaptured</c>: if <c>_momentumSO.IsBurst</c> is true, calls
    /// <c>RecordBurstCapture()</c>, credits wallet, and refreshes.
    /// <c>_onMatchStarted</c>: resets the bonus SO + Refresh.
    /// <c>_onBurstBonusAwarded</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMomentumBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMomentumBonusSO _bonusSO;
        [SerializeField] private ZoneControlMomentumTrackerSO      _momentumSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBurstBonusAwarded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _capturesLabel;
        [SerializeField] private Text       _totalLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBurstBonusAwarded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBurstBonusAwarded?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_bonusSO == null) return;
            if (_momentumSO != null && _momentumSO.IsBurst)
            {
                _bonusSO.RecordBurstCapture();
                _wallet?.AddFunds(_bonusSO.RewardPerBurstCapture);
            }
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bonusSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_bonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_capturesLabel != null)
                _capturesLabel.text = $"Burst Captures: {_bonusSO.BurstCaptureCount}";

            if (_totalLabel != null)
                _totalLabel.text = $"Burst Reward: {_bonusSO.TotalBurstReward}";
        }

        public ZoneControlCaptureMomentumBonusSO BonusSO    => _bonusSO;
        public ZoneControlMomentumTrackerSO       MomentumSO => _momentumSO;
    }
}
