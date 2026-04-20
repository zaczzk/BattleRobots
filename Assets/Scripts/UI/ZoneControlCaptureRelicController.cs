using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRelicController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRelicSO _relicSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRelicRestored;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _fragmentLabel;
        [SerializeField] private Text       _restorationLabel;
        [SerializeField] private Slider     _fragmentBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRestoredDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRestoredDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRelicRestored?.RegisterCallback(_handleRestoredDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRelicRestored?.UnregisterCallback(_handleRestoredDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_relicSO == null) return;
            int bonus = _relicSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_relicSO == null) return;
            _relicSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _relicSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_relicSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_fragmentLabel != null)
                _fragmentLabel.text = $"Relics: {_relicSO.Fragments}/{_relicSO.FragmentsNeeded}";

            if (_restorationLabel != null)
                _restorationLabel.text = $"Restorations: {_relicSO.RestorationCount}";

            if (_fragmentBar != null)
                _fragmentBar.value = _relicSO.FragmentProgress;
        }

        public ZoneControlCaptureRelicSO RelicSO => _relicSO;
    }
}
