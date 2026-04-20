using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAnvilController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAnvilSO _anvilSO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBlow;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _strikeLabel;
        [SerializeField] private Text       _blowLabel;
        [SerializeField] private Slider     _strikeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBlowDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleBlowDelegate         = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBlow?.RegisterCallback(_handleBlowDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBlow?.UnregisterCallback(_handleBlowDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_anvilSO == null) return;
            int bonus = _anvilSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_anvilSO == null) return;
            _anvilSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _anvilSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_anvilSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_strikeLabel != null)
                _strikeLabel.text = $"Strikes: {_anvilSO.StrikeCount}/{_anvilSO.StrikesPerBlow}";

            if (_blowLabel != null)
                _blowLabel.text = $"Blows: {_anvilSO.BlowCount}";

            if (_strikeBar != null)
                _strikeBar.value = _anvilSO.AnvilProgress;
        }

        public ZoneControlCaptureAnvilSO AnvilSO => _anvilSO;
    }
}
