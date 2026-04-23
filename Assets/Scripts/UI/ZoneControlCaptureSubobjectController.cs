using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSubobjectController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSubobjectSO _subobjectSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSubobjectClassified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _inclusionLabel;
        [SerializeField] private Text       _subobjectLabel;
        [SerializeField] private Slider     _inclusionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleClassifiedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleClassifiedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSubobjectClassified?.RegisterCallback(_handleClassifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSubobjectClassified?.UnregisterCallback(_handleClassifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_subobjectSO == null) return;
            int bonus = _subobjectSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_subobjectSO == null) return;
            _subobjectSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _subobjectSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_subobjectSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_inclusionLabel != null)
                _inclusionLabel.text = $"Inclusions: {_subobjectSO.Inclusions}/{_subobjectSO.InclusionsNeeded}";

            if (_subobjectLabel != null)
                _subobjectLabel.text = $"Subobjects: {_subobjectSO.SubobjectCount}";

            if (_inclusionBar != null)
                _inclusionBar.value = _subobjectSO.InclusionProgress;
        }

        public ZoneControlCaptureSubobjectSO SubobjectSO => _subobjectSO;
    }
}
