using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLuckyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLuckySO _luckySO;
        [SerializeField] private PlayerWalletSO            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLuckyCapture;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _luckyLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleLuckyCaptureDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleLuckyCaptureDelegate = HandleLuckyCapture;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLuckyCapture?.RegisterCallback(_handleLuckyCaptureDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLuckyCapture?.UnregisterCallback(_handleLuckyCaptureDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_luckySO == null) return;
            _luckySO.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _luckySO?.Reset();
            Refresh();
        }

        private void HandleLuckyCapture()
        {
            if (_luckySO == null) return;
            _wallet?.AddFunds(_luckySO.BonusPerLucky);
            Refresh();
        }

        public void Refresh()
        {
            if (_luckySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_luckyLabel != null)
                _luckyLabel.text = $"Lucky Caps: {_luckySO.LuckyCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Lucky Bonus: {_luckySO.TotalLuckyBonus}";
        }

        public ZoneControlCaptureLuckySO LuckySO => _luckySO;
    }
}
