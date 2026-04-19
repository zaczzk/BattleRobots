using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCountdown", order = 146)]
    public sealed class ZoneControlCaptureCountdownSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)]   private int   _captureTarget     = 8;
        [SerializeField, Min(5f)]  private float _countdownSeconds  = 60f;
        [SerializeField, Min(0)]   private int   _bonusOnSuccess    = 400;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSuccess;
        [SerializeField] private VoidGameEvent _onFailed;

        private bool  _isActive;
        private bool  _isResolved;
        private bool  _succeeded;
        private int   _captureCount;
        private float _elapsedTime;

        private void OnEnable() => Reset();

        public int   CaptureTarget    => _captureTarget;
        public float CountdownSeconds => _countdownSeconds;
        public int   BonusOnSuccess   => _bonusOnSuccess;
        public bool  IsActive         => _isActive;
        public bool  IsResolved       => _isResolved;
        public bool  Succeeded        => _succeeded;
        public int   CaptureCount     => _captureCount;
        public float ElapsedTime      => _elapsedTime;
        public float RemainingTime    => Mathf.Max(0f, _countdownSeconds - _elapsedTime);
        public float CountdownProgress => _captureTarget > 0
            ? Mathf.Clamp01((float)_captureCount / _captureTarget)
            : 1f;

        public void StartCountdown()
        {
            _isActive    = true;
            _isResolved  = false;
            _succeeded   = false;
            _captureCount = 0;
            _elapsedTime  = 0f;
        }

        public void RecordCapture()
        {
            if (!_isActive || _isResolved) return;
            _captureCount++;
            if (_captureCount >= _captureTarget)
                ResolveSuccess();
        }

        public void Tick(float dt)
        {
            if (!_isActive || _isResolved) return;
            _elapsedTime += dt;
            if (_elapsedTime >= _countdownSeconds)
                ResolveFail();
        }

        public void Reset()
        {
            _isActive     = false;
            _isResolved   = false;
            _succeeded    = false;
            _captureCount = 0;
            _elapsedTime  = 0f;
        }

        private void ResolveSuccess()
        {
            _isResolved = true;
            _succeeded  = true;
            _onSuccess?.Raise();
        }

        private void ResolveFail()
        {
            _isResolved = true;
            _succeeded  = false;
            _onFailed?.Raise();
        }
    }
}
