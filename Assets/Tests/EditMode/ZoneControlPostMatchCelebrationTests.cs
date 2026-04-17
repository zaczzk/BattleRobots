using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T344: <see cref="ZoneControlPostMatchCelebrationSO"/> and
    /// <see cref="ZoneControlPostMatchCelebrationController"/>.
    ///
    /// ZoneControlPostMatchCelebrationTests (12):
    ///   SO_FreshInstance_IsRunning_False                                  ×1
    ///   SO_FreshInstance_CurrentStep_IsWinner                             ×1
    ///   SO_StartCelebration_SetsIsRunning_True                            ×1
    ///   SO_StartCelebration_SetsCurrentStep_Winner                        ×1
    ///   SO_Tick_AdvancesStep_AfterDuration                                ×1
    ///   SO_Tick_SetsIsRunning_False_WhenComplete                          ×1
    ///   SO_Reset_ClearsRunningState                                       ×1
    ///   SO_Tick_NoOp_WhenNotRunning                                       ×1
    ///   Controller_OnEnable_AllNullRefs_DoesNotThrow                      ×1
    ///   Controller_OnDisable_AllNullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_Unregisters_Channel                          ×1
    ///   Controller_HandleMatchEnded_CallsStartCelebration                 ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlPostMatchCelebrationTests
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

        private static ZoneControlPostMatchCelebrationSO CreateCelebrationSO(float stepDuration = 1f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlPostMatchCelebrationSO>();
            SetField(so, "_stepDuration", stepDuration);
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsRunning_False()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlPostMatchCelebrationSO>();
            Assert.IsFalse(so.IsRunning,
                "IsRunning must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CurrentStep_IsWinner()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlPostMatchCelebrationSO>();
            Assert.AreEqual(CelebrationStep.Winner, so.CurrentStep,
                "CurrentStep must be Winner on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartCelebration_SetsIsRunning_True()
        {
            var so = CreateCelebrationSO();
            so.StartCelebration();
            Assert.IsTrue(so.IsRunning,
                "IsRunning must be true after StartCelebration.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartCelebration_SetsCurrentStep_Winner()
        {
            var so = CreateCelebrationSO();
            so.StartCelebration();
            Assert.AreEqual(CelebrationStep.Winner, so.CurrentStep,
                "CurrentStep must be Winner immediately after StartCelebration.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_AdvancesStep_AfterDuration()
        {
            var so = CreateCelebrationSO(stepDuration: 1f);
            so.StartCelebration();

            // Tick past the first step.
            so.Tick(1.1f);

            Assert.AreEqual(CelebrationStep.ScoreTally, so.CurrentStep,
                "CurrentStep must advance to ScoreTally after ticking past the step duration.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_SetsIsRunning_False_WhenComplete()
        {
            var so = CreateCelebrationSO(stepDuration: 0.1f);
            so.StartCelebration();

            // Tick through all steps: Winner → ScoreTally → MVPReveal → Complete.
            so.Tick(0.2f);  // Winner → ScoreTally
            so.Tick(0.2f);  // ScoreTally → MVPReveal
            so.Tick(0.2f);  // MVPReveal → Complete

            Assert.IsFalse(so.IsRunning,
                "IsRunning must be false once the sequence reaches Complete.");
            Assert.AreEqual(CelebrationStep.Complete, so.CurrentStep,
                "CurrentStep must be Complete at end of sequence.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsRunningState()
        {
            var so = CreateCelebrationSO();
            so.StartCelebration();
            so.Reset();

            Assert.IsFalse(so.IsRunning,
                "IsRunning must be false after Reset.");
            Assert.AreEqual(CelebrationStep.Winner, so.CurrentStep,
                "CurrentStep must return to Winner after Reset.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_NoOp_WhenNotRunning()
        {
            var so = CreateCelebrationSO();
            // IsRunning is false by default.
            so.Tick(100f);

            Assert.IsFalse(so.IsRunning,
                "Tick must not start the sequence when IsRunning is false.");
            Assert.AreEqual(CelebrationStep.Winner, so.CurrentStep,
                "CurrentStep must remain Winner when Tick is called while not running.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_Celeb_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlPostMatchCelebrationController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_Celeb_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlPostMatchCelebrationController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Celeb_Unregister");
            var ctrl = go.AddComponent<ZoneControlPostMatchCelebrationController>();
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
        public void Controller_HandleMatchEnded_CallsStartCelebration()
        {
            var go            = new GameObject("Test_Celeb_StartCelebration");
            var ctrl          = go.AddComponent<ZoneControlPostMatchCelebrationController>();
            var celebrationSO = CreateCelebrationSO();

            SetField(ctrl, "_celebrationSO", celebrationSO);

            ctrl.HandleMatchEnded();

            Assert.IsTrue(celebrationSO.IsRunning,
                "HandleMatchEnded must call StartCelebration on the celebration SO.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(celebrationSO);
        }
    }
}
