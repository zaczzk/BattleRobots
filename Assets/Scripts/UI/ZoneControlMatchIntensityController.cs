using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that gathers state from optional surge, frenzy, endurance,
    /// and velocity SOs, computes the composite match intensity via
    /// <see cref="ZoneControlMatchIntensitySO.ComputeIntensity"/>, and displays
    /// "Intensity: F1/10" with a fill bar.
    ///
    /// <c>_onMatchStarted</c>: Reset + Refresh.
    /// <c>_onIntensityChanged</c>: Refresh.
    /// <c>_onZoneCaptured</c>: ComputeIntensity + Refresh.
    /// Update: ComputeIntensity + Refresh every <c>_updateInterval</c> seconds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchIntensityController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchIntensitySO       _intensitySO;
        [SerializeField] private ZoneControlSurgeDetectorSO        _surgeSO;
        [SerializeField] private ZoneControlCaptureFrenzySO        _frenzySO;
        [SerializeField] private ZoneControlCaptureEnduranceSO     _enduranceSO;
        [SerializeField] private ZoneControlCaptureVelocitySO      _velocitySO;

        [Header("Velocity normalisation divisor (captures/s at max intensity)")]
        [Min(0.1f)]
        [SerializeField] private float _velocityDivisor = 2f;

        [Header("Update interval (seconds; 0 = every frame)")]
        [Min(0f)]
        [SerializeField] private float _updateInterval = 0.5f;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onIntensityChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _intensityLabel;
        [SerializeField] private Slider     _intensityBar;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handleZoneCapturedDelegate;
        private Action _refreshDelegate;

        private float _updateTimer;

        private void Awake()
        {
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onIntensityChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onIntensityChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_intensitySO == null)
                return;

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= _updateInterval)
            {
                _updateTimer = 0f;
                EvaluateAndRefresh();
            }
        }

        private void HandleMatchStarted()
        {
            _intensitySO?.Reset();
            _updateTimer = 0f;
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            EvaluateAndRefresh();
        }

        private void EvaluateAndRefresh()
        {
            if (_intensitySO == null)
                return;

            bool  isSurging  = _surgeSO    != null && _surgeSO.IsSurging;
            bool  isFrenzy   = _frenzySO   != null && _frenzySO.IsFrenzy;
            bool  isEnduring = _enduranceSO != null && _enduranceSO.IsEnduring;
            float velocity   = _velocitySO  != null
                ? _velocitySO.GetVelocity(Time.time) / _velocityDivisor
                : 0f;

            _intensitySO.ComputeIntensity(isSurging, isFrenzy, isEnduring, velocity);
            Refresh();
        }

        public void Refresh()
        {
            if (_intensitySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_intensityLabel != null)
                _intensityLabel.text = $"Intensity: {_intensitySO.LastIntensity:F1}/10";

            if (_intensityBar != null)
                _intensityBar.value = _intensitySO.LastIntensity / 10f;
        }

        public ZoneControlMatchIntensitySO IntensitySO => _intensitySO;
    }
}
