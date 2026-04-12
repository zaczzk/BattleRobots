using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="LoadoutPresetManagerSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (Presets not-null, empty, Count=0, IsFull=false).
    ///   • <see cref="LoadoutPresetManagerSO.SavePreset"/>: null/empty/whitespace name
    ///     returns false; null partIds returns false; full capacity returns false;
    ///     valid call adds entry, returns true, fires _onPresetsChanged; null event channel
    ///     does not throw; part-ID list is a defensive copy.
    ///   • <see cref="LoadoutPresetManagerSO.LoadPreset"/>: negative index / out-of-range
    ///     index returns null; valid index returns the correct part-ID list.
    ///   • <see cref="LoadoutPresetManagerSO.DeletePreset"/>: negative index / out-of-range
    ///     index returns false; valid index removes the entry, returns true, fires event.
    ///   • <see cref="LoadoutPresetManagerSO.LoadSnapshot"/>: null input clears list;
    ///     valid list is deep-copied; null entries in list are skipped.
    ///   • <see cref="LoadoutPresetManagerSO.TakeSnapshot"/>: returns an independent deep
    ///     copy — mutating the snapshot does not affect the manager.
    ///   • <see cref="LoadoutPresetManagerSO.Reset"/>: clears list without firing event.
    ///   • Insertion order preserved across Save → Load → TakeSnapshot round-trip.
    ///   • IsFull becomes true at MaxPresets (default 5); SavePreset blocked when full.
    /// </summary>
    public class LoadoutPresetManagerSOTests
    {
        private LoadoutPresetManagerSO _manager;
        private VoidGameEvent          _onChanged;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private void WireEvent()
        {
            SetField(_manager, "_onPresetsChanged", _onChanged);
        }

        private static List<string> Parts(params string[] ids) => new List<string>(ids);

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _manager   = ScriptableObject.CreateInstance<LoadoutPresetManagerSO>();
            _onChanged = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_manager);
            Object.DestroyImmediate(_onChanged);
            _manager   = null;
            _onChanged = null;
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Presets_IsNotNull()
        {
            Assert.IsNotNull(_manager.Presets);
        }

        [Test]
        public void FreshInstance_Presets_IsEmpty()
        {
            Assert.AreEqual(0, _manager.Presets.Count);
        }

        [Test]
        public void FreshInstance_Count_IsZero()
        {
            Assert.AreEqual(0, _manager.Count);
        }

        [Test]
        public void FreshInstance_IsFull_False()
        {
            Assert.IsFalse(_manager.IsFull,
                "A fresh manager with no presets must not be full.");
        }

        [Test]
        public void FreshInstance_MaxPresets_IsDefaultFive()
        {
            Assert.AreEqual(5, _manager.MaxPresets);
        }

        // ── SavePreset — guard paths ──────────────────────────────────────────

        [Test]
        public void SavePreset_NullName_ReturnsFalse()
        {
            bool result = _manager.SavePreset(null, Parts("weapon_01"));
            Assert.IsFalse(result);
            Assert.AreEqual(0, _manager.Count);
        }

        [Test]
        public void SavePreset_EmptyName_ReturnsFalse()
        {
            bool result = _manager.SavePreset("", Parts("weapon_01"));
            Assert.IsFalse(result);
        }

        [Test]
        public void SavePreset_WhitespaceName_ReturnsFalse()
        {
            bool result = _manager.SavePreset("   ", Parts("weapon_01"));
            Assert.IsFalse(result);
        }

        [Test]
        public void SavePreset_NullPartIds_ReturnsFalse()
        {
            bool result = _manager.SavePreset("Speed Build", null);
            Assert.IsFalse(result);
            Assert.AreEqual(0, _manager.Count);
        }

        [Test]
        public void SavePreset_NullEventChannel_DoesNotThrow()
        {
            // _onPresetsChanged left null — must not throw.
            Assert.DoesNotThrow(() => _manager.SavePreset("Build A", Parts("weapon_01")));
        }

        // ── SavePreset — success path ─────────────────────────────────────────

        [Test]
        public void SavePreset_Valid_ReturnsTrue()
        {
            bool result = _manager.SavePreset("Build A", Parts("weapon_01", "chassis_01"));
            Assert.IsTrue(result);
        }

        [Test]
        public void SavePreset_Valid_IncreasesCount()
        {
            _manager.SavePreset("Build A", Parts("weapon_01"));
            Assert.AreEqual(1, _manager.Count);
        }

        [Test]
        public void SavePreset_Valid_PreservesName()
        {
            _manager.SavePreset("  Speed Build  ", Parts("weapon_01"));
            Assert.AreEqual("Speed Build", _manager.Presets[0].name,
                "Name should be trimmed and stored verbatim.");
        }

        [Test]
        public void SavePreset_Valid_PreservesPartIds()
        {
            _manager.SavePreset("Build A", Parts("weapon_01", "chassis_01"));
            IReadOnlyList<string> loaded = _manager.LoadPreset(0);
            Assert.AreEqual(2, loaded.Count);
            Assert.AreEqual("weapon_01",  loaded[0]);
            Assert.AreEqual("chassis_01", loaded[1]);
        }

        [Test]
        public void SavePreset_EmptyPartIds_Accepted()
        {
            // An empty-loadout preset is valid (player may deliberately save nothing).
            bool result = _manager.SavePreset("Empty Build", new List<string>());
            Assert.IsTrue(result);
            Assert.AreEqual(0, _manager.LoadPreset(0).Count);
        }

        [Test]
        public void SavePreset_FiresOnPresetsChanged()
        {
            WireEvent();
            int fireCount = 0;
            _onChanged.RegisterCallback(() => fireCount++);

            _manager.SavePreset("Build A", Parts("weapon_01"));

            Assert.AreEqual(1, fireCount, "SavePreset must fire _onPresetsChanged on success.");
        }

        [Test]
        public void SavePreset_MakesDefensiveCopyOfPartIds()
        {
            // Mutating the original list after saving must NOT change the stored preset.
            var originalIds = new List<string> { "weapon_01" };
            _manager.SavePreset("Build A", originalIds);

            originalIds.Add("chassis_01"); // mutate after save

            IReadOnlyList<string> stored = _manager.LoadPreset(0);
            Assert.AreEqual(1, stored.Count,
                "Stored preset must be a defensive copy — external mutation must not affect it.");
        }

        // ── SavePreset — capacity guard ───────────────────────────────────────

        [Test]
        public void SavePreset_AtMaxCapacity_ReturnsFalse()
        {
            // Fill to default max (5).
            for (int i = 0; i < _manager.MaxPresets; i++)
                _manager.SavePreset($"Build {i}", Parts("weapon_01"));

            bool result = _manager.SavePreset("One Too Many", Parts("weapon_01"));
            Assert.IsFalse(result, "SavePreset must return false when at max capacity.");
        }

        [Test]
        public void SavePreset_AtMaxCapacity_CountUnchanged()
        {
            for (int i = 0; i < _manager.MaxPresets; i++)
                _manager.SavePreset($"Build {i}", Parts("weapon_01"));

            _manager.SavePreset("Overflow", Parts("weapon_01"));
            Assert.AreEqual(_manager.MaxPresets, _manager.Count);
        }

        [Test]
        public void IsFull_TrueAfterMaxPresets()
        {
            for (int i = 0; i < _manager.MaxPresets; i++)
                _manager.SavePreset($"Build {i}", Parts("weapon_01"));

            Assert.IsTrue(_manager.IsFull);
        }

        // ── LoadPreset ────────────────────────────────────────────────────────

        [Test]
        public void LoadPreset_NegativeIndex_ReturnsNull()
        {
            _manager.SavePreset("Build A", Parts("weapon_01"));
            Assert.IsNull(_manager.LoadPreset(-1));
        }

        [Test]
        public void LoadPreset_IndexEqualToCount_ReturnsNull()
        {
            _manager.SavePreset("Build A", Parts("weapon_01"));
            Assert.IsNull(_manager.LoadPreset(_manager.Count),
                "Index == Count is out of range and must return null.");
        }

        [Test]
        public void LoadPreset_ValidIndex_ReturnsCorrectIds()
        {
            _manager.SavePreset("A", Parts("w01"));
            _manager.SavePreset("B", Parts("w02", "c01"));

            IReadOnlyList<string> result = _manager.LoadPreset(1);
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("w02", result[0]);
            Assert.AreEqual("c01", result[1]);
        }

        // ── DeletePreset ──────────────────────────────────────────────────────

        [Test]
        public void DeletePreset_NegativeIndex_ReturnsFalse()
        {
            _manager.SavePreset("Build A", Parts("weapon_01"));
            Assert.IsFalse(_manager.DeletePreset(-1));
        }

        [Test]
        public void DeletePreset_IndexEqualToCount_ReturnsFalse()
        {
            _manager.SavePreset("Build A", Parts("weapon_01"));
            Assert.IsFalse(_manager.DeletePreset(_manager.Count));
        }

        [Test]
        public void DeletePreset_ValidIndex_ReturnsTrue()
        {
            _manager.SavePreset("Build A", Parts("weapon_01"));
            Assert.IsTrue(_manager.DeletePreset(0));
        }

        [Test]
        public void DeletePreset_ValidIndex_DecreasesCount()
        {
            _manager.SavePreset("Build A", Parts("weapon_01"));
            _manager.SavePreset("Build B", Parts("chassis_01"));
            _manager.DeletePreset(0);
            Assert.AreEqual(1, _manager.Count);
        }

        [Test]
        public void DeletePreset_ValidIndex_RemovesCorrectEntry()
        {
            _manager.SavePreset("A", Parts("w01"));
            _manager.SavePreset("B", Parts("w02"));
            _manager.DeletePreset(0); // removes "A"
            Assert.AreEqual("B", _manager.Presets[0].name);
        }

        [Test]
        public void DeletePreset_FiresOnPresetsChanged()
        {
            WireEvent();
            _manager.SavePreset("Build A", Parts("weapon_01"));

            int fireCount = 0;
            _onChanged.RegisterCallback(() => fireCount++);

            _manager.DeletePreset(0);
            Assert.AreEqual(1, fireCount, "DeletePreset must fire _onPresetsChanged on success.");
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_NullInput_ClearsPresets()
        {
            _manager.SavePreset("Build A", Parts("weapon_01"));
            _manager.LoadSnapshot(null);
            Assert.AreEqual(0, _manager.Count);
        }

        [Test]
        public void LoadSnapshot_SetsPresets()
        {
            var snapData = new List<SavedLoadoutPreset>
            {
                new SavedLoadoutPreset { name = "X", partIds = Parts("w01") },
                new SavedLoadoutPreset { name = "Y", partIds = Parts("w02") },
            };
            _manager.LoadSnapshot(snapData);
            Assert.AreEqual(2, _manager.Count);
            Assert.AreEqual("X", _manager.Presets[0].name);
            Assert.AreEqual("Y", _manager.Presets[1].name);
        }

        [Test]
        public void LoadSnapshot_SkipsNullEntries()
        {
            var snapData = new List<SavedLoadoutPreset>
            {
                new SavedLoadoutPreset { name = "A", partIds = Parts("w01") },
                null,
                new SavedLoadoutPreset { name = "C", partIds = Parts("w03") },
            };
            _manager.LoadSnapshot(snapData);
            Assert.AreEqual(2, _manager.Count, "Null entries in snapshot must be skipped.");
        }

        [Test]
        public void LoadSnapshot_MakesDefensiveCopy()
        {
            var snapData = new List<SavedLoadoutPreset>
            {
                new SavedLoadoutPreset { name = "A", partIds = Parts("w01") },
            };
            _manager.LoadSnapshot(snapData);

            // Mutate the original snapshot list after loading.
            snapData.Add(new SavedLoadoutPreset { name = "B", partIds = Parts("w02") });

            Assert.AreEqual(1, _manager.Count,
                "LoadSnapshot must make a defensive copy — external mutations must not affect the manager.");
        }

        [Test]
        public void LoadSnapshot_DoesNotFireChangedEvent()
        {
            WireEvent();
            int fireCount = 0;
            _onChanged.RegisterCallback(() => fireCount++);

            _manager.LoadSnapshot(new List<SavedLoadoutPreset>
            {
                new SavedLoadoutPreset { name = "A", partIds = Parts("w01") },
            });

            Assert.AreEqual(0, fireCount, "LoadSnapshot must not fire _onPresetsChanged.");
        }

        // ── TakeSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void TakeSnapshot_ReturnsCurrentPresets()
        {
            _manager.SavePreset("A", Parts("w01"));
            _manager.SavePreset("B", Parts("w02"));

            List<SavedLoadoutPreset> snap = _manager.TakeSnapshot();
            Assert.AreEqual(2, snap.Count);
            Assert.AreEqual("A", snap[0].name);
            Assert.AreEqual("B", snap[1].name);
        }

        [Test]
        public void TakeSnapshot_IsIndependentCopy()
        {
            _manager.SavePreset("A", Parts("w01"));
            List<SavedLoadoutPreset> snap = _manager.TakeSnapshot();

            // Mutate the snapshot — manager must be unaffected.
            snap.Clear();

            Assert.AreEqual(1, _manager.Count,
                "TakeSnapshot must return an independent copy.");
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllPresets()
        {
            _manager.SavePreset("A", Parts("w01"));
            _manager.SavePreset("B", Parts("w02"));
            _manager.Reset();
            Assert.AreEqual(0, _manager.Count);
        }

        [Test]
        public void Reset_DoesNotFireChangedEvent()
        {
            WireEvent();
            _manager.SavePreset("A", Parts("w01")); // causes one fire

            int fireCount = 0;
            _onChanged.RegisterCallback(() => fireCount++);

            _manager.Reset(); // must NOT fire

            Assert.AreEqual(0, fireCount, "Reset must not fire _onPresetsChanged.");
        }

        // ── Insertion order ───────────────────────────────────────────────────

        [Test]
        public void InsertionOrderPreserved_ThroughRoundTrip()
        {
            // Save → TakeSnapshot → LoadSnapshot → verify order preserved.
            _manager.SavePreset("Alpha",   Parts("w01"));
            _manager.SavePreset("Beta",    Parts("w02"));
            _manager.SavePreset("Gamma",   Parts("w03"));

            List<SavedLoadoutPreset> snap = _manager.TakeSnapshot();
            _manager.Reset();
            _manager.LoadSnapshot(snap);

            Assert.AreEqual("Alpha", _manager.Presets[0].name);
            Assert.AreEqual("Beta",  _manager.Presets[1].name);
            Assert.AreEqual("Gamma", _manager.Presets[2].name);
        }
    }
}
