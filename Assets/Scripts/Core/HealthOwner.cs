using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour bridge that binds a GameObject hierarchy to a <see cref="HealthSO"/> asset.
    ///
    /// <c>DamageDealer</c> (Physics layer) calls <see cref="ApplyDamage"/> via
    /// <c>GetComponentInParent&lt;HealthOwner&gt;</c> so it never holds a direct reference
    /// to the HealthSO — keeping the Physics → Core dependency clean.
    ///
    /// Attach to the root GameObject of each robot. Initialize the SO at match start
    /// (e.g. from MatchManager or GameBootstrapper via HealthSO.Initialize()).
    /// </summary>
    public sealed class HealthOwner : MonoBehaviour
    {
        [SerializeField] private HealthSO _health;

        /// <summary>Read-only access to the underlying HealthSO (e.g. for UI bindings).</summary>
        public HealthSO Health => _health;

        private void OnEnable()
        {
            if (_health == null)
                Debug.LogError($"[HealthOwner] No HealthSO assigned on '{gameObject.name}'.", this);
        }

        /// <summary>
        /// Applies damage with an optional source identifier.
        /// Forwards to <see cref="HealthSO.TakeDamage"/> if a HealthSO is assigned.
        /// </summary>
        public void ApplyDamage(float amount, string sourceId = "")
            => _health?.TakeDamage(amount, sourceId);
    }
}
