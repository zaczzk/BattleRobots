using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PlayerLoadout"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (empty loadout, Count 0).
    ///   • SetLoadout — normal, empty, null, whitespace-only entries.
    ///   • LoadSnapshot — normal, null list, whitespace entries, empty list.
    ///   • Reset — clears the loadout.
    ///   • Event channel — _onLoadoutChanged fires on SetLoadout and Reset,
    ///                     does NOT fire on LoadSnapshot.
    ///   • SaveData.loadoutPartIds field exists and round-trips.
    /// </summary>
    public class PlayerLoadoutTests
    {
        private PlayerLoadout _loadout;
        private VoidGameEvent _event;

        [SetUp]
        public void SetUp()
        {
            _loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            _event   = ScriptableObject.CreateInstance<VoidGameEvent>();

            // Wire the event channel via reflection (no Inspector in EditMode).
            System.Reflection.FieldInfo f = typeof(PlayerLoadout).GetField(
                "_onLoadoutChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(f, "Reflection: '_onLoadoutChanged' not found on PlayerLoadout.");
            f.SetValue(_loadout, _event);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_loadout);
            Object.DestroyImmediate(_event);
            _loadout = null;
            _event   = null;
        }

        // ── Fresh-instance defaults ────────────────────────────────────────────

        [Test]
        public void FreshInstance_EquippedPartIds_IsNotNull()
        {
            Assert.IsNotNull(_loadout.EquippedPartIds);
        }

        [Test]
        public void FreshInstance_Count_IsZero()
        {
            Assert.AreEqual(0, _loadout.Count);
        }

        // ── SetLoadout ────────────────────────────────────────────────────────

        [Test]
        public void SetLoadout_WithTwoParts_CountIsTwo()
        {
            _loadout.SetLoadout(new[] { "part_weapon_01", "part_armor_01" });
            Assert.AreEqual(2, _loadout.Count);
        }

        [Test]
        public void SetLoadout_PreservesOrder()
        {
            _loadout.SetLoadout(new[] { "a", "b", "c" });
            Assert.AreEqual("a", _loadout.EquippedPartIds[0]);
            Assert.AreEqual("b", _loadout.EquippedPartIds[1]);
            Assert.AreEqual("c", _loadout.EquippedPartIds[2]);
        }

        [Test]
        public void SetLoadout_NullCollection_ResultsInEmptyLoadout()
        {
            _loadout.SetLoadout(new[] { "existing" });
            _loadout.SetLoadout(null);
            Assert.AreEqual(0, _loadout.Count);
        }

        [Test]
        public void SetLoadout_EmptyCollection_ResultsInEmptyLoadout()
        {
            _loadout.SetLoadout(new[] { "existing" });
            _loadout.SetLoadout(new string[0]);
            Assert.AreEqual(0, _loadout.Count);
        }

        [Test]
        public void SetLoadout_WhitespaceEntries_AreSkipped()
        {
            _loadout.SetLoadout(new[] { "valid", "  ", "", null });
            Assert.AreEqual(1, _loadout.Count);
            Assert.AreEqual("valid", _loadout.EquippedPartIds[0]);
        }

        [Test]
        public void SetLoadout_ReplacesExistingLoadout()
        {
            _loadout.SetLoadout(new[] { "old_part" });
            _loadout.SetLoadout(new[] { "new_part_a", "new_part_b" });
            Assert.AreEqual(2, _loadout.Count);
            Assert.AreEqual("new_part_a", _loadout.EquippedPartIds[0]);
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_ValidList_PopulatesLoadout()
        {
            _loadout.LoadSnapshot(new List<string> { "snap_a", "snap_b" });
            Assert.AreEqual(2, _loadout.Count);
            Assert.AreEqual("snap_a", _loadout.EquippedPartIds[0]);
            Assert.AreEqual("snap_b", _loadout.EquippedPartIds[1]);
        }

        [Test]
        public void LoadSnapshot_NullList_ResultsInEmptyLoadout()
        {
            _loadout.LoadSnapshot(new List<string> { "existing" });
            _loadout.LoadSnapshot(null);
            Assert.AreEqual(0, _loadout.Count);
        }

        [Test]
        public void LoadSnapshot_EmptyList_ResultsInEmptyLoadout()
        {
            _loadout.LoadSnapshot(new List<string> { "existing" });
            _loadout.LoadSnapshot(new List<string>());
            Assert.AreEqual(0, _loadout.Count);
        }

        [Test]
        public void LoadSnapshot_WhitespaceEntries_AreSkipped()
        {
            _loadout.LoadSnapshot(new List<string> { "valid", " ", "" });
            Assert.AreEqual(1, _loadout.Count);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllParts()
        {
            _loadout.SetLoadout(new[] { "part_a", "part_b" });
            _loadout.Reset();
            Assert.AreEqual(0, _loadout.Count);
        }

        // ── Event channel behaviour ────────────────────────────────────────────

        [Test]
        public void SetLoadout_FiresOnLoadoutChanged()
        {
            int fired = 0;
            _event.RegisterCallback(() => fired++);

            _loadout.SetLoadout(new[] { "p1" });

            Assert.AreEqual(1, fired);
        }

        [Test]
        public void Reset_FiresOnLoadoutChanged()
        {
            int fired = 0;
            _event.RegisterCallback(() => fired++);

            _loadout.Reset();

            Assert.AreEqual(1, fired);
        }

        [Test]
        public void LoadSnapshot_DoesNotFireOnLoadoutChanged()
        {
            int fired = 0;
            _event.RegisterCallback(() => fired++);

            _loadout.LoadSnapshot(new List<string> { "p1", "p2" });

            Assert.AreEqual(0, fired, "LoadSnapshot must NOT raise the event " +
                                       "(bootstrapper calls it before listeners register).");
        }

        // ── SaveData.loadoutPartIds ────────────────────────────────────────────

        [Test]
        public void SaveData_LoadoutPartIds_IsNotNull_OnFreshInstance()
        {
            var save = new SaveData();
            Assert.IsNotNull(save.loadoutPartIds);
        }

        [Test]
        public void SaveData_LoadoutPartIds_IsEmpty_OnFreshInstance()
        {
            var save = new SaveData();
            Assert.AreEqual(0, save.loadoutPartIds.Count);
        }

        [Test]
        public void SaveData_LoadoutPartIds_RoundTrips_ViaJsonUtility()
        {
            var save = new SaveData();
            save.loadoutPartIds.Add("weapon_01");
            save.loadoutPartIds.Add("armor_02");

            string json        = UnityEngine.JsonUtility.ToJson(save);
            SaveData restored  = UnityEngine.JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(2,           restored.loadoutPartIds.Count);
            Assert.AreEqual("weapon_01", restored.loadoutPartIds[0]);
            Assert.AreEqual("armor_02",  restored.loadoutPartIds[1]);
        }
    }
}
