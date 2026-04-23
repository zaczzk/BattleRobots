using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureNaturalController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureNaturalSO _naturalSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNaturalTransformed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _componentLabel;
        [SerializeField] private Text       _transformLabel;
        [SerializeField] private Slider     _componentBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTransformedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleTransformedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNaturalTransformed?.RegisterCallback(_handleTransformedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNaturalTransformed?.UnregisterCallback(_handleTransformedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_naturalSO == null) return;
            int bonus = _naturalSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_naturalSO == null) return;
            _naturalSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _naturalSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_naturalSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_componentLabel != null)
                _componentLabel.text = $"Components: {_naturalSO.Components}/{_naturalSO.ComponentsNeeded}";

            if (_transformLabel != null)
                _transformLabel.text = $"Transformations: {_naturalSO.TransformationCount}";

            if (_componentBar != null)
                _componentBar.value = _naturalSO.ComponentProgress;
        }

        public ZoneControlCaptureNaturalSO NaturalSO => _naturalSO;
    }
}
