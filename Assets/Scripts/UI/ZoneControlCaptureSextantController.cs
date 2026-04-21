using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSextantController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSextantSO _sextantSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSextantFixed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _sightingLabel;
        [SerializeField] private Text       _fixLabel;
        [SerializeField] private Slider     _sightingBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFixedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFixedDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSextantFixed?.RegisterCallback(_handleFixedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSextantFixed?.UnregisterCallback(_handleFixedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_sextantSO == null) return;
            int bonus = _sextantSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_sextantSO == null) return;
            _sextantSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _sextantSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_sextantSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_sightingLabel != null)
                _sightingLabel.text = $"Sightings: {_sextantSO.Sightings}/{_sextantSO.SightingsNeeded}";

            if (_fixLabel != null)
                _fixLabel.text = $"Fixes: {_sextantSO.FixCount}";

            if (_sightingBar != null)
                _sightingBar.value = _sextantSO.SightingProgress;
        }

        public ZoneControlCaptureSextantSO SextantSO => _sextantSO;
    }
}
