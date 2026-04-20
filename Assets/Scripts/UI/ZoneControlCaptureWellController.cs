using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureWellController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureWellSO _wellSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onWellDrawn;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bucketLabel;
        [SerializeField] private Text       _drawLabel;
        [SerializeField] private Slider     _bucketBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleWellDrawnDelegate;

        private void Awake()
        {
            _handlePlayerDelegate    = HandlePlayerCaptured;
            _handleBotDelegate       = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleWellDrawnDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onWellDrawn?.RegisterCallback(_handleWellDrawnDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onWellDrawn?.UnregisterCallback(_handleWellDrawnDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_wellSO == null) return;
            int bonus = _wellSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_wellSO == null) return;
            _wellSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _wellSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_wellSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bucketLabel != null)
                _bucketLabel.text = $"Buckets: {_wellSO.Buckets}/{_wellSO.BucketsNeeded}";

            if (_drawLabel != null)
                _drawLabel.text = $"Draws: {_wellSO.DrawCount}";

            if (_bucketBar != null)
                _bucketBar.value = _wellSO.BucketProgress;
        }

        public ZoneControlCaptureWellSO WellSO => _wellSO;
    }
}
