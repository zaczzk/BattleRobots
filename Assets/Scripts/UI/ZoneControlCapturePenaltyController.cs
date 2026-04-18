using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePenaltyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePenaltySO _penaltySO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPenaltyApplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _penaltiesLabel;
        [SerializeField] private Text       _totalLostLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleBotZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleBotZoneCapturedDelegate = HandleBotZoneCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _refreshDelegate               = Refresh;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPenaltyApplied?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPenaltyApplied?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleBotZoneCaptured()
        {
            if (_penaltySO == null) return;
            _penaltySO.RecordBotCapture();
            _wallet?.Deduct(_penaltySO.PenaltyPerBotCapture);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _penaltySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_penaltySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_penaltiesLabel != null)
                _penaltiesLabel.text = $"Penalties: {_penaltySO.BotCaptureCount}";

            if (_totalLostLabel != null)
                _totalLostLabel.text = $"Total Lost: {_penaltySO.TotalPenaltyApplied}";
        }

        public ZoneControlCapturePenaltySO PenaltySO => _penaltySO;
    }
}
