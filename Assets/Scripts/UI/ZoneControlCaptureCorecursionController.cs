using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCorecursionController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCorecursionSO _corecursionSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCorecursionCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _corecursiveStepLabel;
        [SerializeField] private Text       _corecursionCountLabel;
        [SerializeField] private Slider     _corecursiveStepBar;
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
            _onCorecursionCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCorecursionCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_corecursionSO == null) return;
            int bonus = _corecursionSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_corecursionSO == null) return;
            _corecursionSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _corecursionSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_corecursionSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_corecursiveStepLabel != null)
                _corecursiveStepLabel.text =
                    $"Corecursive Steps: {_corecursionSO.CorecursiveSteps}/{_corecursionSO.CorecursiveStepsNeeded}";

            if (_corecursionCountLabel != null)
                _corecursionCountLabel.text = $"Corecursions: {_corecursionSO.CorecursionCount}";

            if (_corecursiveStepBar != null)
                _corecursiveStepBar.value = _corecursionSO.CorecursiveStepProgress;
        }

        public ZoneControlCaptureCorecursionSO CorecursionSO => _corecursionSO;
    }
}
