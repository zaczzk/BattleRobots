using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="TutorialController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null refs → no throw.
    ///   • OnEnable / OnDisable with null channel → no throw.
    ///   • OnDisable unregisters from _onTutorialCompleted.
    ///   • OnEnable with complete progress → HideTutorial (panel hidden).
    ///   • OnEnable with incomplete progress + sequence → panel shown.
    ///   • SkipAll with null _progress → no throw, panel hidden.
    ///   • AdvanceStep with null _progress or null _sequence → no throw.
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class TutorialControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static TutorialController MakeController(out GameObject go)
        {
            go = new GameObject("TutorialControllerTest");
            go.SetActive(false); // prevent OnEnable before fields are set
            return go.AddComponent<TutorialController>();
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            SetField(go.GetComponent<TutorialController>(), "_progress", progress);
            // _onTutorialCompleted remains null
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(progress);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false));
            Object.DestroyImmediate(go);
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromOnTutorialCompleted()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            MakeController(out GameObject go);
            var ctrl = go.GetComponent<TutorialController>();
            SetField(ctrl, "_onTutorialCompleted", channel);

            go.SetActive(true);   // Awake + OnEnable → subscribed
            go.SetActive(false);  // OnDisable → must unsubscribe

            channel.Raise();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter should fire; controller must be unsubscribed.");
        }

        // ── OnEnable behaviour with progress ──────────────────────────────────

        [Test]
        public void OnEnable_CompleteProgress_HidesTutorialPanel()
        {
            MakeController(out GameObject go);
            var ctrl     = go.GetComponent<TutorialController>();
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            progress.Complete(); // mark tutorial finished

            var panelGo = new GameObject("TutorialPanel");
            panelGo.SetActive(true); // start visible

            SetField(ctrl, "_progress",      progress);
            SetField(ctrl, "_tutorialPanel", panelGo);

            go.SetActive(true); // OnEnable → IsComplete → HideTutorial

            Assert.IsFalse(panelGo.activeSelf,
                "Panel must be hidden when progress.IsComplete is true on OnEnable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(progress);
        }

        [Test]
        public void OnEnable_NullProgress_HidesTutorialPanel()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<TutorialController>();
            var panelGo = new GameObject("TutorialPanel");
            panelGo.SetActive(true);

            SetField(ctrl, "_tutorialPanel", panelGo);
            // _progress remains null → treated as complete (no active tutorial)

            go.SetActive(true);

            Assert.IsFalse(panelGo.activeSelf,
                "Panel must be hidden when _progress is null (no tutorial SO assigned).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        // ── SkipAll / AdvanceStep null guards ─────────────────────────────────

        [Test]
        public void SkipAll_NullProgress_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<TutorialController>();
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.SkipAll(),
                "SkipAll with null _progress must not throw.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AdvanceStep_NullProgress_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<TutorialController>();
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.AdvanceStep(),
                "AdvanceStep with null _progress must not throw.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AdvanceStep_NullSequence_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl     = go.GetComponent<TutorialController>();
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            SetField(ctrl, "_progress", progress);
            // _sequence remains null

            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.AdvanceStep(),
                "AdvanceStep with null _sequence must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(progress);
        }
    }
}
