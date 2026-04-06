using System.Collections.Generic;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Places a robot prefab at the correct spawn point and applies accumulated
    /// PartDefinition stat bonuses to the live <see cref="HealthSO"/> and
    /// <see cref="HingeJointAB"/> joints before the match begins.
    ///
    /// Typical usage (MatchManager or a scene bootstrap MonoBehaviour):
    /// <code>
    ///   spawner.SpawnRobot(teamIndex: 0, prefab: playerPrefab,
    ///                      arenaConfig, playerHealthSO, equippedParts);
    /// </code>
    ///
    /// Architecture constraints
    ///   • <c>BattleRobots.Physics</c> namespace; references Core, never UI.
    ///   • ArticulationBody only — no Rigidbody manipulation.
    ///   • No heap allocations in Update / FixedUpdate (Spawn is called once per match).
    ///   • HealthSO mutation goes through <see cref="HealthSO.InitializeWithBonus"/> only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RobotSpawner : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Catalogue of all available parts. Parts not in this list are ignored.")]
        [SerializeField] private List<PartDefinition> _partCatalogue = new List<PartDefinition>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Instantiates <paramref name="robotPrefab"/> at the spawn point defined for
        /// <paramref name="teamIndex"/> in <paramref name="arenaConfig"/>, then applies
        /// the cumulative bonuses from <paramref name="equippedPartIds"/>.
        /// </summary>
        /// <param name="teamIndex">0 = player, 1 = opponent.</param>
        /// <param name="robotPrefab">Prefab root containing <see cref="RobotController"/> etc.</param>
        /// <param name="arenaConfig">Defines spawn positions.</param>
        /// <param name="healthSO">Robot's runtime HealthSO — will be initialised here.</param>
        /// <param name="equippedPartIds">
        ///   String IDs matching <see cref="PartDefinition.PartId"/> values in the catalogue.
        ///   Unknown IDs are silently skipped.
        /// </param>
        /// <returns>The spawned robot root GameObject, or null if spawn failed.</returns>
        public GameObject SpawnRobot(
            int              teamIndex,
            GameObject       robotPrefab,
            ArenaConfig      arenaConfig,
            HealthSO         healthSO,
            IList<string>    equippedPartIds)
        {
            if (robotPrefab == null)
            {
                Debug.LogError("[RobotSpawner] robotPrefab is null.", this);
                return null;
            }

            if (arenaConfig == null)
            {
                Debug.LogError("[RobotSpawner] arenaConfig is null.", this);
                return null;
            }

            // ── Spawn position ────────────────────────────────────────────────

            SpawnDescriptor spawnDesc = arenaConfig.GetSpawnForTeam(teamIndex);
            Vector3    spawnPos = spawnDesc != null ? spawnDesc.position : Vector3.zero;
            Quaternion spawnRot = spawnDesc != null ? spawnDesc.rotation : Quaternion.identity;

            if (spawnDesc == null)
            {
                Debug.LogWarning(
                    $"[RobotSpawner] No spawn descriptor for team {teamIndex}. " +
                    $"Using world origin.", this);
            }

            GameObject robotGo = Instantiate(robotPrefab, spawnPos, spawnRot);

            // ── Accumulate part bonuses ───────────────────────────────────────

            ComputeBonuses(equippedPartIds,
                out float hpBonus, out float torqueBonus, out float speedBonus);

            // ── Apply HP bonus via HealthSO ───────────────────────────────────

            if (healthSO != null)
            {
                if (hpBonus > 0f)
                    healthSO.InitializeWithBonus(hpBonus);
                else
                    healthSO.Initialize();
            }
            else
            {
                Debug.LogWarning("[RobotSpawner] healthSO is null — HP bonus not applied.", this);
            }

            // ── Apply torque bonus to all HingeJointAB joints on the robot ───

            if (torqueBonus > 0f)
            {
                HingeJointAB[] joints = robotGo.GetComponentsInChildren<HingeJointAB>(
                    includeInactive: true);

                foreach (HingeJointAB joint in joints)
                    joint.ApplyTorqueBonus(torqueBonus);

                if (joints.Length == 0)
                {
                    Debug.LogWarning(
                        "[RobotSpawner] Torque bonus > 0 but no HingeJointAB found on robot.", this);
                }
            }

            // ── Apply speed bonus to RobotController ──────────────────────────

            if (speedBonus > 0f)
            {
                RobotController controller = robotGo.GetComponent<RobotController>();
                if (controller != null)
                    controller.ApplySpeedBonus(speedBonus);
                else
                    Debug.LogWarning(
                        "[RobotSpawner] Speed bonus > 0 but no RobotController found on robot.", this);
            }

            Debug.Log(
                $"[RobotSpawner] Team {teamIndex} robot spawned at {spawnPos}. " +
                $"HP bonus: +{hpBonus:F1}  Torque bonus: +{torqueBonus:F2}  Speed bonus: +{speedBonus:F2}", this);

            return robotGo;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Sums HP, torque, and speed bonuses from the provided part IDs.
        /// Allocates a temporary Dictionary (called once at spawn, not in hot path).
        /// </summary>
        private void ComputeBonuses(
            IList<string> equippedPartIds,
            out float hpBonus,
            out float torqueBonus,
            out float speedBonus)
        {
            hpBonus     = 0f;
            torqueBonus = 0f;
            speedBonus  = 0f;

            if (equippedPartIds == null || equippedPartIds.Count == 0) return;
            if (_partCatalogue  == null || _partCatalogue.Count  == 0) return;

            // Build lookup: partId → PartDefinition (once per spawn call).
            var lookup = new Dictionary<string, PartDefinition>(_partCatalogue.Count,
                StringComparer.Ordinal);

            foreach (PartDefinition part in _partCatalogue)
            {
                if (part != null && !string.IsNullOrEmpty(part.PartId))
                    lookup[part.PartId] = part;
            }

            // Accumulate bonuses.
            for (int i = 0; i < equippedPartIds.Count; i++)
            {
                string id = equippedPartIds[i];
                if (string.IsNullOrEmpty(id)) continue;

                if (lookup.TryGetValue(id, out PartDefinition def))
                {
                    hpBonus     += def.HpBonus;
                    torqueBonus += def.TorqueBonus;
                    speedBonus  += def.SpeedBonus;
                }
                else
                {
                    Debug.LogWarning(
                        $"[RobotSpawner] Part ID '{id}' not found in catalogue.", this);
                }
            }
        }
    }
}
