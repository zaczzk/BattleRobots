using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureVault", order = 205)]
    public sealed class ZoneControlCaptureVaultSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)]  private int   _depositPerCapture = 50;
        [SerializeField, Min(1f)] private float _payoutMultiplier  = 2f;
        [SerializeField, Min(1)]  private int   _maxVault          = 600;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onVaultPayout;

        private int _vaultBalance;
        private int _totalPayouts;
        private int _lastPayoutAmount;

        private void OnEnable() => Reset();

        public int   DepositPerCapture => _depositPerCapture;
        public float PayoutMultiplier  => _payoutMultiplier;
        public int   MaxVault          => _maxVault;
        public int   VaultBalance      => _vaultBalance;
        public int   TotalPayouts      => _totalPayouts;
        public int   LastPayoutAmount  => _lastPayoutAmount;
        public float VaultProgress     => _maxVault > 0
            ? Mathf.Clamp01(_vaultBalance / (float)_maxVault)
            : 0f;

        public void RecordPlayerCapture()
        {
            _vaultBalance = Mathf.Min(_vaultBalance + _depositPerCapture, _maxVault);
        }

        public int RecordBotCapture()
        {
            if (_vaultBalance == 0) return 0;
            int payout        = Mathf.RoundToInt(_vaultBalance * _payoutMultiplier);
            _lastPayoutAmount = payout;
            _totalPayouts++;
            _vaultBalance     = 0;
            _onVaultPayout?.Raise();
            return payout;
        }

        public void Reset()
        {
            _vaultBalance     = 0;
            _totalPayouts     = 0;
            _lastPayoutAmount = 0;
        }
    }
}
