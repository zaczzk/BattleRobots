using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the M28 Match Damage History system (T177):
    ///   <see cref="MatchDamageHistorySO"/> and
    ///   <see cref="PostMatchDamageHistoryController"/>.
    ///
    /// MatchDamageHistorySOTests (10):
    ///   FreshInstance — Count is 0.
    ///   FreshInstance — MaxEntries defaults to 10.
    ///   AddEntry null stats — no-op, Count stays 0.
    ///   AddEntry valid stats — Count increments to 1.
    ///   AddEntry beyond capacity — oldest entry evicted, Count stays at max.
    ///   GetRollingAverage empty history — returns 0.
    ///   GetRollingAverage Physical — returns correct mean value.
    ///   GetRollingAverage unknown type (999) — returns 0.
    ///   TakeSnapshot returns independent copy.
    ///   Reset clears all entries.
    ///
    /// PostMatchDamageHistoryControllerTests (6):
    ///   OnEnable all-null refs — does not throw.
    ///   OnDisable all-null refs — does not throw.
    ///   OnEnable null channels — does not throw.
    ///   OnDisable unregisters callback.
    ///   Refresh null HistorySystem — does not throw.
    ///   Refresh null listContainer — does not throw.
    ///
    /// Total: 16 new EditMode tests.
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class MatchDamageHistoryTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method, object[] args = null)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, args ?? System.Array.Empty<object>());
        }

        /// <summary>Creates a MatchStatisticsSO with specific per-type damage values.</summary>
        private static MatchStatisticsSO CreateStats(
            float physical = 0f, float energy = 0f, float thermal = 0f, float shock = 0f)
        {
            var stats = ScriptableObject.CreateInstance<MatchStatisticsSO>();
            // Inject damage via the DamageInfo overload which routes to type buckets.
            // DamageInfo constructor: (amount, sourceId, hitPoint, statusEffect, damageType)
            if (physical > 0f)
                stats.RecordDamageDealt(new DamageInfo(physical, "test", Vector3.zero, null, DamageType.Physical));
            if (energy > 0f)
                stats.RecordDamageDealt(new DamageInfo(energy, "test", Vector3.zero, null, DamageType.Energy));
            if (thermal > 0f)
                stats.RecordDamageDealt(new DamageInfo(thermal, "test", Vector3.zero, null, DamageType.Thermal));
            if (shock > 0f)
                stats.RecordDamageDealt(new DamageInfo(shock, "test", Vector3.zero, null, DamageType.Shock));
            return stats;
        }

        // ══════════════════════════════════════════════════════════════════════
        // MatchDamageHistorySOTests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void FreshInstance_Count_IsZero()
        {
            var so = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            Assert.AreEqual(0, so.Count, "Fresh instance should have 0 entries.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_MaxEntries_DefaultIsTen()
        {
            var so = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            Assert.AreEqual(10, so.MaxEntries, "Default MaxEntries should be 10.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddEntry_NullStats_IsNoOp()
        {
            var so = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            so.AddEntry(null);
            Assert.AreEqual(0, so.Count, "AddEntry(null) must be a no-op.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddEntry_ValidStats_IncrementsCount()
        {
            var so    = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            var stats = CreateStats(physical: 100f);
            so.AddEntry(stats);
            Assert.AreEqual(1, so.Count, "AddEntry with valid stats must increment Count to 1.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void AddEntry_BeyondCapacity_OldestEntryEvicted()
        {
            var so = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            SetField(so, "_maxEntries", 3);

            // Fill to capacity + 1.
            for (int i = 0; i < 4; i++)
            {
                var stats = CreateStats(physical: (i + 1) * 10f);
                so.AddEntry(stats);
                Object.DestroyImmediate(stats);
            }

            Assert.AreEqual(3, so.Count, "Count must not exceed MaxEntries (3).");
            // The oldest entry (10f) should have been evicted; the newest should be 40f.
            float avg = so.GetRollingAverage(DamageType.Physical);
            // Entries 2 (20), 3 (30), 4 (40) → avg = 30
            Assert.AreEqual(30f, avg, 0.01f,
                "Rolling average should reflect only the 3 most recent entries.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetRollingAverage_EmptyHistory_ReturnsZero()
        {
            var so = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            Assert.AreEqual(0f, so.GetRollingAverage(DamageType.Physical),
                "GetRollingAverage on empty history must return 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetRollingAverage_Physical_ReturnsCorrectMean()
        {
            var so = ScriptableObject.CreateInstance<MatchDamageHistorySO>();

            var s1 = CreateStats(physical: 100f);
            var s2 = CreateStats(physical: 200f);
            so.AddEntry(s1);
            so.AddEntry(s2);

            float avg = so.GetRollingAverage(DamageType.Physical);
            Assert.AreEqual(150f, avg, 0.01f, "Rolling average of [100, 200] should be 150.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(s1);
            Object.DestroyImmediate(s2);
        }

        [Test]
        public void GetRollingAverage_UnknownDamageType_ReturnsZero()
        {
            var so    = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            var stats = CreateStats(physical: 50f);
            so.AddEntry(stats);

            float avg = so.GetRollingAverage((DamageType)999);
            Assert.AreEqual(0f, avg, "Unknown DamageType should return 0.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void TakeSnapshot_ReturnsIndependentCopy()
        {
            var so    = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            var stats = CreateStats(physical: 100f);
            so.AddEntry(stats);

            List<MatchDamageHistoryEntry> snap = so.TakeSnapshot();
            so.Reset();

            Assert.AreEqual(1, snap.Count,
                "Snapshot should preserve the entry even after Reset().");
            Assert.AreEqual(0, so.Count,
                "SO count should be 0 after Reset().");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void Reset_ClearsAllEntries()
        {
            var so    = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            var stats = CreateStats(physical: 100f);
            so.AddEntry(stats);
            so.Reset();
            Assert.AreEqual(0, so.Count, "Reset() must clear all entries.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(stats);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PostMatchDamageHistoryControllerTests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PostMatchDamageHistoryController>();
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PostMatchDamageHistoryController>();
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnEnable_NullChannels_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PostMatchDamageHistoryController>();
            SetField(ctl, "_onMatchEnded",     null);
            SetField(ctl, "_onHistoryUpdated", null);
            InvokePrivate(ctl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with null channels must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_UnregistersCallback()
        {
            var go      = new GameObject();
            var ctl     = go.AddComponent<PostMatchDamageHistoryController>();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(ctl, "_onMatchEnded", channel);

            InvokePrivate(ctl, "Awake");
            InvokePrivate(ctl, "OnEnable");

            int callCount = 0;
            channel.RegisterCallback(() => callCount++);

            InvokePrivate(ctl, "OnDisable");
            channel.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable the controller must have unregistered its callback.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_Refresh_NullHistorySystem_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PostMatchDamageHistoryController>();
            SetField(ctl, "_historySystem", null);
            Assert.DoesNotThrow(() => ctl.Refresh(),
                "Refresh with null _historySystem must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_Refresh_NullListContainer_DoesNotThrow()
        {
            var go   = new GameObject();
            var ctl  = go.AddComponent<PostMatchDamageHistoryController>();
            var hist = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            SetField(ctl, "_historySystem",   hist);
            SetField(ctl, "_listContainer",   (Transform)null);
            Assert.DoesNotThrow(() => ctl.Refresh(),
                "Refresh with null _listContainer must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(hist);
        }
    }
}
