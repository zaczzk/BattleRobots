using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSprocketController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSprocketSO _sprocketSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSprocketEngaged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _toothLabel;
        [SerializeField] private Text       _engageLabel;
        [SerializeField] private Slider     _toothBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleEngagedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleEngagedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSprocketEngaged?.RegisterCallback(_handleEngagedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSprocketEngaged?.UnregisterCallback(_handleEngagedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_sprocketSO == null) return;
            int bonus = _sprocketSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_sprocketSO == null) return;
            _sprocketSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _sprocketSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_sprocketSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_toothLabel != null)
                _toothLabel.text = $"Teeth: {_sprocketSO.Teeth}/{_sprocketSO.TeethNeeded}";

            if (_engageLabel != null)
                _engageLabel.text = $"Engages: {_sprocketSO.EngageCount}";

            if (_toothBar != null)
                _toothBar.value = _sprocketSO.ToothProgress;
        }

        public ZoneControlCaptureSprocketSO SprocketSO => _sprocketSO;
    }
}
