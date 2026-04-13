using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="TutorialReplayController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null refs → no throw.
    ///   • OnEnable with null channel → no throw.
    ///   • OnEnable with complete progress → replay button interactable.
    ///   • OnEnable with incomplete progress → replay button non-interactable.
    ///   • OnEnable with null progress → replay button non-interactable.
    ///   • Replay with null _progress → no throw.
    ///   • Replay with null _tutorialController → no throw.
    ///   • Replay resets progress (IsComplete becomes false).
    ///   • Replay disables replay button after reset.
    ///   • Replay with a wired TutorialController + sequence → shows tutorial panel.
    ///   • OnDisable unregisters from _onTutorialCompleted channel.
    ///   • _onTutorialCompleted fires → replay button re-enabled.
    ///   • Awake wires _replayButton.onClick → invoking calls Replay.
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class TutorialReplayControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static TutorialReplayController MakeController(out GameObject go)
        {
            go = new GameObject("TutorialReplayControllerTest");
            go.SetActive(false); // prevent OnEnable before fields are set
            return go.AddComponent<TutorialReplayController>();
        }

        private static Button MakeButton(out GameObject btnGo)
        {
            btnGo = new GameObject("ReplayBtn");
            btnGo.AddComponent<Image>(); // Button requires a Graphic
            return btnGo.AddComponent<Button>();
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<TutorialReplayController>();
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            SetField(ctrl, "_progress", progress);
            // _onTutorialCompleted remains null

            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null _onTutorialCompleted must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(progress);
        }

        // ── OnEnable — replay button state ────────────────────────────────────

        [Test]
        public void OnEnable_CompleteProgress_EnablesReplayButton()
        {
            MakeController(out GameObject go);
            var ctrl     = go.GetComponent<TutorialReplayController>();
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            progress.Complete(); // IsComplete = true

            var btn = MakeButton(out GameObject btnGo);
            btn.interactable = false; // start disabled

            SetField(ctrl, "_progress",     progress);
            SetField(ctrl, "_replayButton", btn);

            go.SetActive(true);

            Assert.IsTrue(btn.interactable,
                "Replay button must be interactable when progress.IsComplete is true.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGo);
            Object.DestroyImmediate(progress);
        }

        [Test]
        public void OnEnable_IncompleteProgress_DisablesReplayButton()
        {
            MakeController(out GameObject go);
            var ctrl     = go.GetComponent<TutorialReplayController>();
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            // progress is fresh — IsComplete = false

            var btn = MakeButton(out GameObject btnGo);
            btn.interactable = true; // start enabled

            SetField(ctrl, "_progress",     progress);
            SetField(ctrl, "_replayButton", btn);

            go.SetActive(true);

            Assert.IsFalse(btn.interactable,
                "Replay button must not be interactable when progress.IsComplete is false.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGo);
            Object.DestroyImmediate(progress);
        }

        [Test]
        public void OnEnable_NullProgress_DisablesReplayButton()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<TutorialReplayController>();
            // _progress remains null

            var btn = MakeButton(out GameObject btnGo);
            btn.interactable = true; // start enabled

            SetField(ctrl, "_replayButton", btn);

            go.SetActive(true);

            Assert.IsFalse(btn.interactable,
                "Replay button must not be interactable when _progress is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGo);
        }

        // ── Replay — null guards ──────────────────────────────────────────────

        [Test]
        public void Replay_NullProgress_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<TutorialReplayController>();
            go.SetActive(true);
            // _progress is null, _tutorialController is null

            Assert.DoesNotThrow(() => ctrl.Replay(),
                "Replay with null _progress must not throw.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Replay_NullTutorialController_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl     = go.GetComponent<TutorialReplayController>();
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            progress.Complete();
            SetField(ctrl, "_progress", progress);
            // _tutorialController remains null

            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.Replay(),
                "Replay with null _tutorialController must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(progress);
        }

        // ── Replay — progress reset ───────────────────────────────────────────

        [Test]
        public void Replay_ResetsProgress_IsCompleteFalse()
        {
            MakeController(out GameObject go);
            var ctrl     = go.GetComponent<TutorialReplayController>();
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            progress.Complete(); // IsComplete = true before replay

            SetField(ctrl, "_progress", progress);
            go.SetActive(true);

            ctrl.Replay();

            Assert.IsFalse(progress.IsComplete,
                "Replay must call _progress.Reset(), making IsComplete false.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(progress);
        }

        [Test]
        public void Replay_DisablesReplayButton()
        {
            MakeController(out GameObject go);
            var ctrl     = go.GetComponent<TutorialReplayController>();
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            progress.Complete(); // IsComplete = true

            var btn = MakeButton(out GameObject btnGo);
            SetField(ctrl, "_progress",     progress);
            SetField(ctrl, "_replayButton", btn);

            go.SetActive(true);
            Assert.IsTrue(btn.interactable); // confirm enabled before replay

            ctrl.Replay(); // Reset() → IsComplete=false → RefreshButton → disabled

            Assert.IsFalse(btn.interactable,
                "Replay button must be disabled after Replay() resets progress.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGo);
            Object.DestroyImmediate(progress);
        }

        // ── Replay — BeginTutorial forwarding ─────────────────────────────────

        [Test]
        public void Replay_WithWiredController_ShowsTutorialPanel()
        {
            // Set up a TutorialController with a sequence and panel on a separate GO.
            var ctrlGo = new GameObject("TutorialControllerGO");
            ctrlGo.SetActive(false);
            var tutorialCtrl = ctrlGo.AddComponent<TutorialController>();

            var step = ScriptableObject.CreateInstance<TutorialStepSO>();
            SetField(step, "_stepId",     "step_replay_test");
            SetField(step, "_headerText", "Replay Header");
            SetField(step, "_bodyText",   "Replay Body");

            var sequence = ScriptableObject.CreateInstance<TutorialSequenceSO>();
            SetField(sequence, "_steps",
                new System.Collections.Generic.List<TutorialStepSO> { step });

            var panelGo = new GameObject("TutorialPanel");
            panelGo.SetActive(false);

            SetField(tutorialCtrl, "_sequence",      sequence);
            SetField(tutorialCtrl, "_tutorialPanel", panelGo);
            // No _progress on TutorialController → OnEnable hides; we call BeginTutorial directly.
            ctrlGo.SetActive(true); // Awake + OnEnable (hides immediately — _progress null)

            // Set up the TutorialReplayController.
            MakeController(out GameObject replayGo);
            var replayCtrl = replayGo.GetComponent<TutorialReplayController>();
            var progress   = ScriptableObject.CreateInstance<TutorialProgressSO>();
            progress.Complete();

            SetField(replayCtrl, "_progress",           progress);
            SetField(replayCtrl, "_tutorialController", tutorialCtrl);

            replayGo.SetActive(true);

            replayCtrl.Replay(); // Reset progress → BeginTutorial → ShowStep(0)

            Assert.IsTrue(panelGo.activeSelf,
                "Replay must call BeginTutorial on the linked controller, showing the tutorial panel.");

            Object.DestroyImmediate(replayGo);
            Object.DestroyImmediate(ctrlGo);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(progress);
            Object.DestroyImmediate(sequence);
            Object.DestroyImmediate(step);
        }

        // ── OnDisable — unregisters from channel ──────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromOnTutorialCompleted()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            MakeController(out GameObject go);
            var ctrl = go.GetComponent<TutorialReplayController>();
            SetField(ctrl, "_onTutorialCompleted", channel);

            go.SetActive(true);  // Awake + OnEnable → subscribed
            go.SetActive(false); // OnDisable → must unsubscribe

            channel.Raise();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter should fire; TutorialReplayController must be unsubscribed after OnDisable.");
        }

        // ── _onTutorialCompleted fires → re-enables button ────────────────────

        [Test]
        public void OnTutorialCompleted_ReenablesReplayButton()
        {
            var channel  = ScriptableObject.CreateInstance<VoidGameEvent>();
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            progress.Complete(); // start complete so button is enabled

            MakeController(out GameObject go);
            var ctrl = go.GetComponent<TutorialReplayController>();
            var btn  = MakeButton(out GameObject btnGo);

            SetField(ctrl, "_progress",            progress);
            SetField(ctrl, "_onTutorialCompleted", channel);
            SetField(ctrl, "_replayButton",        btn);

            go.SetActive(true);
            Assert.IsTrue(btn.interactable); // baseline

            // Simulate replay: reset makes IsComplete false → button disabled.
            ctrl.Replay();
            Assert.IsFalse(btn.interactable, "Button must be disabled during replay.");

            // Simulate tutorial completing again.
            progress.Complete(); // IsComplete = true again
            channel.Raise();    // fires OnTutorialCompletedCallback → RefreshButton

            Assert.IsTrue(btn.interactable,
                "Replay button must be re-enabled when _onTutorialCompleted fires after progress.Complete().");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGo);
            Object.DestroyImmediate(progress);
            Object.DestroyImmediate(channel);
        }

        // ── Awake wiring ──────────────────────────────────────────────────────

        [Test]
        public void Awake_WiresReplayButton_InvokingResetsProgress()
        {
            MakeController(out GameObject go);
            var ctrl     = go.GetComponent<TutorialReplayController>();
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            progress.Complete(); // IsComplete = true

            var btn = MakeButton(out GameObject btnGo);

            SetField(ctrl, "_progress",     progress);
            SetField(ctrl, "_replayButton", btn);

            go.SetActive(true); // Awake fires → wires btn.onClick → Replay

            btn.onClick.Invoke(); // must call Replay() → Reset() → IsComplete false

            Assert.IsFalse(progress.IsComplete,
                "Invoking _replayButton.onClick must call Replay(), which resets progress.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGo);
            Object.DestroyImmediate(progress);
        }
    }
}
