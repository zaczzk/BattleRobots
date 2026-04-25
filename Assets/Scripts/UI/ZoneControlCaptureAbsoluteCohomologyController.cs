using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAbsoluteCohomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAbsoluteCohomologySO _absoluteCohomologySO;
        [SerializeField] private PlayerWallet                            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onAbsoluteCohomologyRealized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _absoluteCycleLabel;
        [SerializeField] private Text       _realizeLabel;
        [SerializeField] private Slider     _absoluteCycleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRealizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRealizedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onAbsoluteCohomologyRealized?.RegisterCallback(_handleRealizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onAbsoluteCohomologyRealized?.UnregisterCallback(_handleRealizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_absoluteCohomologySO == null) return;
            int bonus = _absoluteCohomologySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_absoluteCohomologySO == null) return;
            _absoluteCohomologySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _absoluteCohomologySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_absoluteCohomologySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_absoluteCycleLabel != null)
                _absoluteCycleLabel.text = $"Absolute Cycles: {_absoluteCohomologySO.AbsoluteCycles}/{_absoluteCohomologySO.AbsoluteCyclesNeeded}";

            if (_realizeLabel != null)
                _realizeLabel.text = $"Realizations: {_absoluteCohomologySO.RealizationCount}";

            if (_absoluteCycleBar != null)
                _absoluteCycleBar.value = _absoluteCohomologySO.AbsoluteCycleProgress;
        }

        public ZoneControlCaptureAbsoluteCohomologySO AbsoluteCohomologySO => _absoluteCohomologySO;
    }
}
