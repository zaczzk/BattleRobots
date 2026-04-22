using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBinaryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBinarySO _binarySO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPatternMatched;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bitLabel;
        [SerializeField] private Text       _matchLabel;
        [SerializeField] private Slider     _bitBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePatternMatchedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate         = HandlePlayerCaptured;
            _handleBotDelegate            = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handlePatternMatchedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPatternMatched?.RegisterCallback(_handlePatternMatchedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPatternMatched?.UnregisterCallback(_handlePatternMatchedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_binarySO == null) return;
            int bonus = _binarySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_binarySO == null) return;
            _binarySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _binarySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_binarySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bitLabel != null)
                _bitLabel.text = $"Bits: {_binarySO.Bits}/{_binarySO.BitsNeeded}";

            if (_matchLabel != null)
                _matchLabel.text = $"Patterns: {_binarySO.PatternCount}";

            if (_bitBar != null)
                _bitBar.value = _binarySO.BitProgress;
        }

        public ZoneControlCaptureBinarySO BinarySO => _binarySO;
    }
}
