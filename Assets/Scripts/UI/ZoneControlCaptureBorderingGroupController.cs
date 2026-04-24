using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBorderingGroupController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBorderingGroupSO _borderingGroupSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBorderingGroupClassified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _manifoldLabel;
        [SerializeField] private Text       _classifyLabel;
        [SerializeField] private Slider     _manifoldBar;
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
            _onBorderingGroupClassified?.RegisterCallback(_handleClassifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBorderingGroupClassified?.UnregisterCallback(_handleClassifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_borderingGroupSO == null) return;
            int bonus = _borderingGroupSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_borderingGroupSO == null) return;
            _borderingGroupSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _borderingGroupSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_borderingGroupSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_manifoldLabel != null)
                _manifoldLabel.text = $"Manifolds: {_borderingGroupSO.Manifolds}/{_borderingGroupSO.ManifoldsNeeded}";

            if (_classifyLabel != null)
                _classifyLabel.text = $"Classifications: {_borderingGroupSO.ClassificationCount}";

            if (_manifoldBar != null)
                _manifoldBar.value = _borderingGroupSO.ManifoldProgress;
        }

        public ZoneControlCaptureBorderingGroupSO BorderingGroupSO => _borderingGroupSO;
    }
}
