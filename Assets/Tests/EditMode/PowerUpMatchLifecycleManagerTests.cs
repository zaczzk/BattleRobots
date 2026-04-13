using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PowerUpMatchLifecycleManager"/>.
    ///
    /// Tests drive the manager through its public
    /// <see cref="PowerUpMatchLifecycleManager.HandleMatchStarted"/> and
    /// <see cref="PowerUpMatchLifecycleManager.HandleMatchEnded"/> methods, and
    /// through VoidGameEvent channel integration. No coroutines or physics
    /// simulation are required.
    ///
    /// Covers:
    ///   • HandleMatchStarted enables all cached child PowerUpControllers.
    ///   • HandleMatchEnded disables all cached child PowerUpControllers.
    ///   • Both handlers are idempotent (enabled → enabled, disabled → disabled).
    ///   • Multiple children are all toggled together.
    ///   • No children present — both handlers are safe no-ops.
    ///   • Event channel integration: VoidGameEvent.Raise() triggers the correct handler.
    ///   • OnEnable / OnDisable with null event channels do not throw.
    ///   • Full enable → disable lifecycle cycle is correct.
    /// </summary>
    public class PowerUpMatchLifecycleManagerTests
    {
        // ── Shared fixtures ───────────────────────────────────────────────────

        private GameObject                   _parentGo;
        private PowerUpMatchLifecycleManager _manager;

        private GameObject       _childGo1;
        private GameObject       _childGo2;
        private PowerUpController _ctrl1;
        private PowerUpController _ctrl2;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // Create the parent and child GameObjects.
            // Children must be added BEFORE the manager so that Awake's
            // GetComponentsInChildren<PowerUpController> finds them.
            _parentGo = new GameObject("PowerUpRoot");

            _childGo1 = new GameObject("Pickup1");
            _childGo2 = new GameObject("Pickup2");
            _childGo1.transform.SetParent(_parentGo.transform);
            _childGo2.transform.SetParent(_parentGo.transform);

            _ctrl1 = _childGo1.AddComponent<PowerUpController>();
            _ctrl2 = _childGo2.AddComponent<PowerUpController>();

            // Add the manager AFTER children — Awake caches the controllers.
            _manager = _parentGo.AddComponent<PowerUpMatchLifecycleManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_parentGo);

            _parentGo = null;
            _manager  = null;
            _childGo1 = null;
            _childGo2 = null;
            _ctrl1    = null;
            _ctrl2    = null;
        }

        // ── HandleMatchStarted ────────────────────────────────────────────────

        [Test]
        public void HandleMatchStarted_EnablesDisabledControllers()
        {
            _ctrl1.enabled = false;
            _manager.HandleMatchStarted();
            Assert.IsTrue(_ctrl1.enabled,
                "HandleMatchStarted should enable a previously disabled PowerUpController.");
        }

        [Test]
        public void HandleMatchStarted_MultipleChildren_AllEnabled()
        {
            _ctrl1.enabled = false;
            _ctrl2.enabled = false;

            _manager.HandleMatchStarted();

            Assert.IsTrue(_ctrl1.enabled, "ctrl1 should be enabled after HandleMatchStarted.");
            Assert.IsTrue(_ctrl2.enabled, "ctrl2 should be enabled after HandleMatchStarted.");
        }

        [Test]
        public void HandleMatchStarted_AlreadyEnabled_RemainsEnabled()
        {
            _ctrl1.enabled = true;
            _manager.HandleMatchStarted();
            Assert.IsTrue(_ctrl1.enabled, "Already-enabled controller should remain enabled.");
        }

        [Test]
        public void HandleMatchStarted_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.HandleMatchStarted());
        }

        // ── HandleMatchEnded ──────────────────────────────────────────────────

        [Test]
        public void HandleMatchEnded_DisablesEnabledControllers()
        {
            _ctrl1.enabled = true;
            _manager.HandleMatchEnded();
            Assert.IsFalse(_ctrl1.enabled,
                "HandleMatchEnded should disable a previously enabled PowerUpController.");
        }

        [Test]
        public void HandleMatchEnded_MultipleChildren_AllDisabled()
        {
            _ctrl1.enabled = true;
            _ctrl2.enabled = true;

            _manager.HandleMatchEnded();

            Assert.IsFalse(_ctrl1.enabled, "ctrl1 should be disabled after HandleMatchEnded.");
            Assert.IsFalse(_ctrl2.enabled, "ctrl2 should be disabled after HandleMatchEnded.");
        }

        [Test]
        public void HandleMatchEnded_AlreadyDisabled_RemainsDisabled()
        {
            _ctrl1.enabled = false;
            _manager.HandleMatchEnded();
            Assert.IsFalse(_ctrl1.enabled, "Already-disabled controller should remain disabled.");
        }

        [Test]
        public void HandleMatchEnded_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.HandleMatchEnded());
        }

        // ── Lifecycle cycle ───────────────────────────────────────────────────

        [Test]
        public void HandleMatchStarted_ThenHandleMatchEnded_CycleIsCorrect()
        {
            _ctrl1.enabled = false;

            _manager.HandleMatchStarted();
            Assert.IsTrue(_ctrl1.enabled, "Should be enabled after match start.");

            _manager.HandleMatchEnded();
            Assert.IsFalse(_ctrl1.enabled, "Should be disabled after match end.");
        }

        [Test]
        public void HandleMatchEnded_ThenHandleMatchStarted_CycleIsCorrect()
        {
            _ctrl1.enabled = true;

            _manager.HandleMatchEnded();
            Assert.IsFalse(_ctrl1.enabled, "Should be disabled after match end.");

            _manager.HandleMatchStarted();
            Assert.IsTrue(_ctrl1.enabled, "Should be re-enabled after next match start.");
        }

        // ── No children ───────────────────────────────────────────────────────

        [Test]
        public void HandleMatchStarted_NoChildControllers_DoesNotThrow()
        {
            var emptyGo = new GameObject("EmptyParent");
            var mgr     = emptyGo.AddComponent<PowerUpMatchLifecycleManager>();

            Assert.DoesNotThrow(() => mgr.HandleMatchStarted());

            Object.DestroyImmediate(emptyGo);
        }

        [Test]
        public void HandleMatchEnded_NoChildControllers_DoesNotThrow()
        {
            var emptyGo = new GameObject("EmptyParent");
            var mgr     = emptyGo.AddComponent<PowerUpMatchLifecycleManager>();

            Assert.DoesNotThrow(() => mgr.HandleMatchEnded());

            Object.DestroyImmediate(emptyGo);
        }

        // ── Event channel integration ─────────────────────────────────────────

        [Test]
        public void MatchStartedEvent_Raised_EnablesChildControllers()
        {
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_manager, "_onMatchStarted", evt);

            // Re-register with the new event channel by toggling enabled state.
            _manager.enabled = false;
            _manager.enabled = true;

            _ctrl1.enabled = false;
            _ctrl2.enabled = false;

            evt.Raise();

            Assert.IsTrue(_ctrl1.enabled, "ctrl1 should be enabled when MatchStarted fires.");
            Assert.IsTrue(_ctrl2.enabled, "ctrl2 should be enabled when MatchStarted fires.");

            Object.DestroyImmediate(evt);
        }

        [Test]
        public void MatchEndedEvent_Raised_DisablesChildControllers()
        {
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_manager, "_onMatchEnded", evt);

            _manager.enabled = false;
            _manager.enabled = true;

            _ctrl1.enabled = true;
            _ctrl2.enabled = true;

            evt.Raise();

            Assert.IsFalse(_ctrl1.enabled, "ctrl1 should be disabled when MatchEnded fires.");
            Assert.IsFalse(_ctrl2.enabled, "ctrl2 should be disabled when MatchEnded fires.");

            Object.DestroyImmediate(evt);
        }

        // ── Null event channels ───────────────────────────────────────────────

        [Test]
        public void OnEnable_NullEventChannels_DoesNotThrow()
        {
            // Both channels default to null — enable/disable must not throw.
            var go  = new GameObject("NullEventsParent");
            var mgr = go.AddComponent<PowerUpMatchLifecycleManager>();

            Assert.DoesNotThrow(() =>
            {
                mgr.enabled = false;
                mgr.enabled = true;
            });

            Object.DestroyImmediate(go);
        }
    }
}
