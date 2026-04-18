using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneChainBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneChainBonusSO _chainBonusSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onChainCompleted;
        [SerializeField] private VoidGameEvent _onChainBroken;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chainLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private Slider     _chainBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleChainCompletedDelegate;
        private Action _handleChainBrokenDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate  = HandlePlayerCaptured;
            _handleBotCapturedDelegate     = HandleBotCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleChainCompletedDelegate  = HandleChainCompleted;
            _handleChainBrokenDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onChainCompleted?.RegisterCallback(_handleChainCompletedDelegate);
            _onChainBroken?.RegisterCallback(_handleChainBrokenDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onChainCompleted?.UnregisterCallback(_handleChainCompletedDelegate);
            _onChainBroken?.UnregisterCallback(_handleChainBrokenDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_chainBonusSO == null) return;
            _chainBonusSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_chainBonusSO == null) return;
            _chainBonusSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _chainBonusSO?.Reset();
            Refresh();
        }

        private void HandleChainCompleted()
        {
            if (_chainBonusSO == null) return;
            _wallet?.AddFunds(_chainBonusSO.BonusPerChain);
            Refresh();
        }

        public void Refresh()
        {
            if (_chainBonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chainLabel != null)
                _chainLabel.text = $"Chain: {_chainBonusSO.ChainLength}/{_chainBonusSO.ChainTarget}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus: {_chainBonusSO.TotalBonusAwarded}";

            if (_chainBar != null)
                _chainBar.value = _chainBonusSO.ChainProgress;
        }

        public ZoneControlZoneChainBonusSO ChainBonusSO => _chainBonusSO;
    }
}
