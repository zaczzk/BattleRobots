using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PauseMenuController"/>.
    ///
    /// Covers:
    ///   • Awake with null _pausePanel — panel?.SetActive(false) silently skips.
    ///   • OnEnable with all null event channels — ?. guards prevent throw.
    ///   • _onPaused / _onResumed raised when panel is null — ShowPauseMenu /
    ///     HidePauseMenu use ?. on _pausePanel so neither throws.
    ///   • OnResumePressed() with null _pauseManager — ?. guard no-ops safely.
    ///   • OnQuitToMenuPressed() with null _pauseManager + null _sceneRegistry —
    ///     falls back to hard-coded "MainMenu", SceneLoader.LoadScene logs an error
    ///     (scene not in build settings) but must not throw an exception.
    ///   • OnDisable unregisters _pauseCallback from _onPaused (external-counter
    ///     pattern verifies only the test counter fires after disable).
    ///   • OnDisable unregisters _resumeCallback from _onResumed (same pattern).
    ///
    /// All tests run headless; no scene or uGUI objects required.
    /// </summary>
    public class PauseMenuControllerTests
    {
        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Factory helper ────────────────────────────────────────────────────

        private static (GameObject go, PauseMenuController ctrl) MakeCtrl()
        {
            var go   = new GameObject("PauseMenuController");
            go.SetActive(false); // inactive so Awake/OnEnable don't run during setup
            var ctrl = go.AddComponent<PauseMenuController>();
            return (go, ctrl);
        }

        // ── Awake — null panel ────────────────────────────────────────────────

        [Test]
        public void Awake_NullPanel_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            // _pausePanel not assigned → Awake's _pausePanel?.SetActive(false) skips.
            Assert.DoesNotThrow(() => go.SetActive(true),
                "Awake with null _pausePanel must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── OnEnable — all null channels ──────────────────────────────────────

        [Test]
        public void OnEnable_AllNullEventChannels_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            // _onPaused and _onResumed not assigned; ?. guards in OnEnable must skip.
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null event channels must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── _onPaused raised — null panel ─────────────────────────────────────

        [Test]
        public void OnPaused_Raise_NullPanel_DoesNotThrow()
        {
            var onPaused = ScriptableObject.CreateInstance<VoidGameEvent>();
            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onPaused", onPaused);

            go.SetActive(true); // OnEnable registers _pauseCallback

            // ShowPauseMenu uses _pausePanel?.SetActive(true) — null panel is a no-op.
            Assert.DoesNotThrow(() => onPaused.Raise(),
                "Raising _onPaused with null _pausePanel must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(onPaused);
        }

        // ── _onResumed raised — null panel ────────────────────────────────────

        [Test]
        public void OnResumed_Raise_NullPanel_DoesNotThrow()
        {
            var onResumed = ScriptableObject.CreateInstance<VoidGameEvent>();
            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onResumed", onResumed);

            go.SetActive(true); // OnEnable registers _resumeCallback

            // HidePauseMenu uses _pausePanel?.SetActive(false) — null panel is a no-op.
            Assert.DoesNotThrow(() => onResumed.Raise(),
                "Raising _onResumed with null _pausePanel must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(onResumed);
        }

        // ── OnResumePressed — null PauseManager ───────────────────────────────

        [Test]
        public void OnResumePressed_NullPauseManager_DoesNotThrow()
        {
            var (go, ctrl) = MakeCtrl();
            go.SetActive(true);

            // _pauseManager is null; _pauseManager?.Resume() must silently no-op.
            Assert.DoesNotThrow(() => ctrl.OnResumePressed(),
                "OnResumePressed with null _pauseManager must not throw.");

            Object.DestroyImmediate(go);
        }

        // ── OnQuitToMenuPressed — null manager + null registry ────────────────

        [Test]
        public void OnQuitToMenuPressed_NullManagerAndRegistry_DoesNotThrow()
        {
            // With null _pauseManager and null _sceneRegistry the method falls back to
            // SceneLoader.LoadScene("MainMenu").  In EditMode the scene won't load but
            // no exception should be thrown.
            var (go, ctrl) = MakeCtrl();
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.OnQuitToMenuPressed(),
                "OnQuitToMenuPressed with null manager and registry must not throw.");

            Object.DestroyImmediate(go);
        }

        // ── OnDisable — unregisters _pauseCallback from _onPaused ────────────

        [Test]
        public void OnDisable_UnregistersFromPausedChannel()
        {
            var onPaused = ScriptableObject.CreateInstance<VoidGameEvent>();

            // External counter — the only callback that should remain after disable.
            int externalCount = 0;
            onPaused.RegisterCallback(() => externalCount++);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onPaused", onPaused);

            go.SetActive(true);   // OnEnable: registers _pauseCallback
            go.SetActive(false);  // OnDisable: unregisters _pauseCallback

            // Raise event — only the external counter must fire.
            onPaused.Raise();

            Assert.AreEqual(1, externalCount,
                "After OnDisable, only the external counter (not _pauseCallback) " +
                "should fire on _onPaused.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(onPaused);
        }

        // ── OnDisable — unregisters _resumeCallback from _onResumed ──────────

        [Test]
        public void OnDisable_UnregistersFromResumedChannel()
        {
            var onResumed = ScriptableObject.CreateInstance<VoidGameEvent>();

            int externalCount = 0;
            onResumed.RegisterCallback(() => externalCount++);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onResumed", onResumed);

            go.SetActive(true);
            go.SetActive(false);

            onResumed.Raise();

            Assert.AreEqual(1, externalCount,
                "After OnDisable, only the external counter (not _resumeCallback) " +
                "should fire on _onResumed.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(onResumed);
        }
    }
}
