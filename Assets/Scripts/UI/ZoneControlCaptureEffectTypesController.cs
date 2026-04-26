using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureEffectTypesController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureEffectTypesSO _effectTypesSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEffectTypesCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _effectLabel;
        [SerializeField] private Text       _effectApplicationLabel;
        [SerializeField] private Slider     _effectBar;
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
            _onEffectTypesCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEffectTypesCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_effectTypesSO == null) return;
            int bonus = _effectTypesSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_effectTypesSO == null) return;
            _effectTypesSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _effectTypesSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_effectTypesSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_effectLabel != null)
                _effectLabel.text =
                    $"Effects: {_effectTypesSO.Effects}/{_effectTypesSO.EffectsNeeded}";

            if (_effectApplicationLabel != null)
                _effectApplicationLabel.text = $"Effect Applications: {_effectTypesSO.EffectApplicationCount}";

            if (_effectBar != null)
                _effectBar.value = _effectTypesSO.EffectProgress;
        }

        public ZoneControlCaptureEffectTypesSO EffectTypesSO => _effectTypesSO;
    }
}
