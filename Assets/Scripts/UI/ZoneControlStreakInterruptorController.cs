using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlStreakInterruptorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlStreakInterruptorSO _interruptorSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onInterrupt;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _interruptsLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleInterruptDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleInterruptDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onInterrupt?.RegisterCallback(_handleInterruptDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onInterrupt?.UnregisterCallback(_handleInterruptDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_interruptorSO == null) return;
            int prevCount = _interruptorSO.InterruptCount;
            _interruptorSO.RecordPlayerCapture();
            if (_interruptorSO.InterruptCount > prevCount)
                _wallet?.AddFunds(_interruptorSO.BonusPerInterrupt);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_interruptorSO == null) return;
            _interruptorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _interruptorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_interruptorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_interruptsLabel != null)
                _interruptsLabel.text = $"Interrupts: {_interruptorSO.InterruptCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Interrupt Bonus: {_interruptorSO.TotalBonusAwarded}";
        }

        public ZoneControlStreakInterruptorSO InterruptorSO => _interruptorSO;
    }
}
