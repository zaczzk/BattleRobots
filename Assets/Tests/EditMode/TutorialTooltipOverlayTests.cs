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
    /// EditMode tests for <see cref="TutorialTooltipOverlay"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null refs → no throw.
    ///   • OnEnable / OnDisable with null event channels → no throw.
    ///   • OnDisable unregisters from _onStepCompleted.
    ///   • OnDisable unregisters from _onTutorialCompleted.
    ///   • Refresh with null _tooltipConfig → hides panel.
    ///   • Refresh with null _highlightConfig → hides panel.
    ///   • Refresh with null _progress → hides panel.
    ///   • Refresh with null _sequence → hides panel.
    ///   • Refresh with complete progress → hides panel.
    ///   • Refresh with no tag for current step → hides panel.
    ///   • Refresh with tag but no tooltip text → hides panel.
    ///   • Refresh with valid full config → shows panel and sets label text.
    ///   • Hide() sets _tooltipPanel inactive.
    ///   • _onTutorialCompleted raised → Hide() called.
    ///   • _onStepCompleted raised → Refresh() called (null config → hides).
    ///   • Refresh with null _highlightFrame → shows panel without throwing.
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class TutorialTooltipOverlayTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static TutorialTooltipOverlay MakeOverlay(out GameObject go)
        {
            go = new GameObject("TutorialTooltipOverlayTest");
            go.SetActive(false); // prevent OnEnable before fields are set
            return go.AddComponent<TutorialTooltipOverlay>();
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
            var overlay  = go.GetComponent<TutorialTooltipOverlay>();
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            SetField(overlay, "_progress", progress);
            // both event channels remain null
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null event channels must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(progress);
        }

        [Test]
        public void OnDisable_NullChannels_DoesNotThrow()
        {
            MakeOverlay(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null event channels must not throw.");
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
            var overlay = go.GetComponent<TutorialTooltipOverlay>();
            SetField(overlay, "_onStepCompleted", channel);

            go.SetActive(true);   // Awake + OnEnable → subscribed
            go.SetActive(false);  // OnDisable → must unsubscribe

            channel.Raise();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter must fire after OnDisable; overlay must be unsubscribed.");
        }

        [Test]
        public void OnDisable_UnregistersFromOnTutorialCompleted()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialTooltipOverlay>();
            SetField(overlay, "_onTutorialCompleted", channel);

            go.SetActive(true);
            go.SetActive(false);

            channel.Raise();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter must fire after OnDisable; overlay must be unsubscribed.");
        }

        // ── Refresh — guards hide panel ───────────────────────────────────────

        [Test]
        public void Refresh_NullTooltipConfig_HidesPanel()
        {
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialTooltipOverlay>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(true);

            SetField(overlay, "_tooltipPanel", panelGo);
            // _tooltipConfig remains null

            go.SetActive(true); // OnEnable → Refresh → null _tooltipConfig → Hide

            Assert.IsFalse(panelGo.activeSelf,
                "Tooltip panel must be hidden when _tooltipConfig is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        [Test]
        public void Refresh_NullHighlightConfig_HidesPanel()
        {
            MakeOverlay(out GameObject go);
            var overlay       = go.GetComponent<TutorialTooltipOverlay>();
            var panelGo       = new GameObject("TooltipPanel");
            panelGo.SetActive(true);
            var tooltipConfig = ScriptableObject.CreateInstance<TutorialTooltipConfig>();

            SetField(overlay, "_tooltipPanel",  panelGo);
            SetField(overlay, "_tooltipConfig", tooltipConfig);
            // _highlightConfig remains null

            go.SetActive(true);

            Assert.IsFalse(panelGo.activeSelf,
                "Tooltip panel must be hidden when _highlightConfig is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(tooltipConfig);
        }

        [Test]
        public void Refresh_NullProgress_HidesPanel()
        {
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialTooltipOverlay>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(true);

            SetField(overlay, "_tooltipPanel", panelGo);
            // _progress remains null

            go.SetActive(true);

            Assert.IsFalse(panelGo.activeSelf,
                "Tooltip panel must be hidden when _progress is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        [Test]
        public void Refresh_NullSequence_HidesPanel()
        {
            MakeOverlay(out GameObject go);
            var overlay         = go.GetComponent<TutorialTooltipOverlay>();
            var panelGo         = new GameObject("TooltipPanel");
            panelGo.SetActive(true);
            var progress        = ScriptableObject.CreateInstance<TutorialProgressSO>();
            var tooltipConfig   = ScriptableObject.CreateInstance<TutorialTooltipConfig>();
            var highlightConfig = ScriptableObject.CreateInstance<TutorialHighlightConfig>();

            SetField(overlay, "_tooltipPanel",    panelGo);
            SetField(overlay, "_progress",        progress);
            SetField(overlay, "_tooltipConfig",   tooltipConfig);
            SetField(overlay, "_highlightConfig", highlightConfig);
            // _sequence remains null

            go.SetActive(true);

            Assert.IsFalse(panelGo.activeSelf,
                "Tooltip panel must be hidden when _sequence is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(progress);
            Object.DestroyImmediate(tooltipConfig);
            Object.DestroyImmediate(highlightConfig);
        }

        [Test]
        public void Refresh_CompleteProgress_HidesPanel()
        {
            MakeOverlay(out GameObject go);
            var overlay  = go.GetComponent<TutorialTooltipOverlay>();
            var panelGo  = new GameObject("TooltipPanel");
            panelGo.SetActive(true);
            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            progress.Complete(); // tutorial already finished

            SetField(overlay, "_tooltipPanel", panelGo);
            SetField(overlay, "_progress",     progress);

            go.SetActive(true);

            Assert.IsFalse(panelGo.activeSelf,
                "Tooltip panel must be hidden when progress.IsComplete is true.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(progress);
        }

        [Test]
        public void Refresh_NoTagForCurrentStep_HidesPanel()
        {
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialTooltipOverlay>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(true);

            var step = ScriptableObject.CreateInstance<TutorialStepSO>();
            SetField(step, "_stepId", "step_01");
            var sequence = ScriptableObject.CreateInstance<TutorialSequenceSO>();
            SetField(sequence, "_steps", new List<TutorialStepSO> { step });

            var progress        = ScriptableObject.CreateInstance<TutorialProgressSO>();
            var tooltipConfig   = ScriptableObject.CreateInstance<TutorialTooltipConfig>();
            // highlightConfig has no entry for step_01 → tag is empty → Hide
            var highlightConfig = ScriptableObject.CreateInstance<TutorialHighlightConfig>();

            SetField(overlay, "_tooltipPanel",    panelGo);
            SetField(overlay, "_sequence",        sequence);
            SetField(overlay, "_progress",        progress);
            SetField(overlay, "_tooltipConfig",   tooltipConfig);
            SetField(overlay, "_highlightConfig", highlightConfig);

            go.SetActive(true); // RefreshHighlight → empty tag → Hide

            Assert.IsFalse(panelGo.activeSelf,
                "Tooltip panel must be hidden when no tag is configured for the current step.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(step);
            Object.DestroyImmediate(sequence);
            Object.DestroyImmediate(progress);
            Object.DestroyImmediate(tooltipConfig);
            Object.DestroyImmediate(highlightConfig);
        }

        [Test]
        public void Refresh_NoTooltipForTag_HidesPanel()
        {
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialTooltipOverlay>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(true);

            var step = ScriptableObject.CreateInstance<TutorialStepSO>();
            SetField(step, "_stepId", "step_01");
            var sequence = ScriptableObject.CreateInstance<TutorialSequenceSO>();
            SetField(sequence, "_steps", new List<TutorialStepSO> { step });

            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            // step_01 is not complete → it is the current step

            // highlightConfig maps step_01 → "ShopPanel"
            var highlightEntry  = new TutorialHighlightEntry { stepId = "step_01", targetTag = "ShopPanel" };
            var highlightConfig = ScriptableObject.CreateInstance<TutorialHighlightConfig>();
            SetField(highlightConfig, "_entries",
                new List<TutorialHighlightEntry> { highlightEntry });

            // tooltipConfig has NO entry for "ShopPanel" → text is empty → Hide
            var tooltipConfig = ScriptableObject.CreateInstance<TutorialTooltipConfig>();

            SetField(overlay, "_tooltipPanel",    panelGo);
            SetField(overlay, "_sequence",        sequence);
            SetField(overlay, "_progress",        progress);
            SetField(overlay, "_tooltipConfig",   tooltipConfig);
            SetField(overlay, "_highlightConfig", highlightConfig);

            go.SetActive(true);

            Assert.IsFalse(panelGo.activeSelf,
                "Tooltip panel must be hidden when the tooltip config has no text for the resolved tag.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(step);
            Object.DestroyImmediate(sequence);
            Object.DestroyImmediate(progress);
            Object.DestroyImmediate(tooltipConfig);
            Object.DestroyImmediate(highlightConfig);
        }

        [Test]
        public void Refresh_ValidFullConfig_ShowsPanelAndSetsLabelText()
        {
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialTooltipOverlay>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(false);
            var textGo  = new GameObject("TextGo");
            var label   = textGo.AddComponent<Text>();

            var step = ScriptableObject.CreateInstance<TutorialStepSO>();
            SetField(step, "_stepId", "step_01");
            var sequence = ScriptableObject.CreateInstance<TutorialSequenceSO>();
            SetField(sequence, "_steps", new List<TutorialStepSO> { step });

            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();
            // step_01 is not complete → it is the current step

            var highlightEntry  = new TutorialHighlightEntry
                { stepId = "step_01", targetTag = "ShopPanel" };
            var highlightConfig = ScriptableObject.CreateInstance<TutorialHighlightConfig>();
            SetField(highlightConfig, "_entries",
                new List<TutorialHighlightEntry> { highlightEntry });

            var tooltipEntry  = new TutorialTooltipEntry
                { panelTag = "ShopPanel", tooltipText = "Welcome to the Shop!" };
            var tooltipConfig = ScriptableObject.CreateInstance<TutorialTooltipConfig>();
            SetField(tooltipConfig, "_entries",
                new List<TutorialTooltipEntry> { tooltipEntry });

            SetField(overlay, "_tooltipPanel",    panelGo);
            SetField(overlay, "_tooltipLabel",    label);
            SetField(overlay, "_sequence",        sequence);
            SetField(overlay, "_progress",        progress);
            SetField(overlay, "_tooltipConfig",   tooltipConfig);
            SetField(overlay, "_highlightConfig", highlightConfig);
            // _highlightFrame left null — no position update, panel still shows

            go.SetActive(true); // OnEnable → Refresh → all guards pass → show

            Assert.IsTrue(panelGo.activeSelf,
                "Tooltip panel must be shown when all config data is valid.");
            Assert.AreEqual("Welcome to the Shop!", label.text,
                "Tooltip label must be populated with the resolved tooltip text.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(textGo);
            Object.DestroyImmediate(step);
            Object.DestroyImmediate(sequence);
            Object.DestroyImmediate(progress);
            Object.DestroyImmediate(tooltipConfig);
            Object.DestroyImmediate(highlightConfig);
        }

        // ── Hide ──────────────────────────────────────────────────────────────

        [Test]
        public void Hide_SetsPanelInactive()
        {
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialTooltipOverlay>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(true);

            SetField(overlay, "_tooltipPanel", panelGo);
            go.SetActive(true);

            overlay.Hide();

            Assert.IsFalse(panelGo.activeSelf,
                "Hide() must set the tooltip panel inactive.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
        }

        // ── Event-driven refresh / hide ───────────────────────────────────────

        [Test]
        public void OnTutorialCompleted_Raise_HidesPanel()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialTooltipOverlay>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(true);

            SetField(overlay, "_onTutorialCompleted", channel);
            SetField(overlay, "_tooltipPanel",        panelGo);

            go.SetActive(true); // subscribe

            // Manually make panel visible, then fire the completed event.
            panelGo.SetActive(true);
            channel.Raise(); // → Hide()

            Assert.IsFalse(panelGo.activeSelf,
                "Tooltip panel must be hidden when _onTutorialCompleted is raised.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnStepCompleted_Raise_CallsRefresh_NullConfigHides()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialTooltipOverlay>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(true);

            SetField(overlay, "_onStepCompleted", channel);
            SetField(overlay, "_tooltipPanel",    panelGo);
            // _tooltipConfig null → Refresh will call Hide()

            go.SetActive(true);

            // Manually set panel active, then fire the step-completed event.
            panelGo.SetActive(true);
            channel.Raise(); // → Refresh → null _tooltipConfig → Hide

            Assert.IsFalse(panelGo.activeSelf,
                "Tooltip panel must be hidden when _onStepCompleted fires and _tooltipConfig is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Refresh_NullHighlightFrame_ShowsPanelWithoutThrowing()
        {
            MakeOverlay(out GameObject go);
            var overlay = go.GetComponent<TutorialTooltipOverlay>();
            var panelGo = new GameObject("TooltipPanel");
            panelGo.SetActive(false);

            var step = ScriptableObject.CreateInstance<TutorialStepSO>();
            SetField(step, "_stepId", "step_01");
            var sequence = ScriptableObject.CreateInstance<TutorialSequenceSO>();
            SetField(sequence, "_steps", new List<TutorialStepSO> { step });

            var progress = ScriptableObject.CreateInstance<TutorialProgressSO>();

            var highlightEntry  = new TutorialHighlightEntry
                { stepId = "step_01", targetTag = "ArenaPanel" };
            var highlightConfig = ScriptableObject.CreateInstance<TutorialHighlightConfig>();
            SetField(highlightConfig, "_entries",
                new List<TutorialHighlightEntry> { highlightEntry });

            var tooltipEntry  = new TutorialTooltipEntry
                { panelTag = "ArenaPanel", tooltipText = "Enter the arena!" };
            var tooltipConfig = ScriptableObject.CreateInstance<TutorialTooltipConfig>();
            SetField(tooltipConfig, "_entries",
                new List<TutorialTooltipEntry> { tooltipEntry });

            SetField(overlay, "_tooltipPanel",    panelGo);
            SetField(overlay, "_sequence",        sequence);
            SetField(overlay, "_progress",        progress);
            SetField(overlay, "_tooltipConfig",   tooltipConfig);
            SetField(overlay, "_highlightConfig", highlightConfig);
            // _highlightFrame intentionally left null

            Assert.DoesNotThrow(() => go.SetActive(true),
                "Refresh with null _highlightFrame must not throw.");
            Assert.IsTrue(panelGo.activeSelf,
                "Tooltip panel must still be shown even when _highlightFrame is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGo);
            Object.DestroyImmediate(step);
            Object.DestroyImmediate(sequence);
            Object.DestroyImmediate(progress);
            Object.DestroyImmediate(tooltipConfig);
            Object.DestroyImmediate(highlightConfig);
        }
    }
}
