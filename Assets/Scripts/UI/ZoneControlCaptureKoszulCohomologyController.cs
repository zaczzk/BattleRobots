using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureKoszulCohomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureKoszulCohomologySO _koszulSO;
        [SerializeField] private PlayerWallet                          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onKoszulCohomologyResolved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _complexLabel;
        [SerializeField] private Text       _resolveLabel;
        [SerializeField] private Slider     _complexBar;
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
            _onKoszulCohomologyResolved?.RegisterCallback(_handleResolvedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onKoszulCohomologyResolved?.UnregisterCallback(_handleResolvedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_koszulSO == null) return;
            int bonus = _koszulSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_koszulSO == null) return;
            _koszulSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _koszulSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_koszulSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_complexLabel != null)
                _complexLabel.text = $"Complexes: {_koszulSO.Complexes}/{_koszulSO.ComplexesNeeded}";

            if (_resolveLabel != null)
                _resolveLabel.text = $"Resolutions: {_koszulSO.ResolveCount}";

            if (_complexBar != null)
                _complexBar.value = _koszulSO.ComplexProgress;
        }

        public ZoneControlCaptureKoszulCohomologySO KoszulSO => _koszulSO;
    }
}
