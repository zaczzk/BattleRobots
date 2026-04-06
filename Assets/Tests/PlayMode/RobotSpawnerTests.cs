using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// PlayMode end-to-end tests for <see cref="RobotSpawner"/>.
    ///
    /// Verifies that HP, speed, and torque bonuses from equipped <see cref="PartDefinition"/>s
    /// are correctly accumulated and applied to the live <see cref="HealthSO"/> and
    /// <see cref="RobotController"/> at spawn time.
    ///
    /// All GameObjects and SO assets created during each test are destroyed in TearDown
    /// so tests remain hermetic.
    ///
    /// Private field injection uses reflection (sealed classes with serialised fields
    /// cannot be written from a separate assembly without InternalsVisibleTo).
    /// </summary>
    [TestFixture]
    public sealed class RobotSpawnerTests
    {
        // ── Per-test fixtures ─────────────────────────────────────────────────

        private GameObject   _spawnerGo;
        private RobotSpawner _spawner;
        private ArenaConfig  _arena;
        private HealthSO     _healthSO;

        // All ScriptableObjects created per-test (destroyed with DestroyImmediate in TearDown).
        private readonly List<ScriptableObject> _soAssets  = new List<ScriptableObject>();
        // All GameObjects created per-test (destroyed with Destroy/DestroyImmediate).
        private readonly List<GameObject>       _gameObjects = new List<GameObject>();

        // ── Reflection helpers ────────────────────────────────────────────────

        private static readonly BindingFlags k_private =
            BindingFlags.NonPublic | BindingFlags.Instance;

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType().GetField(fieldName, k_private);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            FieldInfo fi = target.GetType().GetField(fieldName, k_private);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        // ── SetUp / TearDown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // Spawner
            _spawnerGo = new GameObject("RobotSpawnerTest");
            _spawner   = _spawnerGo.AddComponent<RobotSpawner>();
            _gameObjects.Add(_spawnerGo);

            // Minimal ArenaConfig with one spawn descriptor for team 0
            _arena = ScriptableObject.CreateInstance<ArenaConfig>();
            var spawnPoints = new List<SpawnDescriptor>
            {
                new SpawnDescriptor
                {
                    teamIndex = 0,
                    position  = new Vector3(5f, 0f, 0f),
                    rotation  = Quaternion.identity
                }
            };
            SetField(_arena, "_spawnPoints",      spawnPoints);
            SetField(_arena, "_timeLimitSeconds", 60f);
            SetField(_arena, "_winBonusCurrency", 0);
            _soAssets.Add(_arena);

            // HealthSO: MaxHp = 100
            _healthSO = ScriptableObject.CreateInstance<HealthSO>();
            SetField(_healthSO, "_maxHp", 100f);
            _soAssets.Add(_healthSO);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _gameObjects)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            _gameObjects.Clear();

            foreach (ScriptableObject so in _soAssets)
            {
                if (so != null)
                    Object.DestroyImmediate(so);
            }
            _soAssets.Clear();
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        /// <summary>Creates a PartDefinition with the given stats; tracks for cleanup.</summary>
        private PartDefinition MakePart(string id,
                                        float hpBonus     = 0f,
                                        float speedBonus  = 0f,
                                        float torqueBonus = 0f)
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(part, "_partId",      id);
            SetField(part, "_hpBonus",     hpBonus);
            SetField(part, "_speedBonus",  speedBonus);
            SetField(part, "_torqueBonus", torqueBonus);
            _soAssets.Add(part);
            return part;
        }

        /// <summary>Wires the spawner's catalogue via reflection.</summary>
        private void SetCatalogue(params PartDefinition[] parts)
        {
            SetField(_spawner, "_partCatalogue", new List<PartDefinition>(parts));
        }

        /// <summary>
        /// Creates a plain robot prefab (no physics components); tracks for cleanup.
        /// Left inactive so Awake() does not run before Instantiate.
        /// </summary>
        private GameObject MakePrefab()
        {
            var go = new GameObject("RobotPrefab");
            go.SetActive(false);
            _gameObjects.Add(go);
            return go;
        }

        /// <summary>
        /// Creates a robot prefab with a <see cref="RobotController"/> at the given
        /// baseline drive speed; tracks for cleanup.
        /// </summary>
        private GameObject MakePrefabWithController(float driveSpeed = 15f)
        {
            var go  = new GameObject("RobotPrefabCtrl");
            go.SetActive(false);
            var ctrl = go.AddComponent<RobotController>();
            SetField(ctrl, "_driveSpeedRadPerSec", driveSpeed);
            _gameObjects.Add(go);
            return go;
        }

        /// <summary>Activates <paramref name="prefab"/>, spawns team-0, tracks result.</summary>
        private GameObject SpawnActive(GameObject prefab, IList<string> partIds)
        {
            prefab.SetActive(true);
            GameObject robot = _spawner.SpawnRobot(0, prefab, _arena, _healthSO, partIds);
            if (robot != null)
                _gameObjects.Add(robot);
            return robot;
        }

        /// <summary>Spawns with an inactive prefab (Awake deferred); tracks result.</summary>
        private GameObject SpawnInactive(GameObject prefab, IList<string> partIds = null)
        {
            partIds = partIds ?? new List<string>();
            // prefab stays inactive; Instantiate produces an inactive clone.
            GameObject robot = _spawner.SpawnRobot(0, prefab, _arena, _healthSO, partIds);
            if (robot != null)
                _gameObjects.Add(robot);
            return robot;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        /// <summary>Null robot prefab returns null without throwing.</summary>
        [Test]
        public void SpawnRobot_NullPrefab_ReturnsNull()
        {
            LogAssert.ignoreFailingMessages = true;
            GameObject result = _spawner.SpawnRobot(0, null, _arena, _healthSO,
                                                    new List<string>());
            Assert.IsNull(result, "SpawnRobot must return null when robotPrefab is null.");
        }

        /// <summary>Null arenaConfig returns null without throwing.</summary>
        [Test]
        public void SpawnRobot_NullArenaConfig_ReturnsNull()
        {
            LogAssert.ignoreFailingMessages = true;
            var prefab = MakePrefab();
            prefab.SetActive(true);
            GameObject result = _spawner.SpawnRobot(0, prefab, null, _healthSO,
                                                    new List<string>());
            Assert.IsNull(result, "SpawnRobot must return null when arenaConfig is null.");
        }

        /// <summary>
        /// No parts equipped → HealthSO plain-initialised: CurrentHp == MaxHp.
        /// </summary>
        [Test]
        public void SpawnRobot_NoPartsEquipped_CurrentHpEqualsMaxHp()
        {
            SpawnInactive(MakePrefab());

            Assert.AreEqual(100f, _healthSO.CurrentHp,     0.001f,
                "CurrentHp must equal MaxHp when no parts are equipped.");
            Assert.AreEqual(100f, _healthSO.EffectiveMaxHp, 0.001f,
                "EffectiveMaxHp must equal MaxHp when no bonus is applied.");
        }

        /// <summary>
        /// Single part with HpBonus=50 raises EffectiveMaxHp to 150 and
        /// sets CurrentHp to 150 at spawn.
        /// </summary>
        [Test]
        public void SpawnRobot_WithHpBonus_EffectiveMaxHpIncreased()
        {
            SetCatalogue(MakePart("armor_mk1", hpBonus: 50f));
            SpawnInactive(MakePrefab(), new List<string> { "armor_mk1" });

            Assert.AreEqual(150f, _healthSO.EffectiveMaxHp, 0.001f,
                "EffectiveMaxHp must be MaxHp(100) + HpBonus(50) = 150.");
            Assert.AreEqual(150f, _healthSO.CurrentHp,      0.001f,
                "CurrentHp must equal EffectiveMaxHp immediately after spawn.");
        }

        /// <summary>Two parts each with +25 HP accumulate to +50 total.</summary>
        [Test]
        public void SpawnRobot_TwoPartsWithHpBonus_AccumulatesCorrectly()
        {
            SetCatalogue(MakePart("part_a", hpBonus: 25f),
                         MakePart("part_b", hpBonus: 25f));
            SpawnInactive(MakePrefab(), new List<string> { "part_a", "part_b" });

            Assert.AreEqual(150f, _healthSO.EffectiveMaxHp, 0.001f,
                "Two parts each with +25 HP bonus must total +50 (100 → 150).");
        }

        /// <summary>
        /// Part with SpeedBonus applied to a RobotController increments
        /// <c>_driveSpeedRadPerSec</c> by the bonus amount.
        /// </summary>
        [Test]
        public void SpawnRobot_WithSpeedBonus_ControllerDriveSpeedIncreased()
        {
            const float baseSpeed  = 10f;
            const float speedBonus = 5f;

            SetCatalogue(MakePart("turbo_wheels", speedBonus: speedBonus));

            // Active prefab so Instantiate produces an active clone with components live.
            GameObject robot = SpawnActive(
                MakePrefabWithController(baseSpeed),
                new List<string> { "turbo_wheels" });

            Assert.IsNotNull(robot, "SpawnRobot must return a valid GameObject.");

            RobotController ctrl = robot.GetComponent<RobotController>();
            Assert.IsNotNull(ctrl, "Spawned robot must have a RobotController.");

            float actual = GetField<float>(ctrl, "_driveSpeedRadPerSec");
            Assert.AreEqual(baseSpeed + speedBonus, actual, 0.001f,
                $"Drive speed must be base({baseSpeed}) + bonus({speedBonus}) = {baseSpeed + speedBonus}.");
        }

        /// <summary>Unknown part IDs are silently skipped; known parts still apply.</summary>
        [Test]
        public void SpawnRobot_UnknownPartId_SkippedAndKnownPartApplied()
        {
            SetCatalogue(MakePart("known_part", hpBonus: 20f));
            LogAssert.ignoreFailingMessages = true; // suppress "not found" LogWarnings

            SpawnInactive(MakePrefab(),
                          new List<string> { "ghost_part", "known_part", "another_ghost" });

            Assert.AreEqual(120f, _healthSO.EffectiveMaxHp, 0.001f,
                "Ghost IDs must be skipped; known_part's +20 HP must still apply (100 → 120).");
        }

        /// <summary>Empty catalogue → no bonus resolved; CurrentHp equals base MaxHp.</summary>
        [Test]
        public void SpawnRobot_EmptyCatalogue_NoBonusApplied()
        {
            // Catalogue not set → default empty list in RobotSpawner.
            SpawnInactive(MakePrefab(), new List<string> { "armor_mk1" });

            Assert.AreEqual(100f, _healthSO.CurrentHp, 0.001f,
                "Empty catalogue means no parts resolve; CurrentHp must remain at MaxHp.");
        }

        /// <summary>Null HealthSO is logged and handled gracefully; robot still spawns.</summary>
        [Test]
        public void SpawnRobot_NullHealthSO_StillReturnsValidGameObject()
        {
            LogAssert.ignoreFailingMessages = true;

            var prefab = MakePrefab();
            prefab.SetActive(true);
            GameObject robot = _spawner.SpawnRobot(0, prefab, _arena, null,
                                                   new List<string>());
            if (robot != null)
                _gameObjects.Add(robot);

            Assert.IsNotNull(robot,
                "SpawnRobot must return a valid GameObject even when healthSO is null.");
        }

        /// <summary>
        /// Spawned robot is positioned at the ArenaConfig SpawnDescriptor for team 0.
        /// </summary>
        [Test]
        public void SpawnRobot_PlacesRobotAtSpawnDescriptorPosition()
        {
            var expected = new Vector3(5f, 0f, 0f); // matches SetUp descriptor

            GameObject robot = SpawnActive(MakePrefab(), new List<string>());

            Assert.IsNotNull(robot);
            Assert.AreEqual(expected.x, robot.transform.position.x, 0.001f, "X mismatch.");
            Assert.AreEqual(expected.y, robot.transform.position.y, 0.001f, "Y mismatch.");
            Assert.AreEqual(expected.z, robot.transform.position.z, 0.001f, "Z mismatch.");
        }

        /// <summary>
        /// Missing spawn descriptor (team not configured) → robot spawns at world origin.
        /// </summary>
        [Test]
        public void SpawnRobot_MissingDescriptor_SpawnsAtWorldOrigin()
        {
            LogAssert.ignoreFailingMessages = true; // "No spawn descriptor for team 1"

            var prefab = MakePrefab();
            prefab.SetActive(true);
            // Team 1 has no descriptor — only team 0 is configured in SetUp.
            GameObject robot = _spawner.SpawnRobot(1, prefab, _arena, _healthSO,
                                                   new List<string>());
            if (robot != null)
                _gameObjects.Add(robot);

            Assert.IsNotNull(robot,
                "Robot must still be created when no descriptor exists for the team.");
            Assert.AreEqual(Vector3.zero, robot.transform.position,
                "Robot without a descriptor should spawn at world origin.");
        }

        /// <summary>
        /// A torque-bonus part on a robot with no HingeJointAB logs a warning
        /// but does not throw, and the spawn pipeline completes successfully.
        /// </summary>
        [Test]
        public void SpawnRobot_TorqueBonusPart_NoJoints_CompletesWithoutException()
        {
            SetCatalogue(MakePart("power_motor", torqueBonus: 50f));
            LogAssert.ignoreFailingMessages = true; // "no HingeJointAB found on robot"

            GameObject robot = SpawnActive(MakePrefab(),
                                           new List<string> { "power_motor" });

            Assert.IsNotNull(robot,
                "SpawnRobot must complete even when no HingeJointAB is present on the robot.");
        }
    }
}
