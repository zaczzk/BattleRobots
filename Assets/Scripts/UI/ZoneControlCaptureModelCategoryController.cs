using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureModelCategoryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureModelCategorySO _modelCategorySO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onModelCategoryLocalized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _weakEquivLabel;
        [SerializeField] private Text       _localizeLabel;
        [SerializeField] private Slider     _weakEquivBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleLocalizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleLocalizedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onModelCategoryLocalized?.RegisterCallback(_handleLocalizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onModelCategoryLocalized?.UnregisterCallback(_handleLocalizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_modelCategorySO == null) return;
            int bonus = _modelCategorySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_modelCategorySO == null) return;
            _modelCategorySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _modelCategorySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_modelCategorySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_weakEquivLabel != null)
                _weakEquivLabel.text = $"Weak Equivs: {_modelCategorySO.WeakEquivs}/{_modelCategorySO.WeakEquivNeeded}";

            if (_localizeLabel != null)
                _localizeLabel.text = $"Localizations: {_modelCategorySO.LocalizeCount}";

            if (_weakEquivBar != null)
                _weakEquivBar.value = _modelCategorySO.WeakEquivProgress;
        }

        public ZoneControlCaptureModelCategorySO ModelCategorySO => _modelCategorySO;
    }
}
