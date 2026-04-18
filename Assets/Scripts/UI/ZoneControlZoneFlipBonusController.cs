using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneFlipBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneFlipBonusSO _flipBonusSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFlipBonus;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _flipsLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleBotCapturedDelegate;
        private Action _handlePlayerCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleBotCapturedDelegate    = HandleBotZoneCaptured;
            _handlePlayerCapturedDelegate = HandlePlayerZoneCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _refreshDelegate              = Refresh;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFlipBonus?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFlipBonus?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleBotZoneCaptured()
        {
            if (_flipBonusSO == null) return;
            _flipBonusSO.RecordBotCapture();
            Refresh();
        }

        private void HandlePlayerZoneCaptured()
        {
            if (_flipBonusSO == null) return;
            int prev = _flipBonusSO.FlipCount;
            _flipBonusSO.RecordPlayerCapture();
            if (_flipBonusSO.FlipCount > prev)
                _wallet?.AddFunds(_flipBonusSO.BonusPerFlip);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _flipBonusSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_flipBonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_flipsLabel != null)
                _flipsLabel.text = $"Flips: {_flipBonusSO.FlipCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Flip Bonus: {_flipBonusSO.TotalBonusAwarded}";
        }

        public ZoneControlZoneFlipBonusSO FlipBonusSO => _flipBonusSO;
    }
}
