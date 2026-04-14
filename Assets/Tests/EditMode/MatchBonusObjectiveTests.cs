using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T229: <see cref="MatchBonusObjectiveSO"/> and
    /// <see cref="BonusObjectiveHUDController"/>.
    ///
    /// MatchBonusObjectiveSOTests (8):
    ///   FreshInstance_IsCompleted_False                          ×1
    ///   FreshInstance_BonusReward_Is100                          ×1
    ///   FreshInstance_ObjectiveTitle_NotEmpty                    ×1
    ///   Complete_SetsIsCompleted_True                            ×1
    ///   Complete_FiresOnCompletedEvent                           ×1
    ///   Complete_CalledTwice_IsCompleted_StaysTrue               ×1
    ///   Reset_ClearsIsCompleted                                  ×1
    ///   Reset_Silent_DoesNotFireEvent                            ×1
    ///
    /// BonusObjectiveHUDControllerTests (6):
    ///   FreshInstance_BonusObjectiveNull                         ×1
    ///   OnEnable_NullRefs_DoesNotThrow                           ×1
    ///   OnDisable_Unregisters                                    ×1
    ///   Refresh_NullSO_HidesPanel                                ×1
    ///   Refresh_WithSO_ShowsPanel                                ×1
    ///   Refresh_Completed_ShowsOverlay                           ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class MatchBonusObjectiveSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static MatchBonusObjectiveSO MakeSO()
        {
            var so = ScriptableObject.CreateInstance<MatchBonusObjectiveSO>();
            typeof(MatchBonusObjectiveSO)
                .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)?
                .Invoke(so, null);
            return so;
        }

        private static VoidGameEvent MakeEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_IsCompleted_False()
        {
            var so = MakeSO();
            Assert.IsFalse(so.IsCompleted,
                "Fresh MatchBonusObjectiveSO must not be completed.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_BonusReward_Is100()
        {
            var so = MakeSO();
            Assert.AreEqual(100, so.BonusReward,
                "Default BonusReward must be 100.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_ObjectiveTitle_NotEmpty()
        {
            var so = MakeSO();
            Assert.IsFalse(string.IsNullOrEmpty(so.ObjectiveTitle),
                "ObjectiveTitle must not be empty on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        // ── Complete ──────────────────────────────────────────────────────────

        [Test]
        public void Complete_SetsIsCompleted_True()
        {
            var so = MakeSO();
            so.Complete();
            Assert.IsTrue(so.IsCompleted,
                "IsCompleted must be true after Complete() is called.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Complete_FiresOnCompletedEvent()
        {
            var so  = MakeSO();
            var evt = MakeEvent();
            SetField(so, "_onCompleted", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.Complete();

            Assert.AreEqual(1, fired,
                "_onCompleted must be raised once when Complete() is called.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Complete_CalledTwice_IsCompleted_StaysTrue()
        {
            var so = MakeSO();
            so.Complete();
            so.Complete();
            Assert.IsTrue(so.IsCompleted,
                "IsCompleted must remain true after calling Complete() a second time.");
            Object.DestroyImmediate(so);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsIsCompleted()
        {
            var so = MakeSO();
            so.Complete();
            so.Reset();
            Assert.IsFalse(so.IsCompleted,
                "IsCompleted must be false after Reset().");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Reset_Silent_DoesNotFireEvent()
        {
            var so  = MakeSO();
            var evt = MakeEvent();
            SetField(so, "_onCompleted", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.Complete();   // fires once
            fired = 0;       // reset counter

            so.Reset();      // must be silent

            Assert.AreEqual(0, fired,
                "Reset() must not fire _onCompleted.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BonusObjectiveHUDController tests
    // ═══════════════════════════════════════════════════════════════════════════

    public class BonusObjectiveHUDControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static BonusObjectiveHUDController MakeController(out GameObject go)
        {
            go = new GameObject("BonusObjectiveHUDCtrl_Test");
            go.SetActive(false);
            return go.AddComponent<BonusObjectiveHUDController>();
        }

        private static MatchBonusObjectiveSO MakeSO()
        {
            var so = ScriptableObject.CreateInstance<MatchBonusObjectiveSO>();
            typeof(MatchBonusObjectiveSO)
                .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)?
                .Invoke(so, null);
            return so;
        }

        private static VoidGameEvent MakeEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_BonusObjectiveNull()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<BonusObjectiveHUDController>();
            Assert.IsNull(ctrl.BonusObjective,
                "Fresh BonusObjectiveHUDController must have null BonusObjective.");
            Object.DestroyImmediate(go);
        }

        // ── OnEnable / OnDisable ──────────────────────────────────────────────

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var channel  = MakeEvent();
            int external = 0;
            channel.RegisterCallback(() => external++);

            MakeController(out GameObject go);
            var ctrl = go.GetComponent<BonusObjectiveHUDController>();
            SetField(ctrl, "_onCompleted", channel);

            go.SetActive(true);   // Awake + OnEnable → subscribed
            go.SetActive(false);  // OnDisable → must unsubscribe

            channel.Raise();

            Assert.AreEqual(1, external,
                "After OnDisable only the external callback must fire.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        [Test]
        public void Refresh_NullSO_HidesPanel()
        {
            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<BonusObjectiveHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);

            go.SetActive(true); // OnEnable → Refresh()

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when _bonusObjective is null.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_WithSO_ShowsPanel()
        {
            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<BonusObjectiveHUDController>();
            var so    = MakeSO();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            SetField(ctrl, "_bonusObjective", so);
            SetField(ctrl, "_panel",          panel);

            go.SetActive(true); // OnEnable → Refresh()

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when a valid _bonusObjective is assigned.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_Completed_ShowsOverlay()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<BonusObjectiveHUDController>();
            var so      = MakeSO();
            var overlay = new GameObject("CompletedOverlay");
            overlay.SetActive(false);

            so.Complete(); // mark as completed before wiring

            SetField(ctrl, "_bonusObjective",  so);
            SetField(ctrl, "_completedOverlay", overlay);

            go.SetActive(true); // OnEnable → Refresh()

            Assert.IsTrue(overlay.activeSelf,
                "Completed overlay must be shown when IsCompleted is true.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(overlay);
        }
    }
}
