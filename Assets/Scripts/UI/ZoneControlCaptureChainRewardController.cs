using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureChainRewardController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureChainRewardSO _chainRewardSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onChainCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chainLabel;
        [SerializeField] private Text       _chainsLabel;
        [SerializeField] private Slider     _chainProgressBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleChainCompletedDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate  = HandlePlayerCaptured;
            _handleBotCapturedDelegate     = HandleBotCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleChainCompletedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onChainCompleted?.RegisterCallback(_handleChainCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onChainCompleted?.UnregisterCallback(_handleChainCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_chainRewardSO == null) return;
            int prevChains = _chainRewardSO.ChainCount;
            _chainRewardSO.RecordPlayerCapture();
            if (_chainRewardSO.ChainCount > prevChains)
                _wallet?.AddFunds(_chainRewardSO.BonusPerChain);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_chainRewardSO == null) return;
            _chainRewardSO.BreakChain();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _chainRewardSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_chainRewardSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chainLabel != null)
                _chainLabel.text = $"Chain: {_chainRewardSO.CurrentChain}/{_chainRewardSO.ChainTarget}";

            if (_chainsLabel != null)
                _chainsLabel.text = $"Chains: {_chainRewardSO.ChainCount}";

            if (_chainProgressBar != null)
                _chainProgressBar.value = _chainRewardSO.ChainProgress;
        }

        public ZoneControlCaptureChainRewardSO ChainRewardSO => _chainRewardSO;
    }
}
