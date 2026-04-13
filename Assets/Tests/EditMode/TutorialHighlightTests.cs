using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // TutorialHighlightConfig tests
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EditMode tests for <see cref="TutorialHighlightConfig"/>.
    ///
    /// Covers:
    ///   • FreshInstance: Entries is non-null and empty.
    ///   • Entries returns IReadOnlyList.
    ///   • GetTagForStep: null / whitespace → string.Empty.
    ///   • GetTagForStep: no matching entry → string.Empty.
    ///   • GetTagForStep: single matching entry → correct tag.
    ///   • GetTagForStep: multiple entries → returns the correct one.
    ///   • GetTagForStep: null entry in list → silently skipped, no throw.
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class TutorialHighlightConfigTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── SetUp / TearDown ──────────────────────────────────────────────────

        private TutorialHighlightConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TutorialHighlightConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_Entries_NotNull()
        {
            Assert.IsNotNull(_config.Entries,
                "Fresh TutorialHighlightConfig.Entries must not be null.");
        }

        [Test]
        public void FreshInstance_Entries_IsEmpty()
        {
            Assert.AreEqual(0, _config.Entries.Count,
                "Fresh TutorialHighlightConfig.Entries must be empty.");
        }

        [Test]
        public void Entries_ReturnsIReadOnlyList()
        {
            Assert.IsInstanceOf<IReadOnlyList<TutorialHighlightEntry>>(_config.Entries,
                "Entries must implement IReadOnlyList<TutorialHighlightEntry>.");
        }

        // ── GetTagForStep — null / whitespace guards ──────────────────────────

        [Test]
        public void GetTagForStep_Null_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, _config.GetTagForStep(null),
                "GetTagForStep(null) must return string.Empty.");
        }

        [Test]
        public void GetTagForStep_Whitespace_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, _config.GetTagForStep("   "),
                "GetTagForStep with whitespace-only input must return string.Empty.");
        }

        // ── GetTagForStep — no match ──────────────────────────────────────────

        [Test]
        public void GetTagForStep_NoMatch_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, _config.GetTagForStep("step_unknown"),
                "GetTagForStep with no matching entry must return string.Empty.");
        }

        // ── GetTagForStep — happy path ────────────────────────────────────────

        [Test]
        public void GetTagForStep_SingleEntry_ReturnsTag()
        {
            var entry = new TutorialHighlightEntry { stepId = "step_01", targetTag = "ShopButton" };
            SetField(_config, "_entries", new List<TutorialHighlightEntry> { entry });

            Assert.AreEqual("ShopButton", _config.GetTagForStep("step_01"),
                "GetTagForStep must return the configured tag for a matching entry.");
        }

        [Test]
        public void GetTagForStep_MultipleEntries_ReturnsCorrectTag()
        {
            var e1 = new TutorialHighlightEntry { stepId = "step_01", targetTag = "PartPanel" };
            var e2 = new TutorialHighlightEntry { stepId = "step_02", targetTag = "ArenaButton" };
            SetField(_config, "_entries", new List<TutorialHighlightEntry> { e1, e2 });

            Assert.AreEqual("ArenaButton", _config.GetTagForStep("step_02"),
                "GetTagForStep must return the correct tag when multiple entries are present.");
        }

        // ── GetTagForStep — null entry in list ────────────────────────────────

        [Test]
        public void GetTagForStep_NullEntryInList_DoesNotThrow()
        {
            var entries = new List<TutorialHighlightEntry> { null };
            SetField(_config, "_entries", entries);

            Assert.DoesNotThrow(() => _config.GetTagForStep("step_01"),
                "GetTagForStep must silently skip null list entries without throwing.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TutorialHighlightOverlay tests
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EditMode tests for <see cref="TutorialHighlightOverlay"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null refs → no throw.
    ///   • OnEnable / OnDisable with null channels → no throw.
    ///   • OnDisable unregisters from _onStepCompleted.
    ///   • OnDisable unregisters from _onTutorialCompleted.
    ///   • RefreshHighlight with null _config → hides panel.
    ///   • RefreshHighlight with null _progress → hides panel.
    ///   • RefreshHighlight with null _sequence → hides panel.
    ///   • RefreshHighlight with complete progress → hides panel.
    ///   • RefreshHighlight with no config entry for current step → hides panel.
    ///   • Hide() sets _overlayPanel inactive.
    ///   • OnTutorialCompleted channel fires → Hide() called.
    ///   • OnStepCompleted channel fires → RefreshHighlight() called.
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class TutorialHighlightOverlayTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static TutorialHighlightOverlay MakeOverlay(out GameObject go)
        {
            go = new GameObject("TutorialHighlightOverlayTest");
            go.SetActive(false); // prevent OnEnable before fields are set
            return go.AddComponent<TutorialHighlightOverlay>();
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeOverlay(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeOverlay(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannels_DoesNotThrow()
        {
            MakeOverlay(out GameObject go);
            var overlay  = go.GetComponent<TutorialHighlightOverlay>();
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            SetField(overlay, "_progress", progress);
            // both event channels remain null
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null channels must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(progress);
        }

        [Test]
        public void OnDisable_NullChannels_DoesNotThrow()
        {
            MakeOverlay(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null channels must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromOnStepCompleted()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialHighlightOverlay>();
            SetField(overlay, "_onStepCompleted", channel);

            go.SetActive(true);   // Awake + OnEnable → subscribed
            go.SetActive(false);  // OnDisable → must unsubscribe

            channel.Raise();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter must fire; overlay must be unsubscribed after OnDisable.");
        }

        [Test]
        public void OnDisable_UnregistersFromOnTutorialCompleted()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialHighlightOverlay>();
            SetField(overlay, "_onTutorialCompleted", channel);

            go.SetActive(true);
            go.SetActive(false);

            channel.Raise();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter must fire; overlay must be unsubscribed after OnDisable.");
        }

        // ── RefreshHighlight — guards hide overlay ────────────────────────────

        [Test]
        public void RefreshHighlight_NullConfig_HidesOverlayPanel()
        {
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialHighlightOverlay>();
            var panelGo = new GameObject("OverlayPanel");
            panelGo.SetActive(true);

            SetField(overlay, "_overlayPanel", panelGo);
            // _config remains null

            go.SetActive(true); // OnEnable → RefreshHighlight → null config → Hide

            Assert.IsFalse(panelGo.activeSelf,
                "Overlay panel must be hidden when _config is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        [Test]
        public void RefreshHighlight_NullProgress_HidesOverlayPanel()
        {
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialHighlightOverlay>();
            var panelGo = new GameObject("OverlayPanel");
            panelGo.SetActive(true);

            SetField(overlay, "_overlayPanel", panelGo);
            // _progress remains null

            go.SetActive(true);

            Assert.IsFalse(panelGo.activeSelf,
                "Overlay panel must be hidden when _progress is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        [Test]
        public void RefreshHighlight_NullSequence_HidesOverlayPanel()
        {
            MakeOverlay(out GameObject go);
            var overlay  = go.GetComponent<TutorialHighlightOverlay>();
            var panelGo  = new GameObject("OverlayPanel");
            panelGo.SetActive(true);
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            var config   = ScriptableObject.CreateInstance<TutorialHighlightConfig>();

            SetField(overlay, "_overlayPanel", panelGo);
            SetField(overlay, "_progress",     progress);
            SetField(overlay, "_config",       config);
            // _sequence remains null

            go.SetActive(true);

            Assert.IsFalse(panelGo.activeSelf,
                "Overlay panel must be hidden when _sequence is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(progress);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void RefreshHighlight_CompleteProgress_HidesOverlayPanel()
        {
            MakeOverlay(out GameObject go);
            var overlay  = go.GetComponent<TutorialHighlightOverlay>();
            var panelGo  = new GameObject("OverlayPanel");
            panelGo.SetActive(true);
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            progress.Complete(); // tutorial already finished

            SetField(overlay, "_overlayPanel", panelGo);
            SetField(overlay, "_progress",     progress);

            go.SetActive(true);

            Assert.IsFalse(panelGo.activeSelf,
                "Overlay panel must be hidden when progress.IsComplete is true.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(progress);
        }

        [Test]
        public void RefreshHighlight_NoTagForCurrentStep_HidesOverlayPanel()
        {
            MakeOverlay(out GameObject go);
            var overlay  = go.GetComponent<TutorialHighlightOverlay>();
            var panelGo  = new GameObject("OverlayPanel");
            panelGo.SetActive(true);

            // Sequence with one incomplete step (step_01).
            var step = ScriptableObject.CreateInstance<TutorialStepSO>();
            SetField(step, "_stepId", "step_01");
            var sequence = ScriptableObject.CreateInstance<TutorialSequenceSO>();
            SetField(sequence, "_steps", new List<TutorialStepSO> { step });

            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            // step_01 is not complete → it is the current step.

            // Config has no entry for step_01 → tag is empty → Hide.
            var config = ScriptableObject.CreateInstance<TutorialHighlightConfig>();

            SetField(overlay, "_overlayPanel", panelGo);
            SetField(overlay, "_sequence",     sequence);
            SetField(overlay, "_progress",     progress);
            SetField(overlay, "_config",       config);

            go.SetActive(true); // RefreshHighlight → empty tag → Hide

            Assert.IsFalse(panelGo.activeSelf,
                "Overlay panel must be hidden when no highlight tag is configured for the current step.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(step);
            Object.DestroyImmediate(sequence);
            Object.DestroyImmediate(progress);
            Object.DestroyImmediate(config);
        }

        // ── Hide ──────────────────────────────────────────────────────────────

        [Test]
        public void Hide_SetsOverlayPanelInactive()
        {
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialHighlightOverlay>();
            var panelGo = new GameObject("OverlayPanel");
            panelGo.SetActive(true);

            SetField(overlay, "_overlayPanel", panelGo);
            go.SetActive(true);

            overlay.Hide();

            Assert.IsFalse(panelGo.activeSelf,
                "Hide() must set the overlay panel inactive.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        // ── Event-driven refresh / hide ───────────────────────────────────────

        [Test]
        public void OnTutorialCompleted_Raise_HidesOverlayPanel()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialHighlightOverlay>();
            var panelGo = new GameObject("OverlayPanel");
            panelGo.SetActive(true);

            SetField(overlay, "_onTutorialCompleted", channel);
            SetField(overlay, "_overlayPanel",        panelGo);

            go.SetActive(true); // subscribe

            // Manually make panel visible to confirm Hide is called on event.
            panelGo.SetActive(true);
            channel.Raise(); // → Hide()

            Assert.IsFalse(panelGo.activeSelf,
                "Overlay panel must be hidden when _onTutorialCompleted is raised.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnStepCompleted_Raise_CallsRefreshHighlight_NullConfigHides()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialHighlightOverlay>();
            var panelGo = new GameObject("OverlayPanel");
            panelGo.SetActive(true);

            SetField(overlay, "_onStepCompleted", channel);
            SetField(overlay, "_overlayPanel",    panelGo);
            // _config null → RefreshHighlight will call Hide()

            go.SetActive(true);

            // Manually set panel active, then fire step-completed event.
            panelGo.SetActive(true);
            channel.Raise(); // → RefreshHighlight → null config → Hide

            Assert.IsFalse(panelGo.activeSelf,
                "Overlay panel must be hidden when _onStepCompleted fires and config is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(channel);
        }
    }
}
