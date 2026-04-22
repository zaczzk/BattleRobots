using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureProfunctorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureProfunctorSO _profunctorSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onProfunctorDimapped;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _projectionLabel;
        [SerializeField] private Text       _dimapLabel;
        [SerializeField] private Slider     _projectionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDimappedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDimappedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onProfunctorDimapped?.RegisterCallback(_handleDimappedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onProfunctorDimapped?.UnregisterCallback(_handleDimappedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_profunctorSO == null) return;
            int bonus = _profunctorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_profunctorSO == null) return;
            _profunctorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _profunctorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_profunctorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_projectionLabel != null)
                _projectionLabel.text = $"Projections: {_profunctorSO.Projections}/{_profunctorSO.ProjectionsNeeded}";

            if (_dimapLabel != null)
                _dimapLabel.text = $"Dimaps: {_profunctorSO.DimapCount}";

            if (_projectionBar != null)
                _projectionBar.value = _profunctorSO.ProjectionProgress;
        }

        public ZoneControlCaptureProfunctorSO ProfunctorSO => _profunctorSO;
    }
}
