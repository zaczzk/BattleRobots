using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCategoryStackController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCategoryStackSO _categoryStackSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCategoryStackDescended;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _patchLabel;
        [SerializeField] private Text       _descendLabel;
        [SerializeField] private Slider     _patchBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDescendedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDescendedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCategoryStackDescended?.RegisterCallback(_handleDescendedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCategoryStackDescended?.UnregisterCallback(_handleDescendedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_categoryStackSO == null) return;
            int bonus = _categoryStackSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_categoryStackSO == null) return;
            _categoryStackSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _categoryStackSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_categoryStackSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_patchLabel != null)
                _patchLabel.text = $"Patches: {_categoryStackSO.Patches}/{_categoryStackSO.PatchesNeeded}";

            if (_descendLabel != null)
                _descendLabel.text = $"Descents: {_categoryStackSO.DescendCount}";

            if (_patchBar != null)
                _patchBar.value = _categoryStackSO.PatchProgress;
        }

        public ZoneControlCaptureCategoryStackSO CategoryStackSO => _categoryStackSO;
    }
}
