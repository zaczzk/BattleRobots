using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSheafController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSheafSO _sheafSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSheafGlued;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _sectionLabel;
        [SerializeField] private Text       _glueLabel;
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
            _onSheafGlued?.RegisterCallback(_handleGluedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSheafGlued?.UnregisterCallback(_handleGluedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_sheafSO == null) return;
            int bonus = _sheafSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_sheafSO == null) return;
            _sheafSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _sheafSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_sheafSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_sectionLabel != null)
                _sectionLabel.text = $"Sections: {_sheafSO.Sections}/{_sheafSO.SectionsNeeded}";

            if (_glueLabel != null)
                _glueLabel.text = $"Glues: {_sheafSO.GlueCount}";

            if (_sectionBar != null)
                _sectionBar.value = _sheafSO.SectionProgress;
        }

        public ZoneControlCaptureSheafSO SheafSO => _sheafSO;
    }
}
