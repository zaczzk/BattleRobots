using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGradualTypesController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGradualTypesSO _gradualTypesSO;
        [SerializeField] private PlayerWallet                      _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGradualTypesCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _consistentTypingLabel;
        [SerializeField] private Text       _gradualStepLabel;
        [SerializeField] private Slider     _consistentTypingBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompletedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompletedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGradualTypesCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGradualTypesCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_gradualTypesSO == null) return;
            int bonus = _gradualTypesSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_gradualTypesSO == null) return;
            _gradualTypesSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _gradualTypesSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_gradualTypesSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_consistentTypingLabel != null)
                _consistentTypingLabel.text =
                    $"Consistent Typings: {_gradualTypesSO.ConsistentTypings}/{_gradualTypesSO.ConsistentTypingsNeeded}";

            if (_gradualStepLabel != null)
                _gradualStepLabel.text = $"Gradual Steps: {_gradualTypesSO.GradualStepCount}";

            if (_consistentTypingBar != null)
                _consistentTypingBar.value = _gradualTypesSO.ConsistentTypingProgress;
        }

        public ZoneControlCaptureGradualTypesSO GradualTypesSO => _gradualTypesSO;
    }
}
