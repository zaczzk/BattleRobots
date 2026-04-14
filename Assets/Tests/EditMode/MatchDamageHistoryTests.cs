using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the M28 Match Damage History system (T178):
    ///   <see cref="MatchDamageHistorySO"/> and
    ///   <see cref="PostMatchDamageHistoryController"/>.
    ///
    /// MatchDamageHistorySOTests (10):
    ///   Fresh instance — MaxHistory defaults to 10.
    ///   Fresh instance — Count is 0.
    ///   AddEntry — Count increments by one.
    ///   AddEntry — Count is capped at MaxHistory (ring buffer full).
    ///   GetRollingAverage — empty history returns 0.
    ///   GetRollingAverage — single entry returns that entry's value.
    ///   GetRollingAverage — two entries returns their average.
    ///   GetRollingAverage — overflow ring buffer returns correct rolling average.
    ///   Clear — resets Count to 0.
    ///   GetRollingAverage — after Clear returns 0.
    ///
    /// PostMatchDamageHistoryControllerTests (10):
    ///   Fresh instance — History property is null.
    ///   Fresh instance — MatchStatistics property is null.
    ///   OnEnable with all-null refs does not throw.
    ///   OnDisable with all-null refs does not throw.
    ///   OnDisable unregisters from _onMatchEnded channel.
    ///   OnMatchEnded — null _history does not throw.
    ///   OnMatchEnded — null _matchStatistics does not throw.
    ///   OnMatchEnded — adds one entry to history.
    ///   ShowAverages — null history hides the history panel.
    ///   ShowAverages — with history does not throw (null UI refs safe).
    ///
    /// Total: 20 new EditMode tests.
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

        private static MatchDamageHistorySO CreateHistory(int maxHistory = 10)
        {
            var h = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            SetField(h, "_maxHistory", maxHistory);
            // Re-init the buffer to respect the new maxHistory value.
            InvokePrivate(h, "InitBuffer");
            return h;
        }

        private static DamageTypeSnapshot MakeSnapshot(
            float physical = 0f, float energy = 0f,
            float thermal  = 0f, float shock   = 0f)
        {
            return new DamageTypeSnapshot
            {
                physical = physical,
                energy   = energy,
                thermal  = thermal,
                shock    = shock,
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        // MatchDamageHistorySO Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void FreshInstance_MaxHistory_DefaultsToTen()
        {
            var h = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            Assert.AreEqual(10, h.MaxHistory,
                "MaxHistory should default to 10.");
            Object.DestroyImmediate(h);
        }

        [Test]
        public void FreshInstance_Count_IsZero()
        {
            var h = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            Assert.AreEqual(0, h.Count,
                "Count should be 0 on a fresh instance (empty ring buffer).");
            Object.DestroyImmediate(h);
        }

        [Test]
        public void AddEntry_IncreasesCount()
        {
            var h = CreateHistory(maxHistory: 5);
            h.AddEntry(MakeSnapshot(physical: 10f));
            Assert.AreEqual(1, h.Count,
                "Count must increase by 1 after AddEntry.");
            Object.DestroyImmediate(h);
        }

        [Test]
        public void AddEntry_CountCapped_AtMaxHistory()
        {
            var h = CreateHistory(maxHistory: 3);
            for (int i = 0; i < 5; i++)
                h.AddEntry(MakeSnapshot(physical: 10f));
            Assert.AreEqual(3, h.Count,
                "Count must not exceed MaxHistory once the ring buffer is full.");
            Object.DestroyImmediate(h);
        }

        [Test]
        public void GetRollingAverage_EmptyHistory_ReturnsZero()
        {
            var h = CreateHistory();
            Assert.AreEqual(0f, h.GetRollingAverage(DamageType.Physical), 1e-5f,
                "GetRollingAverage must return 0 for an empty history.");
            Object.DestroyImmediate(h);
        }

        [Test]
        public void GetRollingAverage_SingleEntry_Physical_ReturnsValue()
        {
            var h = CreateHistory();
            h.AddEntry(MakeSnapshot(physical: 42f));
            Assert.AreEqual(42f, h.GetRollingAverage(DamageType.Physical), 1e-5f,
                "Rolling average of a single entry must equal that entry's value.");
            Object.DestroyImmediate(h);
        }

        [Test]
        public void GetRollingAverage_TwoEntries_ReturnsAverage()
        {
            var h = CreateHistory();
            h.AddEntry(MakeSnapshot(energy: 20f));
            h.AddEntry(MakeSnapshot(energy: 40f));
            // (20 + 40) / 2 = 30
            Assert.AreEqual(30f, h.GetRollingAverage(DamageType.Energy), 1e-5f,
                "Rolling average of two entries must equal their mean.");
            Object.DestroyImmediate(h);
        }

        [Test]
        public void GetRollingAverage_OverflowRingBuffer_ReturnsCorrectAverage()
        {
            // maxHistory = 3; add 4 entries — oldest is overwritten.
            var h = CreateHistory(maxHistory: 3);
            h.AddEntry(MakeSnapshot(thermal: 10f));  // entry A — will be evicted
            h.AddEntry(MakeSnapshot(thermal: 20f));  // entry B
            h.AddEntry(MakeSnapshot(thermal: 30f));  // entry C
            h.AddEntry(MakeSnapshot(thermal: 40f));  // entry D — overwrites A

            // Ring buffer now holds B=20, C=30, D=40.  Avg = (20+30+40)/3 = 30.
            Assert.AreEqual(30f, h.GetRollingAverage(DamageType.Thermal), 1e-4f,
                "After overflow, rolling average must use the last MaxHistory entries only.");
            Object.DestroyImmediate(h);
        }

        [Test]
        public void Clear_ResetsCount()
        {
            var h = CreateHistory();
            h.AddEntry(MakeSnapshot(physical: 10f));
            h.AddEntry(MakeSnapshot(physical: 20f));
            h.Clear();
            Assert.AreEqual(0, h.Count,
                "Clear must reset Count to 0.");
            Object.DestroyImmediate(h);
        }

        [Test]
        public void GetRollingAverage_AfterClear_ReturnsZero()
        {
            var h = CreateHistory();
            h.AddEntry(MakeSnapshot(shock: 50f));
            h.Clear();
            Assert.AreEqual(0f, h.GetRollingAverage(DamageType.Shock), 1e-5f,
                "GetRollingAverage must return 0 after Clear.");
            Object.DestroyImmediate(h);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PostMatchDamageHistoryController Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Controller_FreshInstance_History_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PostMatchDamageHistoryController>();
            Assert.IsNull(ctl.History,
                "History should default to null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_FreshInstance_MatchStatistics_IsNull()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PostMatchDamageHistoryController>();
            Assert.IsNull(ctl.MatchStatistics,
                "MatchStatistics should default to null.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PostMatchDamageHistoryController>();
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnEnable"),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var ctl = go.AddComponent<PostMatchDamageHistoryController>();
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnDisable"),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_UnregistersFromOnMatchEnded()
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
                "After OnDisable, the controller must have unregistered its OnMatchEnded delegate.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_OnMatchEnded_NullHistory_DoesNotThrow()
        {
            var go       = new GameObject();
            var ctl      = go.AddComponent<PostMatchDamageHistoryController>();
            var stats    = ScriptableObject.CreateInstance<MatchStatisticsSO>();
            SetField(ctl, "_history",          null);
            SetField(ctl, "_matchStatistics",  stats);
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnMatchEnded"),
                "OnMatchEnded with null _history must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void Controller_OnMatchEnded_NullMatchStatistics_DoesNotThrow()
        {
            var go      = new GameObject();
            var ctl     = go.AddComponent<PostMatchDamageHistoryController>();
            var history = CreateHistory();
            SetField(ctl, "_history",         history);
            SetField(ctl, "_matchStatistics", null);
            Assert.DoesNotThrow(() => InvokePrivate(ctl, "OnMatchEnded"),
                "OnMatchEnded with null _matchStatistics must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Controller_OnMatchEnded_AddsEntryToHistory()
        {
            var go      = new GameObject();
            var ctl     = go.AddComponent<PostMatchDamageHistoryController>();
            var history = CreateHistory();
            var stats   = ScriptableObject.CreateInstance<MatchStatisticsSO>();

            // Record some damage so the snapshot is non-trivial.
            stats.RecordDamageDealt(new DamageInfo { amount = 25f, damageType = DamageType.Energy });

            SetField(ctl, "_history",         history);
            SetField(ctl, "_matchStatistics", stats);

            Assert.AreEqual(0, history.Count, "Precondition: history starts empty.");
            InvokePrivate(ctl, "OnMatchEnded");
            Assert.AreEqual(1, history.Count,
                "OnMatchEnded must add exactly one snapshot to the history.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void Controller_ShowAverages_NullHistory_HidesPanel()
        {
            var go      = new GameObject();
            var panelGo = new GameObject();
            var ctl     = go.AddComponent<PostMatchDamageHistoryController>();

            panelGo.SetActive(true);
            SetField(ctl, "_history",      null);
            SetField(ctl, "_historyPanel", panelGo);

            ctl.ShowAverages();

            Assert.IsFalse(panelGo.activeSelf,
                "ShowAverages must hide the panel when _history is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        [Test]
        public void Controller_ShowAverages_WithHistory_DoesNotThrow()
        {
            var go      = new GameObject();
            var ctl     = go.AddComponent<PostMatchDamageHistoryController>();
            var history = CreateHistory();
            history.AddEntry(MakeSnapshot(physical: 10f, energy: 20f));

            SetField(ctl, "_history", history);
            // All text fields are null — no exception expected.
            Assert.DoesNotThrow(() => ctl.ShowAverages(),
                "ShowAverages with a non-null history and null UI refs must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
        }
    }
}
