using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureUltrafilterController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureUltrafilterSO _ultrafilterSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onUltrafilterRefined;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _ultrasetLabel;
        [SerializeField] private Text       _ultrafineLabel;
        [SerializeField] private Slider     _ultrasetBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRefinedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRefinedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onUltrafilterRefined?.RegisterCallback(_handleRefinedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onUltrafilterRefined?.UnregisterCallback(_handleRefinedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_ultrafilterSO == null) return;
            int bonus = _ultrafilterSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_ultrafilterSO == null) return;
            _ultrafilterSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _ultrafilterSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_ultrafilterSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_ultrasetLabel != null)
                _ultrasetLabel.text = $"Ultrasets: {_ultrafilterSO.Ultrasets}/{_ultrafilterSO.UltrasetsNeeded}";

            if (_ultrafineLabel != null)
                _ultrafineLabel.text = $"Ultrarefinements: {_ultrafilterSO.UltrafineCount}";

            if (_ultrasetBar != null)
                _ultrasetBar.value = _ultrafilterSO.UltrafilterProgress;
        }

        public ZoneControlCaptureUltrafilterSO UltrafilterSO => _ultrafilterSO;
    }
}
