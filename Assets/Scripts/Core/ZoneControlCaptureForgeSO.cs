using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureForge", order = 209)]
    public sealed class ZoneControlCaptureForgeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesPerIngot = 3;
        [SerializeField, Min(1)] private int _ingotsForForge   = 3;
        [SerializeField, Min(0)] private int _bonusPerForge    = 450;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onForge;

        private int _rawCaptures;
        private int _ingotCount;
        private int _forgeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CapturesPerIngot  => _capturesPerIngot;
        public int   IngotsForForge    => _ingotsForForge;
        public int   BonusPerForge     => _bonusPerForge;
        public int   RawCaptures       => _rawCaptures;
        public int   IngotCount        => _ingotCount;
        public int   ForgeCount        => _forgeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float IngotProgress     => _ingotsForForge > 0
            ? Mathf.Clamp01(_ingotCount / (float)_ingotsForForge)
            : 0f;
        public float RawProgress       => _capturesPerIngot > 0
            ? Mathf.Clamp01(_rawCaptures / (float)_capturesPerIngot)
            : 0f;

        public void RecordPlayerCapture()
        {
            _rawCaptures++;
            if (_rawCaptures >= _capturesPerIngot)
            {
                _rawCaptures = 0;
                _ingotCount++;
                if (_ingotCount >= _ingotsForForge)
                    Forge();
            }
        }

        private void Forge()
        {
            _forgeCount++;
            _totalBonusAwarded += _bonusPerForge;
            _ingotCount         = 0;
            _onForge?.Raise();
        }

        public void RecordBotCapture()
        {
            _rawCaptures = 0;
            _ingotCount  = 0;
        }

        public void Reset()
        {
            _rawCaptures       = 0;
            _ingotCount        = 0;
            _forgeCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
