using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlScoreMomentumController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlScoreMomentumSO _momentumSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMajorityAchieved;
        [SerializeField] private VoidGameEvent _onMajorityLost;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMomentumGained;
        [SerializeField] private VoidGameEvent _onMomentumLost;
        [SerializeField] private VoidGameEvent _onMomentumBonus;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _leadingLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMajorityAchievedDelegate;
        private Action _handleMajorityLostDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMomentumBonusDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleMajorityAchievedDelegate = HandleMajorityAchieved;
            _handleMajorityLostDelegate     = HandleMajorityLost;
            _handleMatchStartedDelegate     = HandleMatchStarted;
            _handleMomentumBonusDelegate    = HandleMomentumBonus;
            _refreshDelegate                = Refresh;
        }

        private void OnEnable()
        {
            _onMajorityAchieved?.RegisterCallback(_handleMajorityAchievedDelegate);
            _onMajorityLost?.RegisterCallback(_handleMajorityLostDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMomentumGained?.RegisterCallback(_refreshDelegate);
            _onMomentumLost?.RegisterCallback(_refreshDelegate);
            _onMomentumBonus?.RegisterCallback(_handleMomentumBonusDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMajorityAchieved?.UnregisterCallback(_handleMajorityAchievedDelegate);
            _onMajorityLost?.UnregisterCallback(_handleMajorityLostDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMomentumGained?.UnregisterCallback(_refreshDelegate);
            _onMomentumLost?.UnregisterCallback(_refreshDelegate);
            _onMomentumBonus?.UnregisterCallback(_handleMomentumBonusDelegate);
        }

        private void Update()
        {
            _momentumSO?.Tick(Time.deltaTime);
        }

        private void HandleMajorityAchieved()
        {
            _momentumSO?.SetLeading(true);
            Refresh();
        }

        private void HandleMajorityLost()
        {
            _momentumSO?.SetLeading(false);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _momentumSO?.Reset();
            Refresh();
        }

        private void HandleMomentumBonus()
        {
            _wallet?.AddFunds(_momentumSO?.BonusPerInterval ?? 0);
            Refresh();
        }

        public void Refresh()
        {
            if (_momentumSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_leadingLabel != null)
                _leadingLabel.text = $"Leading: {_momentumSO.ElapsedWhileLeading:F1}s";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Momentum Bonus: {_momentumSO.TotalBonusAwarded}";
        }

        public ZoneControlScoreMomentumSO MomentumSO => _momentumSO;
    }
}
