using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCechCohomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCechCohomologySO _cechSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCechCohomologyResolved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _simplexLabel;
        [SerializeField] private Text       _resolveLabel;
        [SerializeField] private Slider     _simplexBar;
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
            _onCechCohomologyResolved?.RegisterCallback(_handleResolvedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCechCohomologyResolved?.UnregisterCallback(_handleResolvedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cechSO == null) return;
            int bonus = _cechSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cechSO == null) return;
            _cechSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cechSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cechSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_simplexLabel != null)
                _simplexLabel.text = $"Simplices: {_cechSO.Simplices}/{_cechSO.SimplicesNeeded}";

            if (_resolveLabel != null)
                _resolveLabel.text = $"Resolutions: {_cechSO.ResolveCount}";

            if (_simplexBar != null)
                _simplexBar.value = _cechSO.SimplexProgress;
        }

        public ZoneControlCaptureCechCohomologySO CechSO => _cechSO;
    }
}
