using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePerfectComplexController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePerfectComplexSO _perfectComplexSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPerfectComplexResolved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _perfectModuleLabel;
        [SerializeField] private Text       _resolveLabel;
        [SerializeField] private Slider     _perfectModuleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleResolvedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleResolvedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPerfectComplexResolved?.RegisterCallback(_handleResolvedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPerfectComplexResolved?.UnregisterCallback(_handleResolvedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_perfectComplexSO == null) return;
            int bonus = _perfectComplexSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_perfectComplexSO == null) return;
            _perfectComplexSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _perfectComplexSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_perfectComplexSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_perfectModuleLabel != null)
                _perfectModuleLabel.text = $"Perfect Modules: {_perfectComplexSO.PerfectModules}/{_perfectComplexSO.PerfectModulesNeeded}";

            if (_resolveLabel != null)
                _resolveLabel.text = $"Resolutions: {_perfectComplexSO.ResolutionCount}";

            if (_perfectModuleBar != null)
                _perfectModuleBar.value = _perfectComplexSO.PerfectModuleProgress;
        }

        public ZoneControlCapturePerfectComplexSO PerfectComplexSO => _perfectComplexSO;
    }
}
