using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that awards a per-capture bonus to the player's wallet
    /// whenever a zone is captured during an active surge, and displays the
    /// surge-capture count and total reward earned.
    ///
    /// <c>_onZoneCaptured</c>: if <c>_surgeSO.IsSurging</c> →
    ///   <see cref="ZoneControlSurgeRewardSO.RecordCaptureDuringSurge"/> +
    ///   wallet credit + Refresh.
    /// <c>_onMatchStarted</c>: resets the SO + Refresh.
    /// <c>_onSurgeRewardAwarded</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlSurgeRewardController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlSurgeRewardSO    _surgeRewardSO;
        [SerializeField] private ZoneControlSurgeDetectorSO  _surgeSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSurgeRewardAwarded;

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
            _onSurgeRewardAwarded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSurgeRewardAwarded?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_surgeRewardSO == null || _surgeSO == null || !_surgeSO.IsSurging)
            {
                Refresh();
                return;
            }

            _surgeRewardSO.RecordCaptureDuringSurge();

            if (_wallet != null)
                _wallet.AddFunds(_surgeRewardSO.RewardPerCapture);

            Refresh();
        }

        private void HandleMatchStarted()
        {
            _surgeRewardSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_surgeRewardSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_capturesLabel != null)
                _capturesLabel.text = $"Surge Captures: {_surgeRewardSO.SurgeCaptures}";

            if (_totalLabel != null)
                _totalLabel.text = $"Surge Reward: {_surgeRewardSO.TotalSurgeReward}";
        }

        public ZoneControlSurgeRewardSO SurgeRewardSO => _surgeRewardSO;
    }
}
