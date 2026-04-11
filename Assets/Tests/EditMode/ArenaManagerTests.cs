using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ArenaManager.HandleMatchStarted"/>.
    ///
    /// Covers:
    ///   • Null-guard: HandleMatchStarted returns early (no throw) when ArenaConfig is null.
    ///   • Happy path: robot position and rotation are set to the matching spawn point.
    ///   • Index mapping: robot[i] lands at spawnPoints[i].
    ///   • Count mismatch: more robots than spawn points (extra robots unchanged);
    ///     fewer robots than spawn points (only available robots moved).
    ///   • Null robot root: null list entry is silently skipped; subsequent valid
    ///     entries are still positioned.
    ///   • Re-entry: calling HandleMatchStarted twice repositions robots correctly.
    ///
    /// ArenaManager is a MonoBehaviour; a headless GameObject is created in SetUp
    /// and destroyed in TearDown.  Private serialised fields are injected via
    /// reflection — the same pattern used throughout this test suite.
    /// </summary>
    public class ArenaManagerTests
    {
        // ── Scene objects ─────────────────────────────────────────────────────
        private GameObject  _go;
        private ArenaManager _manager;
        private ArenaConfig  _arenaConfig;

        // Track extra GameObjects for cleanup.
        private readonly List<GameObject> _robots = new List<GameObject>();

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static SpawnPointData MakeSpawn(Vector3 pos, Vector3 euler) =>
            new SpawnPointData { position = pos, eulerAngles = euler };

        private void ConfigureArena(List<SpawnPointData> spawnPoints)
        {
            SetField(_arenaConfig, "_spawnPoints", spawnPoints);
            SetField(_manager, "_arenaConfig", _arenaConfig);
        }

        private void SetRobots(List<GameObject> robots)
        {
            SetField(_manager, "_robotRoots", robots);
        }

        private GameObject MakeRobot(string robotName = "Robot")
        {
            var go = new GameObject(robotName);
            _robots.Add(go);
            return go;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go          = new GameObject("TestArenaManager");
            _manager     = _go.AddComponent<ArenaManager>();
            _arenaConfig = ScriptableObject.CreateInstance<ArenaConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_arenaConfig);
            foreach (var r in _robots)
                if (r != null) Object.DestroyImmediate(r);
            _robots.Clear();

            _go          = null;
            _manager     = null;
            _arenaConfig = null;
        }

        // ── Null-guard ────────────────────────────────────────────────────────

        [Test]
        public void NullArenaConfig_DoesNotThrow()
        {
            // _arenaConfig intentionally left null in the manager.
            Assert.DoesNotThrow(() => _manager.HandleMatchStarted());
        }

        [Test]
        public void NoRobots_NoSpawnPoints_DoesNotThrow()
        {
            ConfigureArena(new List<SpawnPointData>());
            SetRobots(new List<GameObject>());

            Assert.DoesNotThrow(() => _manager.HandleMatchStarted());
        }

        [Test]
        public void ZeroRobots_DoesNotThrow()
        {
            ConfigureArena(new List<SpawnPointData>
            {
                MakeSpawn(Vector3.zero, Vector3.zero),
                MakeSpawn(Vector3.one,  Vector3.zero)
            });
            SetRobots(new List<GameObject>());

            Assert.DoesNotThrow(() => _manager.HandleMatchStarted());
        }

        // ── Happy path: position and rotation ─────────────────────────────────

        [Test]
        public void OneRobot_OneSpawnPoint_PositionApplied()
        {
            var spawnPos = new Vector3(5f, 0f, 3f);
            ConfigureArena(new List<SpawnPointData> { MakeSpawn(spawnPos, Vector3.zero) });
            var robot = MakeRobot();
            SetRobots(new List<GameObject> { robot });

            _manager.HandleMatchStarted();

            Assert.AreEqual(spawnPos, robot.transform.position,
                "Robot position must match spawn point position.");
        }

        [Test]
        public void OneRobot_OneSpawnPoint_RotationApplied()
        {
            var euler = new Vector3(0f, 90f, 0f);
            ConfigureArena(new List<SpawnPointData> { MakeSpawn(Vector3.zero, euler) });
            var robot = MakeRobot();
            SetRobots(new List<GameObject> { robot });

            _manager.HandleMatchStarted();

            float angle = Quaternion.Angle(robot.transform.rotation, Quaternion.Euler(euler));
            Assert.Less(angle, 0.1f,
                "Robot rotation should match spawn point euler angles within 0.1 degree.");
        }

        // ── Index mapping ─────────────────────────────────────────────────────

        [Test]
        public void TwoRobots_TwoSpawnPoints_BothPositioned()
        {
            var pos0 = new Vector3( 5f, 0f, 0f);
            var pos1 = new Vector3(-5f, 0f, 0f);
            ConfigureArena(new List<SpawnPointData>
            {
                MakeSpawn(pos0, Vector3.zero),
                MakeSpawn(pos1, Vector3.zero)
            });
            var r0 = MakeRobot("Player");
            var r1 = MakeRobot("Enemy");
            SetRobots(new List<GameObject> { r0, r1 });

            _manager.HandleMatchStarted();

            Assert.AreEqual(pos0, r0.transform.position, "Player (index 0) → spawn[0].");
            Assert.AreEqual(pos1, r1.transform.position, "Enemy (index 1) → spawn[1].");
        }

        [Test]
        public void TwoRobots_TwoSpawnPoints_EachGetsCorrectSpawnPoint()
        {
            // Distinct positions — verifies the i→i mapping rather than just "both moved".
            var pos0 = new Vector3( 10f, 0f, 0f);
            var pos1 = new Vector3(-10f, 0f, 0f);
            ConfigureArena(new List<SpawnPointData>
            {
                MakeSpawn(pos0, Vector3.zero),
                MakeSpawn(pos1, Vector3.zero)
            });
            var r0 = MakeRobot("A");
            var r1 = MakeRobot("B");
            SetRobots(new List<GameObject> { r0, r1 });

            _manager.HandleMatchStarted();

            // Swap check: r0 must NOT be at pos1 and r1 must NOT be at pos0.
            Assert.AreNotEqual(pos1, r0.transform.position, "Player should not be at enemy spawn.");
            Assert.AreNotEqual(pos0, r1.transform.position, "Enemy should not be at player spawn.");
        }

        // ── Count mismatch ─────────────────────────────────────────────────────

        [Test]
        public void MoreRobotsThanSpawnPoints_DoesNotThrow()
        {
            ConfigureArena(new List<SpawnPointData> { MakeSpawn(Vector3.zero, Vector3.zero) });
            SetRobots(new List<GameObject> { MakeRobot("R0"), MakeRobot("R1") });

            Assert.DoesNotThrow(() => _manager.HandleMatchStarted());
        }

        [Test]
        public void MoreRobotsThanSpawnPoints_ExtraRobotRemainsAtOriginalPosition()
        {
            var pos0           = new Vector3(1f, 0f, 0f);
            var originalExtra  = new Vector3(99f, 0f, 0f);

            ConfigureArena(new List<SpawnPointData> { MakeSpawn(pos0, Vector3.zero) });

            var r0 = MakeRobot("Robot0");
            var r1 = MakeRobot("Robot1");
            r1.transform.position = originalExtra; // no spawn point for r1

            SetRobots(new List<GameObject> { r0, r1 });

            _manager.HandleMatchStarted();

            Assert.AreEqual(pos0,          r0.transform.position, "Robot0 must be placed at spawn[0].");
            Assert.AreEqual(originalExtra, r1.transform.position, "Robot1 (no spawn) must not move.");
        }

        [Test]
        public void FewerRobotsThanSpawnPoints_OnlyAvailableRobotPositioned()
        {
            var pos0 = new Vector3(1f, 0f, 0f);
            var pos1 = new Vector3(2f, 0f, 0f);
            ConfigureArena(new List<SpawnPointData>
            {
                MakeSpawn(pos0, Vector3.zero),
                MakeSpawn(pos1, Vector3.zero)
            });
            var r0 = MakeRobot();
            SetRobots(new List<GameObject> { r0 }); // only one robot for two spawn points

            Assert.DoesNotThrow(() => _manager.HandleMatchStarted());
            Assert.AreEqual(pos0, r0.transform.position);
        }

        // ── Null robot root ───────────────────────────────────────────────────

        [Test]
        public void NullRobotRoot_Skipped_ValidRobotAtNextIndex_StillPositioned()
        {
            var pos1 = new Vector3(-5f, 0f, 0f);
            ConfigureArena(new List<SpawnPointData>
            {
                MakeSpawn(Vector3.zero, Vector3.zero), // slot for the null entry
                MakeSpawn(pos1,         Vector3.zero)
            });
            var r1 = MakeRobot("ValidRobot");
            // robots[0] = null, robots[1] = r1
            SetRobots(new List<GameObject> { null, r1 });

            Assert.DoesNotThrow(() => _manager.HandleMatchStarted());
            Assert.AreEqual(pos1, r1.transform.position,
                "Valid robot at index 1 must be positioned at spawn[1] despite null at index 0.");
        }

        // ── Re-entry ──────────────────────────────────────────────────────────

        [Test]
        public void HandleMatchStarted_CalledTwice_ReposesRobotsCorrectly()
        {
            var pos0 = new Vector3(5f, 0f, 0f);
            ConfigureArena(new List<SpawnPointData> { MakeSpawn(pos0, Vector3.zero) });
            var robot = MakeRobot();
            SetRobots(new List<GameObject> { robot });

            _manager.HandleMatchStarted();
            robot.transform.position = Vector3.zero; // simulate movement mid-match

            _manager.HandleMatchStarted(); // second call (e.g., rematch)

            Assert.AreEqual(pos0, robot.transform.position,
                "Second HandleMatchStarted call must reposition the robot.");
        }

        // ── SelectedArenaSO override ──────────────────────────────────────────

        [Test]
        public void HandleMatchStarted_WithSelectedArena_UsesPresetConfig()
        {
            // Inspector _arenaConfig has no spawn points; preset config has one.
            var presetConfig = ScriptableObject.CreateInstance<ArenaConfig>();
            var presetPos    = new Vector3(7f, 0f, 0f);
            SetField(presetConfig, "_spawnPoints",
                new List<SpawnPointData> { MakeSpawn(presetPos, Vector3.zero) });

            var selectedArena = ScriptableObject.CreateInstance<SelectedArenaSO>();
            var preset        = new ArenaPresetsConfig.ArenaPreset
            {
                displayName = "Override",
                config      = presetConfig
            };
            selectedArena.Select(preset);

            // Assign the override but leave inspector _arenaConfig at the default instance
            // (no spawn points so positions would stay at origin if it were used).
            SetField(_manager, "_selectedArena", selectedArena);
            SetField(_manager, "_arenaConfig",   _arenaConfig); // no spawns on default

            var robot = MakeRobot();
            SetRobots(new List<GameObject> { robot });

            _manager.HandleMatchStarted();

            Assert.AreEqual(presetPos, robot.transform.position,
                "With SelectedArenaSO HasSelection=true, ArenaManager must use the preset's config.");

            Object.DestroyImmediate(presetConfig);
            Object.DestroyImmediate(selectedArena);
        }

        [Test]
        public void HandleMatchStarted_SelectedArena_NoSelection_FallsBackToInspectorConfig()
        {
            var inspectorPos = new Vector3(3f, 0f, 0f);
            ConfigureArena(new List<SpawnPointData> { MakeSpawn(inspectorPos, Vector3.zero) });

            // SelectedArenaSO has no selection (Reset or freshly created).
            var selectedArena = ScriptableObject.CreateInstance<SelectedArenaSO>();
            SetField(_manager, "_selectedArena", selectedArena);

            var robot = MakeRobot();
            SetRobots(new List<GameObject> { robot });

            _manager.HandleMatchStarted();

            Assert.AreEqual(inspectorPos, robot.transform.position,
                "When SelectedArenaSO.HasSelection is false, ArenaManager must fall back to _arenaConfig.");

            Object.DestroyImmediate(selectedArena);
        }

        [Test]
        public void HandleMatchStarted_SelectedArena_NullPresetConfig_FallsBackToInspectorConfig()
        {
            var inspectorPos = new Vector3(4f, 0f, 0f);
            ConfigureArena(new List<SpawnPointData> { MakeSpawn(inspectorPos, Vector3.zero) });

            // Preset has HasSelection=true but config is null (incomplete wiring).
            var selectedArena = ScriptableObject.CreateInstance<SelectedArenaSO>();
            selectedArena.Select(new ArenaPresetsConfig.ArenaPreset
            {
                displayName = "Incomplete",
                config      = null  // config not wired
            });
            SetField(_manager, "_selectedArena", selectedArena);

            var robot = MakeRobot();
            SetRobots(new List<GameObject> { robot });

            _manager.HandleMatchStarted();

            Assert.AreEqual(inspectorPos, robot.transform.position,
                "When the selected preset's config is null, ArenaManager must fall back to _arenaConfig.");

            Object.DestroyImmediate(selectedArena);
        }

        [Test]
        public void HandleMatchStarted_NullSelectedArena_FallsBackToInspectorConfig()
        {
            // Explicit test that null _selectedArena field is handled gracefully.
            var inspectorPos = new Vector3(2f, 0f, 0f);
            ConfigureArena(new List<SpawnPointData> { MakeSpawn(inspectorPos, Vector3.zero) });
            // _selectedArena field not set — remains null by default.

            var robot = MakeRobot();
            SetRobots(new List<GameObject> { robot });

            _manager.HandleMatchStarted();

            Assert.AreEqual(inspectorPos, robot.transform.position,
                "Null _selectedArena must be treated as 'no override' — use inspector _arenaConfig.");
        }
    }
}
