using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBountyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBountySO _bountySO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBountyClaimed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bountyLabel;
        [SerializeField] private Text       _earnedLabel;
        [SerializeField] private Slider     _bountyBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _refreshDelegate              = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBountyClaimed?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBountyClaimed?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_bountySO == null) return;
            _bountySO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_bountySO == null) return;
            int earned = _bountySO.ClaimPlayerCapture();
            _wallet?.AddFunds(earned);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_bountySO == null) return;
            _bountySO.ClaimBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bountySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_bountySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bountyLabel != null)
                _bountyLabel.text = $"Bounty: {_bountySO.CurrentBounty}";

            if (_earnedLabel != null)
                _earnedLabel.text = $"Earned: {_bountySO.TotalBountyEarned}";

            if (_bountyBar != null)
                _bountyBar.value = _bountySO.BountyProgress;
        }

        public ZoneControlCaptureBountySO BountySO => _bountySO;
    }
}
