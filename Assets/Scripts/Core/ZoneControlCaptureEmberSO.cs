using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEmber", order = 211)]
    public sealed class ZoneControlCaptureEmberSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _heatPerCapture  = 25f;
        [SerializeField, Min(1f)]   private float _igniteThreshold = 100f;
        [SerializeField, Min(0f)]   private float _coolingAmount   = 30f;
        [SerializeField, Min(0)]    private int   _bonusPerIgnite  = 350;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onIgnite;

        private float _currentHeat;
        private int   _igniteCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float HeatPerCapture    => _heatPerCapture;
        public float IgniteThreshold   => _igniteThreshold;
        public float CoolingAmount     => _coolingAmount;
        public int   BonusPerIgnite    => _bonusPerIgnite;
        public float CurrentHeat       => _currentHeat;
        public int   IgniteCount       => _igniteCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float EmberProgress     => _igniteThreshold > 0f
            ? Mathf.Clamp01(_currentHeat / _igniteThreshold)
            : 0f;

        public int RecordPlayerCapture()
        {
            _currentHeat += _heatPerCapture;
            if (_currentHeat >= _igniteThreshold)
                return Ignite();
            return 0;
        }

        private int Ignite()
        {
            _currentHeat = 0f;
            _igniteCount++;
            _totalBonusAwarded += _bonusPerIgnite;
            _onIgnite?.Raise();
            return _bonusPerIgnite;
        }

        public void RecordBotCapture()
        {
            _currentHeat = Mathf.Max(0f, _currentHeat - _coolingAmount);
        }

        public void Reset()
        {
            _currentHeat       = 0f;
            _igniteCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
