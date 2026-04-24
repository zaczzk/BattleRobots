using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSheaveController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSheaveSO _sheaveSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSheaveGlued;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _sectionLabel;
        [SerializeField] private Text       _gluingLabel;
        [SerializeField] private Slider     _sectionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleGluedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleGluedDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSheaveGlued?.RegisterCallback(_handleGluedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSheaveGlued?.UnregisterCallback(_handleGluedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_sheaveSO == null) return;
            int bonus = _sheaveSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_sheaveSO == null) return;
            _sheaveSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _sheaveSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_sheaveSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_sectionLabel != null)
                _sectionLabel.text = $"Sections: {_sheaveSO.Sections}/{_sheaveSO.SectionsNeeded}";

            if (_gluingLabel != null)
                _gluingLabel.text = $"Gluings: {_sheaveSO.GluingCount}";

            if (_sectionBar != null)
                _sectionBar.value = _sheaveSO.SheaveProgress;
        }

        public ZoneControlCaptureSheaveSO SheaveSO => _sheaveSO;
    }
}
