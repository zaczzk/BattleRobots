using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGroupoidController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGroupoidSO _groupoidSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGroupoidInverted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _morphismLabel;
        [SerializeField] private Text       _inversionLabel;
        [SerializeField] private Slider     _morphismBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleInvertedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleInvertedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGroupoidInverted?.RegisterCallback(_handleInvertedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGroupoidInverted?.UnregisterCallback(_handleInvertedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_groupoidSO == null) return;
            int bonus = _groupoidSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_groupoidSO == null) return;
            _groupoidSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _groupoidSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_groupoidSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_morphismLabel != null)
                _morphismLabel.text = $"Morphisms: {_groupoidSO.Morphisms}/{_groupoidSO.MorphismsNeeded}";

            if (_inversionLabel != null)
                _inversionLabel.text = $"Inversions: {_groupoidSO.InversionCount}";

            if (_morphismBar != null)
                _morphismBar.value = _groupoidSO.GroupoidProgress;
        }

        public ZoneControlCaptureGroupoidSO GroupoidSO => _groupoidSO;
    }
}
