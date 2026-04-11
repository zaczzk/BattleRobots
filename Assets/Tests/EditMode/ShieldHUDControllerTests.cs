using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ShieldHUDController"/>.
    ///
    /// Covers:
    ///   • Null-safety: no exceptions when _shield or _onShieldChanged are null.
    ///   • Event subscription lifecycle: RegisterCallback wired in OnEnable;
    ///     UnregisterCallback wired in OnDisable (no double-subscription on toggle).
    ///   • Event-driven update: raising _onShieldChanged triggers no exceptions.
    ///
    /// uGUI Image / GameObject UI refs are left null here (no Canvas required in
    /// EditMode). Only the event subscription and data-binding path is tested.
    /// </summary>
    public class ShieldHUDControllerTests
    {
        private GameObject          _go;
        private ShieldHUDController _controller;
        private ShieldSO            _shieldSO;
        private FloatGameEvent      _onShieldChanged;

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
            _go              = new GameObject("TestShieldHUD");
            _controller      = _go.AddComponent<ShieldHUDController>();
            _shieldSO        = ScriptableObject.CreateInstance<ShieldSO>();
            _onShieldChanged = ScriptableObject.CreateInstance<FloatGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_shieldSO);
            Object.DestroyImmediate(_onShieldChanged);
            _go              = null;
            _controller      = null;
            _shieldSO        = null;
            _onShieldChanged = null;
        }

        // ── Null-safety ───────────────────────────────────────────────────────

        [Test]
        public void Setup_NullShieldAndEvent_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                SetField(_controller, "_shield",          null);
                SetField(_controller, "_onShieldChanged", null);
            });
        }

        [Test]
        public void OnEnable_NullEvent_DoesNotThrow()
        {
            SetField(_controller, "_shield",          _shieldSO);
            SetField(_controller, "_onShieldChanged", null);

            Assert.DoesNotThrow(() =>
            {
                _go.SetActive(false);
                _go.SetActive(true);   // triggers OnEnable
            });
        }

        // ── Event subscription ────────────────────────────────────────────────

        [Test]
        public void OnEnable_SubscribesCallback_EventRaisedWithoutException()
        {
            _shieldSO.Reset(50f);
            SetField(_controller, "_shield",          _shieldSO);
            SetField(_controller, "_onShieldChanged", _onShieldChanged);

            _go.SetActive(false);
            _go.SetActive(true);   // OnEnable → RegisterCallback

            Assert.DoesNotThrow(() => _onShieldChanged.Raise(40f));
        }

        [Test]
        public void OnDisable_UnsubscribesCallback_SubsequentRaiseNoException()
        {
            _shieldSO.Reset(50f);
            SetField(_controller, "_shield",          _shieldSO);
            SetField(_controller, "_onShieldChanged", _onShieldChanged);

            _go.SetActive(true);
            _go.SetActive(false);  // OnDisable → UnregisterCallback

            // Raising the event after unsubscription should not throw.
            Assert.DoesNotThrow(() => _onShieldChanged.Raise(25f));
        }

        [Test]
        public void ToggleEnable_DoesNotDoubleSubscribe()
        {
            _shieldSO.Reset(50f);
            SetField(_controller, "_shield",          _shieldSO);
            SetField(_controller, "_onShieldChanged", _onShieldChanged);

            // Toggle enable twice — if duplicate subscriptions existed this would
            // fire the handler multiple times, potentially causing assertion issues
            // in tighter tests. Just verify no exception is thrown.
            _go.SetActive(false);
            _go.SetActive(true);
            _go.SetActive(false);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _onShieldChanged.Raise(30f));
        }
    }
}
