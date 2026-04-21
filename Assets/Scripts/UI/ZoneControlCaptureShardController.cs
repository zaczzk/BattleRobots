using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureShardController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureShardSO _shardSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onShardCrystallized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _shardLabel;
        [SerializeField] private Text       _crystalLabel;
        [SerializeField] private Slider     _shardBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleShardCrystallizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate              = HandlePlayerCaptured;
            _handleBotDelegate                 = HandleBotCaptured;
            _handleMatchStartedDelegate        = HandleMatchStarted;
            _handleShardCrystallizedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onShardCrystallized?.RegisterCallback(_handleShardCrystallizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onShardCrystallized?.UnregisterCallback(_handleShardCrystallizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_shardSO == null) return;
            int bonus = _shardSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_shardSO == null) return;
            _shardSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _shardSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_shardSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_shardLabel != null)
                _shardLabel.text = $"Shards: {_shardSO.Shards}/{_shardSO.ShardsNeeded}";

            if (_crystalLabel != null)
                _crystalLabel.text = $"Crystals: {_shardSO.CrystallizationCount}";

            if (_shardBar != null)
                _shardBar.value = _shardSO.ShardProgress;
        }

        public ZoneControlCaptureShardSO ShardSO => _shardSO;
    }
}
