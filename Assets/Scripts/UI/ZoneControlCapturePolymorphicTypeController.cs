using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePolymorphicTypeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePolymorphicTypeSO _polymorphicTypeSO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPolymorphicTypeCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _typeInstantiationLabel;
        [SerializeField] private Text       _instantiationCountLabel;
        [SerializeField] private Slider     _typeInstantiationBar;
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
            _onPolymorphicTypeCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPolymorphicTypeCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_polymorphicTypeSO == null) return;
            int bonus = _polymorphicTypeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_polymorphicTypeSO == null) return;
            _polymorphicTypeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _polymorphicTypeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_polymorphicTypeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_typeInstantiationLabel != null)
                _typeInstantiationLabel.text =
                    $"Type Instantiations: {_polymorphicTypeSO.TypeInstantiations}/{_polymorphicTypeSO.TypeInstantiationsNeeded}";

            if (_instantiationCountLabel != null)
                _instantiationCountLabel.text = $"Instantiations: {_polymorphicTypeSO.InstantiationCount}";

            if (_typeInstantiationBar != null)
                _typeInstantiationBar.value = _polymorphicTypeSO.TypeInstantiationProgress;
        }

        public ZoneControlCapturePolymorphicTypeSO PolymorphicTypeSO => _polymorphicTypeSO;
    }
}
