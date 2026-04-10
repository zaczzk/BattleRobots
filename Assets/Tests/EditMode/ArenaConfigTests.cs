using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ArenaConfig"/> ScriptableObject
    /// and the companion <see cref="SpawnPointData"/> data class.
    ///
    /// Covers:
    ///   • Fresh-instance dimension properties (GroundWidth/Depth, WallHeight/Thickness)
    ///     satisfy their inspector [Min] constraints and are positive.
    ///   • SpawnPoints list is non-null and starts empty.
    ///   • ArenaIndex defaults to zero.
    ///   • <see cref="SpawnPointData"/> default field values (position, eulerAngles, label).
    ///
    /// No scene or asset database required — <see cref="ScriptableObject.CreateInstance{T}"/>
    /// and plain-object construction are sufficient.
    /// </summary>
    public class ArenaConfigTests
    {
        private ArenaConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ArenaConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            _config = null;
        }

        // ── Ground dimensions ─────────────────────────────────────────────────

        [Test]
        public void FreshInstance_GroundWidth_IsPositive()
        {
            // Inspector: Min(1f); default 20f.
            Assert.Greater(_config.GroundWidth, 0f);
        }

        [Test]
        public void FreshInstance_GroundDepth_IsPositive()
        {
            // Inspector: Min(1f); default 20f.
            Assert.Greater(_config.GroundDepth, 0f);
        }

        // ── Wall dimensions ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_WallHeight_IsPositive()
        {
            // Inspector: Min(0.5f); default 3f.
            Assert.Greater(_config.WallHeight, 0f);
        }

        [Test]
        public void FreshInstance_WallThickness_IsPositive()
        {
            // Inspector: Min(0.1f); default 0.5f.
            Assert.Greater(_config.WallThickness, 0f);
        }

        // ── Spawn points ───────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_SpawnPoints_IsNotNull()
        {
            Assert.IsNotNull(_config.SpawnPoints);
        }

        [Test]
        public void FreshInstance_SpawnPoints_IsEmpty()
        {
            // Spawn points are authored in the Inspector; fresh SO has none.
            Assert.AreEqual(0, _config.SpawnPoints.Count);
        }

        // ── Arena index ────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_ArenaIndex_IsZero()
        {
            // Default arenaIndex is 0 — the first (and currently only) arena.
            Assert.AreEqual(0, _config.ArenaIndex);
        }

        // ── SpawnPointData defaults ────────────────────────────────────────────

        [Test]
        public void SpawnPointData_DefaultLabel_IsSpawn()
        {
            var spd = new SpawnPointData();
            Assert.AreEqual("Spawn", spd.label);
        }

        [Test]
        public void SpawnPointData_DefaultPosition_IsZero()
        {
            var spd = new SpawnPointData();
            Assert.AreEqual(Vector3.zero, spd.position);
        }

        [Test]
        public void SpawnPointData_DefaultEulerAngles_IsZero()
        {
            var spd = new SpawnPointData();
            Assert.AreEqual(Vector3.zero, spd.eulerAngles);
        }
    }
}
