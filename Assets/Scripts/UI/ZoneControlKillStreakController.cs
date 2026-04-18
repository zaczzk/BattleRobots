using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlKillStreakController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlKillStreakSO _killStreakSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBotDefeated;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onKillStreakStarted;
        [SerializeField] private VoidGameEvent _onKillStreakEnded;
        [SerializeField] private VoidGameEvent _onKillDuringSurge;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _streakLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleBotDefeatedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleStreakChangedDelegate;
        private Action _handleKillDuringSurgeDelegate;

        private void Awake()
        {
            _handleBotDefeatedDelegate   = HandleBotDefeated;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleStreakChangedDelegate  = Refresh;
            _handleKillDuringSurgeDelegate = HandleKillDuringSurge;
        }

        private void OnEnable()
        {
            _onBotDefeated?.RegisterCallback(_handleBotDefeatedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onKillStreakStarted?.RegisterCallback(_handleStreakChangedDelegate);
            _onKillStreakEnded?.RegisterCallback(_handleStreakChangedDelegate);
            _onKillDuringSurge?.RegisterCallback(_handleKillDuringSurgeDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotDefeated?.UnregisterCallback(_handleBotDefeatedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onKillStreakStarted?.UnregisterCallback(_handleStreakChangedDelegate);
            _onKillStreakEnded?.UnregisterCallback(_handleStreakChangedDelegate);
            _onKillDuringSurge?.UnregisterCallback(_handleKillDuringSurgeDelegate);
        }

        private void Update()
        {
            if (_killStreakSO == null) return;
            _killStreakSO.Tick(Time.time);
            Refresh();
        }

        private void HandleBotDefeated()
        {
            if (_killStreakSO == null) return;
            _killStreakSO.RecordKill(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _killStreakSO?.Reset();
            Refresh();
        }

        private void HandleKillDuringSurge()
        {
            if (_killStreakSO == null) return;
            _wallet?.AddFunds(_killStreakSO.BonusPerKillInStreak);
            Refresh();
        }

        public void Refresh()
        {
            if (_killStreakSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_streakLabel != null)
                _streakLabel.text = $"Streak: {_killStreakSO.CurrentWindowKills}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Kill Bonus: {_killStreakSO.TotalBonusAwarded}";
        }

        public ZoneControlKillStreakSO KillStreakSO => _killStreakSO;
    }
}
