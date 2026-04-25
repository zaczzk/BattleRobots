using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureResolutionOfSingularitiesController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureResolutionOfSingularitiesSO _resolutionSO;
        [SerializeField] private PlayerWallet                                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onResolutionAchieved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _blowupLabel;
        [SerializeField] private Text       _resolutionLabel;
        [SerializeField] private Slider     _blowupBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleResolutionDelegate;

        private void Awake()
        {
            _handlePlayerDelegate      = HandlePlayerCaptured;
            _handleBotDelegate         = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleResolutionDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onResolutionAchieved?.RegisterCallback(_handleResolutionDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onResolutionAchieved?.UnregisterCallback(_handleResolutionDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_resolutionSO == null) return;
            int bonus = _resolutionSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_resolutionSO == null) return;
            _resolutionSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _resolutionSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_resolutionSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_blowupLabel != null)
                _blowupLabel.text = $"Blowups: {_resolutionSO.Blowups}/{_resolutionSO.BlowupsNeeded}";

            if (_resolutionLabel != null)
                _resolutionLabel.text = $"Resolutions: {_resolutionSO.ResolutionCount}";

            if (_blowupBar != null)
                _blowupBar.value = _resolutionSO.BlowupProgress;
        }

        public ZoneControlCaptureResolutionOfSingularitiesSO ResolutionSO => _resolutionSO;
    }
}
