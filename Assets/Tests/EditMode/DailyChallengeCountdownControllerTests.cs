using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T203:
    ///   <see cref="DailyChallengeCountdownController"/>.
    ///
    /// DailyChallengeCountdownControllerTests (10):
    ///   FreshInstance_ChallengeIsNull                      ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow                  ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow                 ×1
    ///   OnDisable_HidesPanelAndUnregisters                 ×1
    ///   Refresh_NullChallenge_HidesPanel                   ×1
    ///   Refresh_NotCompleted_ShowsPanel                    ×1
    ///   Refresh_NotCompleted_LabelContainsResetsIn         ×1
    ///   Refresh_IsCompleted_ShowsCompletedText             ×1
    ///   Refresh_NullLabel_DoesNotThrow                     ×1
    ///   OnMatchEnded_TriggersRefresh                       ×1
    ///
    /// Total: 10 new EditMode tests.
    /// </summary>
    public class DailyChallengeCountdownControllerTests
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

        /// <summary>
        /// Creates a DailyChallengeSO in either completed or not-completed state.
        /// When <paramref name="completed"/> is true the SO's IsCompleted flag is set
        /// via the public MarkCompleted() method.
        /// </summary>
        private static DailyChallengeSO CreateChallenge(bool completed = false)
        {
            var so = ScriptableObject.CreateInstance<DailyChallengeSO>();
            if (completed)
            {
                // LoadSnapshot with today's date so IsCompleted can be set.
                so.LoadSnapshot(DailyChallengeSO.TodayUtcString(), 0, true);
            }
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static DailyChallengeCountdownController CreateController()
        {
            var go = new GameObject("DailyChallengeCountdownCtrl_Test");
            return go.AddComponent<DailyChallengeCountdownController>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_ChallengeIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Challenge);
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
        public void Ctrl_OnDisable_HidesPanelAndUnregisters()
        {
            var ctrl  = CreateController();
            var ch    = CreateEvent();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_onMatchEnded",    ch);
            SetField(ctrl, "_countdownPanel", panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.IsFalse(panel.activeSelf, "OnDisable must hide the countdown panel.");
            Assert.AreEqual(1, callCount,
                "After OnDisable only the manually registered callback should fire.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_Refresh_NullChallenge_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_countdownPanel", panel);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Null DailyChallengeSO must hide the countdown panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Ctrl_Refresh_NotCompleted_ShowsPanel()
        {
            var ctrl      = CreateController();
            var challenge = CreateChallenge(completed: false);
            var panel     = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_challenge",      challenge);
            SetField(ctrl, "_countdownPanel", panel);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Assigned DailyChallengeSO must show the countdown panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(challenge);
        }

        [Test]
        public void Ctrl_Refresh_NotCompleted_LabelContainsResetsIn()
        {
            var ctrl      = CreateController();
            var challenge = CreateChallenge(completed: false);
            var labelGO   = new GameObject("Label");
            var label     = labelGO.AddComponent<Text>();

            SetField(ctrl, "_challenge",       challenge);
            SetField(ctrl, "_countdownLabel", label);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            StringAssert.Contains("Resets in", label.text,
                "Not-completed challenge must show a 'Resets in …' countdown.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(challenge);
        }

        [Test]
        public void Ctrl_Refresh_IsCompleted_ShowsCompletedText()
        {
            var ctrl      = CreateController();
            var challenge = CreateChallenge(completed: true);
            var labelGO   = new GameObject("Label");
            var label     = labelGO.AddComponent<Text>();

            SetField(ctrl, "_challenge",       challenge);
            SetField(ctrl, "_countdownLabel", label);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            StringAssert.Contains("complete", label.text,
                "Completed challenge must show a completion message.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(challenge);
        }

        [Test]
        public void Ctrl_Refresh_NullLabel_DoesNotThrow()
        {
            var ctrl      = CreateController();
            var challenge = CreateChallenge(completed: false);
            SetField(ctrl, "_challenge", challenge);
            // _countdownLabel intentionally null

            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh() must not throw when _countdownLabel is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(challenge);
        }

        [Test]
        public void Ctrl_OnMatchEnded_TriggersRefresh()
        {
            var ctrl      = CreateController();
            var ch        = CreateEvent();
            var challenge = CreateChallenge(completed: true); // starts complete
            var labelGO   = new GameObject("Label");
            var label     = labelGO.AddComponent<Text>();

            SetField(ctrl, "_challenge",       challenge);
            SetField(ctrl, "_onMatchEnded",    ch);
            SetField(ctrl, "_countdownLabel", label);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable"); // Refresh: shows "Challenge complete!"

            // Reset to not-completed state then raise the event.
            challenge.Reset();
            ch.Raise(); // triggers Refresh() → now not-completed

            StringAssert.Contains("Resets in", label.text,
                "OnMatchEnded must trigger Refresh(), updating the label to the countdown.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(challenge);
            Object.DestroyImmediate(ch);
        }
    }
}
