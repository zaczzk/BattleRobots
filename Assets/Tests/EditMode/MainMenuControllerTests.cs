using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MainMenuController"/>.
    ///
    /// Covers:
    ///   • PlayGame() with null _sceneRegistry — falls back to hard-coded "Arena";
    ///     SceneLoader.LoadScene may log an error but must not throw.
    ///   • OpenShop() with null _sceneRegistry — falls back to "Shop".
    ///   • QuitGame() with null _onMenuAction — ?. guard must not throw.
    ///   • PlayGame() fires _onMenuAction VoidGameEvent exactly once.
    ///   • OpenShop() fires _onMenuAction VoidGameEvent exactly once.
    ///   • QuitGame() fires _onMenuAction VoidGameEvent exactly once.
    ///   • PlayGame() + OpenShop() + QuitGame() with null event channel — ?. guard.
    ///
    /// MainMenuController has no OnEnable/OnDisable lifecycle (button-only), so
    /// there are no subscription/unsubscription tests.
    /// All tests run headless; no scene or uGUI objects required.
    /// </summary>
    public class MainMenuControllerTests
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

        private static (GameObject go, MainMenuController ctrl) MakeCtrl()
        {
            var go   = new GameObject("MainMenuController");
            var ctrl = go.AddComponent<MainMenuController>();
            return (go, ctrl);
        }

        // ── PlayGame — null SceneRegistry ─────────────────────────────────────

        [Test]
        public void PlayGame_NullSceneRegistry_DoesNotThrow()
        {
            // Falls back to SceneLoader.LoadScene("Arena"). Scene won't load in
            // EditMode but no exception should be thrown.
            var (go, ctrl) = MakeCtrl();

            Assert.DoesNotThrow(() => ctrl.PlayGame(),
                "PlayGame with null _sceneRegistry must not throw.");

            Object.DestroyImmediate(go);
        }

        // ── OpenShop — null SceneRegistry ─────────────────────────────────────

        [Test]
        public void OpenShop_NullSceneRegistry_DoesNotThrow()
        {
            var (go, ctrl) = MakeCtrl();

            Assert.DoesNotThrow(() => ctrl.OpenShop(),
                "OpenShop with null _sceneRegistry must not throw.");

            Object.DestroyImmediate(go);
        }

        // ── QuitGame — null event channel ─────────────────────────────────────

        [Test]
        public void QuitGame_NullMenuActionEvent_DoesNotThrow()
        {
            // _onMenuAction is null; _onMenuAction?.Raise() must silently skip.
            var (go, ctrl) = MakeCtrl();

            Assert.DoesNotThrow(() => ctrl.QuitGame(),
                "QuitGame with null _onMenuAction must not throw.");

            Object.DestroyImmediate(go);
        }

        // ── PlayGame — fires _onMenuAction exactly once ───────────────────────

        [Test]
        public void PlayGame_RaisesMenuActionEvent()
        {
            var menuAction = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count = 0;
            menuAction.RegisterCallback(() => count++);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onMenuAction", menuAction);

            ctrl.PlayGame();

            Assert.AreEqual(1, count,
                "PlayGame must raise _onMenuAction exactly once.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(menuAction);
        }

        // ── OpenShop — fires _onMenuAction exactly once ───────────────────────

        [Test]
        public void OpenShop_RaisesMenuActionEvent()
        {
            var menuAction = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count = 0;
            menuAction.RegisterCallback(() => count++);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onMenuAction", menuAction);

            ctrl.OpenShop();

            Assert.AreEqual(1, count,
                "OpenShop must raise _onMenuAction exactly once.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(menuAction);
        }

        // ── QuitGame — fires _onMenuAction exactly once ───────────────────────

        [Test]
        public void QuitGame_RaisesMenuActionEvent()
        {
            var menuAction = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count = 0;
            menuAction.RegisterCallback(() => count++);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onMenuAction", menuAction);

            ctrl.QuitGame();

            Assert.AreEqual(1, count,
                "QuitGame must raise _onMenuAction exactly once.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(menuAction);
        }
    }
}
