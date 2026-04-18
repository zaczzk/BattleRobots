using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchLongevityBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchLongevityBonusSO _longevityBonusSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onLongevityBonus;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _timeLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleLongevityBonusDelegate;

        private void Awake()
        {
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleMatchEndedDelegate     = HandleMatchEnded;
            _handleLongevityBonusDelegate = HandleLongevityBonus;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onLongevityBonus?.RegisterCallback(_handleLongevityBonusDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onLongevityBonus?.UnregisterCallback(_handleLongevityBonusDelegate);
        }

        private void Update()
        {
            _longevityBonusSO?.Tick(Time.deltaTime);
        }

        private void HandleMatchStarted()
        {
            _longevityBonusSO?.Reset();
            _longevityBonusSO?.StartTracking();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            _longevityBonusSO?.StopTracking();
            Refresh();
        }

        private void HandleLongevityBonus()
        {
            _wallet?.AddFunds(_longevityBonusSO?.BonusPerInterval ?? 0);
            Refresh();
        }

        public void Refresh()
        {
            if (_longevityBonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_timeLabel != null)
                _timeLabel.text = $"Match Time: {_longevityBonusSO.ElapsedTime:F1}s";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Longevity Bonus: {_longevityBonusSO.TotalBonusAwarded}";
        }

        public ZoneControlMatchLongevityBonusSO LongevityBonusSO => _longevityBonusSO;
    }
}
