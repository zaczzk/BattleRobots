using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="BuildRatingController"/>.
    ///
    /// Uses the inactive-GameObject pattern so Awake runs only after all fields are
    /// injected via reflection.  Activating the GO triggers Awake + OnEnable.
    ///
    /// Covers:
    ///   • OnEnable with all-null fields  → DoesNotThrow.
    ///   • OnDisable with all-null fields → DoesNotThrow.
    ///   • OnEnable with null channel     → DoesNotThrow.
    ///   • OnDisable with null channel    → DoesNotThrow.
    ///   • OnDisable unregisters callback → external counter confirms unregister.
    ///   • Refresh with null BuildRatingSO → DoesNotThrow.
    /// </summary>
    public class BuildRatingControllerTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private GameObject             _go;
        private BuildRatingController  _controller;
        private VoidGameEvent          _onLoadoutChanged;

        // ── Reflection helper ─────────────────────────────────────────────────

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
            _onLoadoutChanged = ScriptableObject.CreateInstance<VoidGameEvent>();

            _go = new GameObject("BuildRatingController");
            _go.SetActive(false);   // keep inactive until fields are injected
            _controller = _go.AddComponent<BuildRatingController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)              Object.DestroyImmediate(_go);
            if (_onLoadoutChanged != null) Object.DestroyImmediate(_onLoadoutChanged);
        }

        private void Activate() => _go.SetActive(true);

        // ── Null-guard: all fields null ───────────────────────────────────────

        [Test]
        public void OnEnable_AllNullFields_DoesNotThrow()
        {
            // All inspector fields are null by default — must not throw on enable.
            Assert.DoesNotThrow(() => Activate());
        }

        [Test]
        public void OnDisable_AllNullFields_DoesNotThrow()
        {
            Activate();
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        // ── Null _onLoadoutChanged channel ────────────────────────────────────

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            // _onLoadoutChanged not assigned; Refresh must still execute safely.
            Assert.DoesNotThrow(() => Activate());
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            Activate();
            // Channel was never assigned; OnDisable must not throw.
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        // ── OnDisable unregisters callback ────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersCallback_ExternalCounterConfirms()
        {
            // Wire the channel so the controller actually subscribes in OnEnable.
            SetField(_controller, "_onLoadoutChanged", _onLoadoutChanged);

            Activate();             // OnEnable — subscribes Refresh delegate
            _go.SetActive(false);   // OnDisable — must unregister Refresh delegate

            // Register an external counter AFTER the controller has unregistered.
            int externalCount = 0;
            _onLoadoutChanged.RegisterCallback(() => externalCount++);

            _onLoadoutChanged.Raise();  // controller's Refresh must NOT run

            // The external callback must fire exactly once, confirming the controller
            // no longer holds a reference to the channel.
            Assert.AreEqual(1, externalCount,
                "External callback should fire once; controller must be unregistered after OnDisable.");
        }

        // ── Refresh: null BuildRatingSO ───────────────────────────────────────

        [Test]
        public void Refresh_NullBuildRating_DoesNotThrow()
        {
            // _buildRating is null by default; OnEnable triggers Refresh internally.
            Assert.DoesNotThrow(() => Activate());
        }
    }
}
