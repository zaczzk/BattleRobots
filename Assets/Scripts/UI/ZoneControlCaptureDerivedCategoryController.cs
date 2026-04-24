using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDerivedCategoryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDerivedCategorySO _derivedCategorySO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDerivedCategoryDerived;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _quasiIsoLabel;
        [SerializeField] private Text       _deriveLabel;
        [SerializeField] private Slider     _quasiIsoBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDerivedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDerivedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDerivedCategoryDerived?.RegisterCallback(_handleDerivedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDerivedCategoryDerived?.UnregisterCallback(_handleDerivedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_derivedCategorySO == null) return;
            int bonus = _derivedCategorySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_derivedCategorySO == null) return;
            _derivedCategorySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _derivedCategorySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_derivedCategorySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_quasiIsoLabel != null)
                _quasiIsoLabel.text = $"Quasi-Isos: {_derivedCategorySO.QuasiIsos}/{_derivedCategorySO.QuasiIsosNeeded}";

            if (_deriveLabel != null)
                _deriveLabel.text = $"Derivations: {_derivedCategorySO.DeriveCount}";

            if (_quasiIsoBar != null)
                _quasiIsoBar.value = _derivedCategorySO.QuasiIsoProgress;
        }

        public ZoneControlCaptureDerivedCategorySO DerivedCategorySO => _derivedCategorySO;
    }
}
