using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureUnionTypesController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureUnionTypesSO _unionTypesSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onUnionTypesCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _variantLabel;
        [SerializeField] private Text       _unionEliminationLabel;
        [SerializeField] private Slider     _variantBar;
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
            _onUnionTypesCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onUnionTypesCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_unionTypesSO == null) return;
            int bonus = _unionTypesSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_unionTypesSO == null) return;
            _unionTypesSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _unionTypesSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_unionTypesSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_variantLabel != null)
                _variantLabel.text =
                    $"Variants: {_unionTypesSO.Variants}/{_unionTypesSO.VariantsNeeded}";

            if (_unionEliminationLabel != null)
                _unionEliminationLabel.text = $"Union Eliminations: {_unionTypesSO.UnionEliminationCount}";

            if (_variantBar != null)
                _variantBar.value = _unionTypesSO.VariantProgress;
        }

        public ZoneControlCaptureUnionTypesSO UnionTypesSO => _unionTypesSO;
    }
}
