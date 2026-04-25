using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureArakelovTheoryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureArakelovTheorySO _arakelovTheorySO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onArakelovTheoryIntersected;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _arithmeticDivisorLabel;
        [SerializeField] private Text       _intersectLabel;
        [SerializeField] private Slider     _arithmeticDivisorBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleIntersectedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleIntersectedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onArakelovTheoryIntersected?.RegisterCallback(_handleIntersectedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onArakelovTheoryIntersected?.UnregisterCallback(_handleIntersectedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_arakelovTheorySO == null) return;
            int bonus = _arakelovTheorySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_arakelovTheorySO == null) return;
            _arakelovTheorySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _arakelovTheorySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_arakelovTheorySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_arithmeticDivisorLabel != null)
                _arithmeticDivisorLabel.text = $"Arithmetic Divisors: {_arakelovTheorySO.ArithmeticDivisors}/{_arakelovTheorySO.ArithmeticDivisorsNeeded}";

            if (_intersectLabel != null)
                _intersectLabel.text = $"Intersections: {_arakelovTheorySO.ArakelovIntersectionCount}";

            if (_arithmeticDivisorBar != null)
                _arithmeticDivisorBar.value = _arakelovTheorySO.ArithmeticDivisorProgress;
        }

        public ZoneControlCaptureArakelovTheorySO ArakelovTheorySO => _arakelovTheorySO;
    }
}
