using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T193 — <see cref="PostPrestigeHistoryController"/>.
    ///
    /// PostPrestigeHistoryControllerTests (8):
    ///   Ctrl_FreshInstance_HistoryIsNull                   ×1
    ///   Ctrl_FreshInstance_PrestigeSystemIsNull            ×1
    ///   Ctrl_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Ctrl_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Ctrl_OnDisable_Unregisters_PrestigeChannel         ×1
    ///   Ctrl_OnPrestige_NullGuards_DoesNotThrow            ×1
    ///   Ctrl_OnPrestige_AddsEntryToHistory                 ×1
    ///   Ctrl_Refresh_EmptyHistory_ShowsEmptyLabel          ×1
    /// </summary>
    public class PostPrestigeHistoryControllerTests
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

        private static PostPrestigeHistoryController CreateCtrl()
        {
            var go = new GameObject("PostPrestigeHistoryCtrl_Test");
            return go.AddComponent<PostPrestigeHistoryController>();
        }

        private static PrestigeHistorySO CreateHistory() =>
            ScriptableObject.CreateInstance<PrestigeHistorySO>();

        private static PrestigeSystemSO CreatePrestige(int count = 1)
        {
            var so = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            so.LoadSnapshot(count);
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── FreshInstance ─────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_HistoryIsNull()
        {
            var ctrl = CreateCtrl();
            Assert.IsNull(ctrl.History,
                "Fresh controller must have null History.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_PrestigeSystemIsNull()
        {
            var ctrl = CreateCtrl();
            Assert.IsNull(ctrl.PrestigeSystem,
                "Fresh controller must have null PrestigeSystem.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle null safety ─────────────────────────────────────────────

        [Test]
        public void Ctrl_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_PrestigeChannel()
        {
            var ctrl = CreateCtrl();
            var ch   = CreateEvent();
            SetField(ctrl, "_onPrestige", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "OnDisable must unregister from _onPrestige " +
                "(only the external counter must fire after unsubscribe).");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        // ── OnPrestige / AddEntry ─────────────────────────────────────────────

        [Test]
        public void Ctrl_OnPrestige_NullGuards_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            // Both _history and _prestigeSystem are null — must be a silent no-op.
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnPrestige"),
                "OnPrestige with null history and null prestige system must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnPrestige_AddsEntryToHistory()
        {
            var ctrl     = CreateCtrl();
            var history  = CreateHistory();
            var prestige = CreatePrestige(1); // count=1, label="Bronze I"
            SetField(ctrl, "_history",        history);
            SetField(ctrl, "_prestigeSystem", prestige);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnPrestige");

            Assert.AreEqual(1, history.Count,
                "OnPrestige must add one entry to the PrestigeHistorySO.");

            PrestigeHistoryEntry? latest = history.GetLatest();
            Assert.IsTrue(latest.HasValue);
            Assert.AreEqual(1,          latest.Value.prestigeCount,
                "Entry prestigeCount must match PrestigeSystemSO.PrestigeCount.");
            Assert.AreEqual("Bronze I", latest.Value.rankLabel,
                "Entry rankLabel must match PrestigeSystemSO.GetRankLabel().");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(prestige);
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_Refresh_EmptyHistory_ShowsEmptyLabel()
        {
            var ctrl    = CreateCtrl();
            var history = CreateHistory();         // empty (0 entries)
            var labelGO = new GameObject("EmptyLabel");
            labelGO.SetActive(false);
            SetField(ctrl, "_history",    history);
            SetField(ctrl, "_emptyLabel", labelGO);

            ctrl.Refresh();

            Assert.IsTrue(labelGO.activeSelf,
                "Refresh with an empty history must activate the _emptyLabel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(history);
        }
    }
}
