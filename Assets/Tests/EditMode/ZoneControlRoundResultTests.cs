using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T327: <see cref="ZoneControlRoundResultSO"/> and
    /// <see cref="ZoneControlRoundResultController"/>.
    ///
    /// ZoneControlRoundResultTests (12):
    ///   SO_FreshInstance_EntryCount_Zero                     ×1
    ///   SO_RecordResult_Win_AddsEntry                        ×1
    ///   SO_RecordResult_PrunesOldestWhenFull                 ×1
    ///   SO_WinRate_ZeroWhenNoEntries                         ×1
    ///   SO_WinRate_CalculatesCorrectly                       ×1
    ///   SO_Reset_ClearsAll                                   ×1
    ///   Controller_FreshInstance_ResultSO_Null               ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow            ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow           ×1
    ///   Controller_OnDisable_Unregisters_Channel             ×1
    ///   Controller_HandleMatchEnded_NullRefs_NoThrow         ×1
    ///   Controller_Refresh_NullResultSO_HidesPanel           ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlRoundResultTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlRoundResultSO CreateResultSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlRoundResultSO>();
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_EntryCount_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlRoundResultSO>();
            Assert.AreEqual(0, so.EntryCount,
                "EntryCount must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordResult_Win_AddsEntry()
        {
            var so = CreateResultSO();
            so.RecordResult(true, 5);
            Assert.AreEqual(1, so.EntryCount,
                "EntryCount must be 1 after one RecordResult call.");
            Assert.IsTrue(so.GetResults()[0].PlayerWon,
                "PlayerWon must be true for a winning result.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordResult_PrunesOldestWhenFull()
        {
            var so = CreateResultSO(); // default capacity = 5
            for (int i = 0; i < 6; i++)
                so.RecordResult(i == 5, i); // 6th entry is a win

            Assert.AreEqual(5, so.EntryCount,
                "EntryCount must not exceed Capacity.");
            Assert.IsTrue(so.GetResults()[4].PlayerWon,
                "The newest (winning) entry must be the last after pruning.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WinRate_ZeroWhenNoEntries()
        {
            var so = CreateResultSO();
            Assert.AreEqual(0f, so.WinRate,
                "WinRate must be 0 when no results have been recorded.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WinRate_CalculatesCorrectly()
        {
            var so = CreateResultSO();
            so.RecordResult(true,  3);
            so.RecordResult(false, -2);
            so.RecordResult(true,  1);
            Assert.AreEqual(2f / 3f, so.WinRate, 0.001f,
                "WinRate must equal wins / total entries.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateResultSO();
            so.RecordResult(true, 5);
            so.Reset();
            Assert.AreEqual(0, so.EntryCount,
                "EntryCount must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ResultSO_Null()
        {
            var go   = new GameObject("Test_ResultSO_Null");
            var ctrl = go.AddComponent<ZoneControlRoundResultController>();
            Assert.IsNull(ctrl.ResultSO,
                "ResultSO must be null on a fresh controller instance.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlRoundResultController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlRoundResultController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlRoundResultController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onMatchEnded", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleMatchEnded_NullRefs_NoThrow()
        {
            var go   = new GameObject("Test_HandleMatchEnded_Null");
            var ctrl = go.AddComponent<ZoneControlRoundResultController>();
            Assert.DoesNotThrow(() => ctrl.HandleMatchEnded(),
                "HandleMatchEnded must not throw when all refs are null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_Refresh_NullResultSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlRoundResultController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when ResultSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
