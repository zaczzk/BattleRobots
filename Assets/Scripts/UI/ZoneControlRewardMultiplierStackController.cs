using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives <see cref="ZoneControlRewardMultiplierStackSO"/>
    /// lifecycle and awards scaled wallet bonuses per zone capture.
    ///
    /// <c>_onZoneCaptured</c>: calls <c>AddStack()</c>, credits wallet via
    /// <c>ComputeBonus(_bonusPerCapture)</c>, and refreshes.
    /// <c>_onMatchStarted</c>: resets the stack SO + Refresh.
    /// <c>_onStacksChanged</c>: Refresh.
    /// <see cref="Update"/> ticks the stack decay each frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlRewardMultiplierStackController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlRewardMultiplierStackSO _stackSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Bonus Settings")]
        [Min(0)]
        [SerializeField] private int _bonusPerCapture = 20;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onStacksChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stacksLabel;
        [SerializeField] private Text       _multiplierLabel;
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onStacksChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onStacksChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _stackSO?.Tick(Time.deltaTime);
        }

        private void HandleZoneCaptured()
        {
            if (_stackSO == null) return;
            _stackSO.AddStack();
            _wallet?.AddFunds(_stackSO.ComputeBonus(_bonusPerCapture));
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _stackSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_stackSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stacksLabel != null)
                _stacksLabel.text = $"Stacks: {_stackSO.CurrentStacks}/{_stackSO.MaxStacks}";

            if (_multiplierLabel != null)
                _multiplierLabel.text = $"x{_stackSO.CurrentMultiplier:F2}";

            if (_progressBar != null)
                _progressBar.value = _stackSO.MaxStacks > 0
                    ? (float)_stackSO.CurrentStacks / _stackSO.MaxStacks
                    : 0f;
        }

        public ZoneControlRewardMultiplierStackSO StackSO => _stackSO;
    }
}
