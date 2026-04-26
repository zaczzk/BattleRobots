using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureEffectSystemController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureEffectSystemSO _effectSystemSO;
        [SerializeField] private PlayerWallet                      _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEffectSystemCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pureEffectAnnotationLabel;
        [SerializeField] private Text       _annotationCountLabel;
        [SerializeField] private Slider     _pureEffectAnnotationBar;
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
            _onEffectSystemCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEffectSystemCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_effectSystemSO == null) return;
            int bonus = _effectSystemSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_effectSystemSO == null) return;
            _effectSystemSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _effectSystemSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_effectSystemSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pureEffectAnnotationLabel != null)
                _pureEffectAnnotationLabel.text =
                    $"Pure Effect Annotations: {_effectSystemSO.PureEffectAnnotations}/{_effectSystemSO.PureEffectAnnotationsNeeded}";

            if (_annotationCountLabel != null)
                _annotationCountLabel.text = $"Annotations: {_effectSystemSO.AnnotationCount}";

            if (_pureEffectAnnotationBar != null)
                _pureEffectAnnotationBar.value = _effectSystemSO.PureEffectAnnotationProgress;
        }

        public ZoneControlCaptureEffectSystemSO EffectSystemSO => _effectSystemSO;
    }
}
