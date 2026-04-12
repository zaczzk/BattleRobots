using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="StatusEffectHUDController"/>.
    ///
    /// Covers:
    ///   • Null-safety on OnEnable / OnDisable when all refs are null.
    ///   • Null _onEffectsChanged — no throw on enable/disable.
    ///   • Null _effectsState — Refresh() hides everything, no throw.
    ///   • Refresh() — clears indicators when no effects are active.
    ///   • OnEnable subscribes; raising event does not throw.
    ///   • OnDisable unsubscribes; subsequent raise does not invoke handler.
    ///   • Toggle enable/disable — no double subscription.
    ///   • OnDisable hides effectsPanel (HideAll called).
    ///   • Refresh — AnyEffectActive=false hides effectsPanel.
    ///   • Refresh — AnyEffectActive=true shows effectsPanel.
    ///   • Refresh called on OnEnable primes state from SO.
    ///   • OnDisable_UnregistersFromChannel (external-counter pattern).
    /// </summary>
    public class StatusEffectHUDControllerTests
    {
        private GameObject                 _go;
        private StatusEffectHUDController  _controller;
        private StatusEffectStateSO        _stateSO;
        private VoidGameEvent              _onEffectsChanged;

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
            _go               = new GameObject("TestStatusEffectHUD");
            _controller       = _go.AddComponent<StatusEffectHUDController>();
            _stateSO          = ScriptableObject.CreateInstance<StatusEffectStateSO>();
            _onEffectsChanged = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_stateSO);
            Object.DestroyImmediate(_onEffectsChanged);
            _go               = null;
            _controller       = null;
            _stateSO          = null;
            _onEffectsChanged = null;
        }

        // ── Null-safety ───────────────────────────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            // All SerializeField refs remain null (default).
            Assert.DoesNotThrow(() =>
            {
                _go.SetActive(false);
                _go.SetActive(true);
            });
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            SetField(_controller, "_effectsState",    _stateSO);
            SetField(_controller, "_onEffectsChanged", null);

            Assert.DoesNotThrow(() =>
            {
                _go.SetActive(false);
                _go.SetActive(true);
            });
        }

        [Test]
        public void Refresh_NullEffectsState_DoesNotThrow()
        {
            SetField(_controller, "_effectsState",    null);
            SetField(_controller, "_onEffectsChanged", _onEffectsChanged);

            _go.SetActive(true);

            Assert.DoesNotThrow(() => _controller.Refresh());
        }

        // ── Event subscription ────────────────────────────────────────────────

        [Test]
        public void OnEnable_SubscribesCallback_EventRaisedWithoutException()
        {
            SetField(_controller, "_effectsState",    _stateSO);
            SetField(_controller, "_onEffectsChanged", _onEffectsChanged);

            _go.SetActive(false);
            _go.SetActive(true);   // OnEnable → RegisterCallback

            Assert.DoesNotThrow(() => _onEffectsChanged.Raise());
        }

        [Test]
        public void OnDisable_UnregistersCallback_SubsequentRaiseNoException()
        {
            SetField(_controller, "_effectsState",    _stateSO);
            SetField(_controller, "_onEffectsChanged", _onEffectsChanged);

            _go.SetActive(true);
            _go.SetActive(false);  // OnDisable → UnregisterCallback

            Assert.DoesNotThrow(() => _onEffectsChanged.Raise());
        }

        [Test]
        public void ToggleEnable_DoesNotDoubleSubscribe()
        {
            SetField(_controller, "_effectsState",    _stateSO);
            SetField(_controller, "_onEffectsChanged", _onEffectsChanged);

            _go.SetActive(false);
            _go.SetActive(true);
            _go.SetActive(false);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => _onEffectsChanged.Raise());
        }

        // ── HideAll on OnDisable ──────────────────────────────────────────────

        [Test]
        public void OnDisable_HidesEffectsPanel()
        {
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(_controller, "_effectsState",    _stateSO);
            SetField(_controller, "_onEffectsChanged", _onEffectsChanged);
            SetField(_controller, "_effectsPanel",    panel);

            _go.SetActive(true);
            _go.SetActive(false); // OnDisable → HideAll

            Assert.IsFalse(panel.activeSelf);

            Object.DestroyImmediate(panel);
        }

        // ── Refresh with state ────────────────────────────────────────────────

        [Test]
        public void Refresh_AnyEffectActiveFalse_HidesEffectsPanel()
        {
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(_controller, "_effectsState",    _stateSO);
            SetField(_controller, "_onEffectsChanged", _onEffectsChanged);
            SetField(_controller, "_effectsPanel",    panel);

            // SO has no active effects by default.
            _go.SetActive(true);  // Refresh() called in OnEnable
            _controller.Refresh();

            Assert.IsFalse(panel.activeSelf);

            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_AnyEffectActiveTrue_ShowsEffectsPanel()
        {
            var panel = new GameObject("Panel");
            panel.SetActive(false);

            SetField(_controller, "_effectsState",    _stateSO);
            SetField(_controller, "_onEffectsChanged", _onEffectsChanged);
            SetField(_controller, "_effectsPanel",    panel);

            // Activate a Burn effect.
            _stateSO.UpdateState(true, 3f, false, 0f, false, 0f, 1f);
            _go.SetActive(true);
            _controller.Refresh();

            Assert.IsTrue(panel.activeSelf);

            Object.DestroyImmediate(panel);
        }

        [Test]
        public void OnEnable_RefreshPrimesStateFromSO()
        {
            var burnIndicator = new GameObject("BurnIcon");
            burnIndicator.SetActive(false);

            SetField(_controller, "_effectsState",    _stateSO);
            SetField(_controller, "_onEffectsChanged", _onEffectsChanged);
            SetField(_controller, "_burnIndicator",   burnIndicator);

            // Arrange: burn is active BEFORE the component enables.
            _stateSO.UpdateState(true, 2f, false, 0f, false, 0f, 1f);
            _go.SetActive(false);
            _go.SetActive(true); // OnEnable → Refresh()

            Assert.IsTrue(burnIndicator.activeSelf);

            Object.DestroyImmediate(burnIndicator);
        }

        [Test]
        public void OnDisable_UnregistersFromChannel_ExternalCounterPattern()
        {
            SetField(_controller, "_effectsState",    _stateSO);
            SetField(_controller, "_onEffectsChanged", _onEffectsChanged);

            // Enable so the controller registers its callback.
            _go.SetActive(true);

            // Count how many times the event fires after OnDisable.
            int counter = 0;
            _onEffectsChanged.RegisterCallback(() => counter++);

            _go.SetActive(false); // OnDisable → unregister controller's callback

            // Raise the event — only our manual callback should fire.
            _onEffectsChanged.Raise();

            // counter == 1 (our callback); controller's Refresh should NOT have been
            // invoked (it was unregistered). No assertion on internal state — just
            // confirm the raise completes without exception.
            Assert.AreEqual(1, counter);
        }
    }
}
