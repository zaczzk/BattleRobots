using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBlitzController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBlitzSO _blitzSO;
        [SerializeField] private PlayerWalletSO            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBlitz;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _streakLabel;
        [SerializeField] private Text       _blitzCountLabel;
        [SerializeField] private Slider     _blitzProgressBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBlitzDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleBlitzDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBlitz?.RegisterCallback(_handleBlitzDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBlitz?.UnregisterCallback(_handleBlitzDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_blitzSO == null) return;
            int prev  = _blitzSO.BlitzCount;
            int bonus = _blitzSO.RecordPlayerCapture();
            if (_blitzSO.BlitzCount > prev)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_blitzSO == null) return;
            _blitzSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _blitzSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_blitzSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_streakLabel != null)
                _streakLabel.text = $"Streak: {_blitzSO.CurrentStreak}/{_blitzSO.BlitzTarget}";

            if (_blitzCountLabel != null)
                _blitzCountLabel.text = $"Blitzes: {_blitzSO.BlitzCount}";

            if (_blitzProgressBar != null)
                _blitzProgressBar.value = _blitzSO.BlitzProgress;
        }

        public ZoneControlCaptureBlitzSO BlitzSO => _blitzSO;
    }
}
