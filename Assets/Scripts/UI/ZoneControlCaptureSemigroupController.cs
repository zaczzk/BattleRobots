using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSemigroupController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSemigroupSO _semigroupSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSemigroupCombined;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _elementLabel;
        [SerializeField] private Text       _combineLabel;
        [SerializeField] private Slider     _elementBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSemigroupCombinedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate             = HandlePlayerCaptured;
            _handleBotDelegate                = HandleBotCaptured;
            _handleMatchStartedDelegate       = HandleMatchStarted;
            _handleSemigroupCombinedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSemigroupCombined?.RegisterCallback(_handleSemigroupCombinedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSemigroupCombined?.UnregisterCallback(_handleSemigroupCombinedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_semigroupSO == null) return;
            int bonus = _semigroupSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_semigroupSO == null) return;
            _semigroupSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _semigroupSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_semigroupSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_elementLabel != null)
                _elementLabel.text = $"Elements: {_semigroupSO.Elements}/{_semigroupSO.ElementsNeeded}";

            if (_combineLabel != null)
                _combineLabel.text = $"Combines: {_semigroupSO.CombineCount}";

            if (_elementBar != null)
                _elementBar.value = _semigroupSO.ElementProgress;
        }

        public ZoneControlCaptureSemigroupSO SemigroupSO => _semigroupSO;
    }
}
