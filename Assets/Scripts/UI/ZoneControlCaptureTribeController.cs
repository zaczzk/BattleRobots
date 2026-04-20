using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTribeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTribeSO _tribeSO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMaxTier;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _tierLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private Slider     _captureBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMaxTierDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMaxTierDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMaxTier?.RegisterCallback(_handleMaxTierDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMaxTier?.UnregisterCallback(_handleMaxTierDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_tribeSO == null) return;
            int bonus = _tribeSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_tribeSO == null) return;
            _tribeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _tribeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_tribeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_tierLabel != null)
                _tierLabel.text = $"Tribe Tier: {_tribeSO.CurrentTier}/{_tribeSO.MaxTier}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus: {_tribeSO.BonusPerTierCapture * _tribeSO.CurrentTier} per cap";

            if (_captureBar != null)
                _captureBar.value = _tribeSO.TierProgress;
        }

        public ZoneControlCaptureTribeSO TribeSO => _tribeSO;
    }
}
