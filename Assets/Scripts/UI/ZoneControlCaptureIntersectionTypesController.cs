using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureIntersectionTypesController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureIntersectionTypesSO _intersectionTypesSO;
        [SerializeField] private PlayerWallet                           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onIntersectionTypesCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _witnessLabel;
        [SerializeField] private Text       _intersectionCountLabel;
        [SerializeField] private Slider     _witnessBar;
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
            _onIntersectionTypesCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onIntersectionTypesCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_intersectionTypesSO == null) return;
            int bonus = _intersectionTypesSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_intersectionTypesSO == null) return;
            _intersectionTypesSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _intersectionTypesSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_intersectionTypesSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_witnessLabel != null)
                _witnessLabel.text =
                    $"Witnesses: {_intersectionTypesSO.Witnesses}/{_intersectionTypesSO.WitnessesNeeded}";

            if (_intersectionCountLabel != null)
                _intersectionCountLabel.text = $"Intersections: {_intersectionTypesSO.IntersectionCount}";

            if (_witnessBar != null)
                _witnessBar.value = _intersectionTypesSO.WitnessProgress;
        }

        public ZoneControlCaptureIntersectionTypesSO IntersectionTypesSO => _intersectionTypesSO;
    }
}
