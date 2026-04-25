using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureKolmogorovComplexity", order = 524)]
    public sealed class ZoneControlCaptureKolmogorovComplexitySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _compressedDescriptionsNeeded = 5;
        [SerializeField, Min(1)] private int _incompressibleStringsPerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerDescription          = 4600;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDescriptionCompressed;

        private int _compressedDescriptions;
        private int _compressionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CompressedDescriptionsNeeded => _compressedDescriptionsNeeded;
        public int   IncompressibleStringsPerBot  => _incompressibleStringsPerBot;
        public int   BonusPerDescription          => _bonusPerDescription;
        public int   CompressedDescriptions       => _compressedDescriptions;
        public int   CompressionCount             => _compressionCount;
        public int   TotalBonusAwarded            => _totalBonusAwarded;
        public float DescriptionProgress          => _compressedDescriptionsNeeded > 0
            ? Mathf.Clamp01(_compressedDescriptions / (float)_compressedDescriptionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _compressedDescriptions = Mathf.Min(_compressedDescriptions + 1, _compressedDescriptionsNeeded);
            if (_compressedDescriptions >= _compressedDescriptionsNeeded)
            {
                int bonus = _bonusPerDescription;
                _compressionCount++;
                _totalBonusAwarded      += bonus;
                _compressedDescriptions  = 0;
                _onDescriptionCompressed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _compressedDescriptions = Mathf.Max(0, _compressedDescriptions - _incompressibleStringsPerBot);
        }

        public void Reset()
        {
            _compressedDescriptions = 0;
            _compressionCount       = 0;
            _totalBonusAwarded      = 0;
        }
    }
}
