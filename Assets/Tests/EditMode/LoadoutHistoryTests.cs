using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T206:
    ///   <see cref="LoadoutHistorySO"/> + <see cref="LoadoutHistoryController"/>.
    ///
    /// LoadoutHistorySOTests (10):
    ///   DefaultMaxHistory_IsFive                           ×1
    ///   FreshCount_IsZero                                  ×1
    ///   AddEntry_IncrementsCount                           ×1
    ///   Count_CappedAtMaxHistory                           ×1
    ///   GetEntry_EmptyBuffer_ReturnsNull                   ×1
    ///   GetEntry_NewestFirst_CorrectEntry                  ×1
    ///   GetLatest_IsNewestEntry                            ×1
    ///   RingBuffer_OldestEvicted                           ×1
    ///   Clear_ResetsCount                                  ×1
    ///   GetEntry_OutOfRange_ReturnsNull                    ×1
    ///
    /// LoadoutHistoryControllerTests (8):
    ///   FreshInstance_HistoryIsNull                        ×1
    ///   FreshInstance_PlayerLoadoutIsNull                  ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow                  ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow                 ×1
    ///   OnDisable_Unregisters                              ×1
    ///   OnMatchEnded_NullGuards_NoThrow                    ×1
    ///   OnMatchEnded_AddsEntry                             ×1
    ///   Refresh_EmptyHistory_ShowsEmptyLabel               ×1
    ///
    /// Total: 18 new EditMode tests.
    /// </summary>
    public class LoadoutHistoryTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static LoadoutHistorySO CreateHistory(int max = 5)
        {
            var so = ScriptableObject.CreateInstance<LoadoutHistorySO>();
            SetField(so, "_maxHistory", max);
            InvokePrivate(so, "OnEnable"); // initialise buffer
            return so;
        }

        private static LoadoutHistoryController CreateController()
        {
            var go = new GameObject("LoadoutHistoryCtrl_Test");
            return go.AddComponent<LoadoutHistoryController>();
        }

        // ─────────────────────────────────────────────────────────────────────
        // LoadoutHistorySOTests
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void DefaultMaxHistory_IsFive()
        {
            var so = ScriptableObject.CreateInstance<LoadoutHistorySO>();
            Assert.AreEqual(5, so.MaxHistory);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshCount_IsZero()
        {
            var so = CreateHistory();
            Assert.AreEqual(0, so.Count);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void AddEntry_IncrementsCount()
        {
            var so = CreateHistory();
            so.AddEntry(new[] { "part1" }, playerWon: true, timestamp: 0);
            Assert.AreEqual(1, so.Count);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Count_CappedAtMaxHistory()
        {
            var so = CreateHistory(max: 3);
            so.AddEntry(new[] { "a" }, true,  0);
            so.AddEntry(new[] { "b" }, false, 1);
            so.AddEntry(new[] { "c" }, true,  2);
            so.AddEntry(new[] { "d" }, false, 3); // overflow

            Assert.AreEqual(3, so.Count,
                "Count must not exceed MaxHistory.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetEntry_EmptyBuffer_ReturnsNull()
        {
            var so = CreateHistory();
            Assert.IsNull(so.GetEntry(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetEntry_NewestFirst_CorrectEntry()
        {
            var so = CreateHistory();
            so.AddEntry(new[] { "a" }, playerWon: false, timestamp: 1.0);
            so.AddEntry(new[] { "b", "c" }, playerWon: true, timestamp: 2.0);

            var latest = so.GetEntry(0);
            Assert.IsNotNull(latest);
            Assert.AreEqual(2, latest.Value.partIds.Length,
                "Most-recent entry (index 0) should have 2 parts.");
            Assert.IsTrue(latest.Value.playerWon);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetLatest_IsNewestEntry()
        {
            var so = CreateHistory();
            so.AddEntry(new[] { "old" }, false, 1.0);
            so.AddEntry(new[] { "new1", "new2" }, true, 2.0);

            var latest = so.GetLatest();
            Assert.IsNotNull(latest);
            Assert.AreEqual(2, latest.Value.partIds.Length);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RingBuffer_OldestEvicted()
        {
            var so = CreateHistory(max: 2);
            so.AddEntry(new[] { "first" }, false, 1.0);
            so.AddEntry(new[] { "second" }, true,  2.0);
            so.AddEntry(new[] { "third" }, false,  3.0); // evicts "first"

            // Newest is "third", older is "second".
            var newest = so.GetEntry(0);
            var older  = so.GetEntry(1);
            Assert.AreEqual("third",  newest.Value.partIds[0]);
            Assert.AreEqual("second", older.Value.partIds[0]);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Clear_ResetsCount()
        {
            var so = CreateHistory();
            so.AddEntry(new[] { "a" }, true, 0);
            so.AddEntry(new[] { "b" }, false, 1);
            so.Clear();

            Assert.AreEqual(0, so.Count);
            Assert.IsNull(so.GetLatest());
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetEntry_OutOfRange_ReturnsNull()
        {
            var so = CreateHistory();
            so.AddEntry(new[] { "x" }, true, 0);

            Assert.IsNull(so.GetEntry(5),
                "Out-of-range index must return null.");
            Object.DestroyImmediate(so);
        }

        // ─────────────────────────────────────────────────────────────────────
        // LoadoutHistoryControllerTests
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_HistoryIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.History);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_PlayerLoadoutIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PlayerLoadout);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMatchEnded", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable only the manually registered callback should fire.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnMatchEnded_NullGuards_NoThrow()
        {
            var ctrl = CreateController();
            // No history or loadout assigned.
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnMatchEnded"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnMatchEnded_AddsEntry()
        {
            var ctrl    = CreateController();
            var history = CreateHistory();
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new[] { "gun", "shield" });

            SetField(ctrl, "_history",       history);
            SetField(ctrl, "_playerLoadout", loadout);
            InvokePrivate(ctrl, "Awake");

            InvokePrivate(ctrl, "OnMatchEnded");

            Assert.AreEqual(1, history.Count,
                "OnMatchEnded should add one entry to the history.");
            Assert.AreEqual(2, history.GetLatest().Value.partIds.Length,
                "The entry should contain the 2 equipped part IDs.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(loadout);
        }

        [Test]
        public void Ctrl_Refresh_EmptyHistory_ShowsEmptyLabel()
        {
            var ctrl    = CreateController();
            var history = CreateHistory();
            var empty   = new GameObject("empty");
            empty.SetActive(false);

            SetField(ctrl, "_history",    history);
            SetField(ctrl, "_emptyLabel", empty);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(empty.activeSelf,
                "Empty history must activate the empty label.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(empty);
        }
    }
}
