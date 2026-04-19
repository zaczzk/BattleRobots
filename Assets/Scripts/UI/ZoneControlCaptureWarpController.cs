using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureWarpController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureWarpSO _warpSO;
        [SerializeField, Min(0)] private int              _baseCaptureReward = 50;
        [SerializeField] private PlayerWalletSO           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onWarpLevelChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _warpLabel;
        [SerializeField] private Text       _streakLabel;
        [SerializeField] private Slider     _warpBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleWarpChangedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleWarpChangedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onWarpLevelChanged?.RegisterCallback(_handleWarpChangedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onWarpLevelChanged?.UnregisterCallback(_handleWarpChangedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_warpSO == null) return;
            _warpSO.RecordPlayerCapture();
            if (_baseCaptureReward > 0)
                _wallet?.AddFunds(_warpSO.ComputeWarpBonus(_baseCaptureReward));
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_warpSO == null) return;
            _warpSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _warpSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_warpSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_warpLabel != null)
                _warpLabel.text = $"Warp: {_warpSO.WarpMultiplier:F1}\u00d7";

            if (_streakLabel != null)
                _streakLabel.text = $"Streak: {_warpSO.CurrentStreak}";

            if (_warpBar != null)
                _warpBar.value = _warpSO.WarpProgress;
        }

        public ZoneControlCaptureWarpSO WarpSO => _warpSO;
    }
}
