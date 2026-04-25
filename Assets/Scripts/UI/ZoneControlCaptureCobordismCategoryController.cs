using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCobordismCategoryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCobordismCategorySO _cobordismCategorySO;
        [SerializeField] private PlayerWallet                           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCobordismCategoryComposed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bordismLabel;
        [SerializeField] private Text       _composeLabel;
        [SerializeField] private Slider     _bordismBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComposedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleComposedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCobordismCategoryComposed?.RegisterCallback(_handleComposedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCobordismCategoryComposed?.UnregisterCallback(_handleComposedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cobordismCategorySO == null) return;
            int bonus = _cobordismCategorySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cobordismCategorySO == null) return;
            _cobordismCategorySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cobordismCategorySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cobordismCategorySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bordismLabel != null)
                _bordismLabel.text = $"Bordisms: {_cobordismCategorySO.Bordisms}/{_cobordismCategorySO.BordismsNeeded}";

            if (_composeLabel != null)
                _composeLabel.text = $"Compositions: {_cobordismCategorySO.CompositionCount}";

            if (_bordismBar != null)
                _bordismBar.value = _cobordismCategorySO.BordismProgress;
        }

        public ZoneControlCaptureCobordismCategorySO CobordismCategorySO => _cobordismCategorySO;
    }
}
