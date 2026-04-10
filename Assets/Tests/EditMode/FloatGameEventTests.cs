using System;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="FloatGameEvent"/> (GameEvent&lt;float&gt;).
    ///
    /// Verifies that float payloads are delivered correctly to registered callbacks,
    /// that duplicate registration is guarded, and that safe unregistration during
    /// iteration is supported. Mirrors the structure of <see cref="IntGameEventTests"/>
    /// for the float variant, which is used extensively by HealthSO and CombatHUDController.
    /// </summary>
    public class FloatGameEventTests
    {
        private FloatGameEvent _evt;

        [SetUp]
        public void SetUp()
        {
            _evt = ScriptableObject.CreateInstance<FloatGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_evt);
            _evt = null;
        }

        // ── Basic raise / invoke ───────────────────────────────────────────────

        [Test]
        public void Raise_WithNoCallbacks_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _evt.Raise(1.5f));
        }

        [Test]
        public void Raise_InvokesRegisteredCallback()
        {
            int callCount = 0;
            _evt.RegisterCallback(v => callCount++);
            _evt.Raise(1f);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Raise_DeliversCorrectPayload()
        {
            float received = -1f;
            _evt.RegisterCallback(v => received = v);
            _evt.Raise(3.14f);
            Assert.AreEqual(3.14f, received, 0.0001f);
        }

        [Test]
        public void Raise_ZeroPayload_IsDeliveredCorrectly()
        {
            float received = -1f;
            _evt.RegisterCallback(v => received = v);
            _evt.Raise(0f);
            Assert.AreEqual(0f, received, 0.0001f);
        }

        [Test]
        public void Raise_NegativePayload_IsDeliveredCorrectly()
        {
            float received = 0f;
            _evt.RegisterCallback(v => received = v);
            _evt.Raise(-100f);
            Assert.AreEqual(-100f, received, 0.0001f);
        }

        [Test]
        public void Raise_DeliversPayload_ToAllCallbacks()
        {
            float a = -1f, b = -1f;
            _evt.RegisterCallback(v => a = v);
            _evt.RegisterCallback(v => b = v);
            _evt.Raise(7.7f);
            Assert.AreEqual(7.7f, a, 0.0001f);
            Assert.AreEqual(7.7f, b, 0.0001f);
        }

        [Test]
        public void Raise_MultipleInvocations_DeliversLatestPayload()
        {
            float last = -1f;
            _evt.RegisterCallback(v => last = v);
            _evt.Raise(10f);
            Assert.AreEqual(10f, last, 0.0001f);
            _evt.Raise(50f);
            Assert.AreEqual(50f, last, 0.0001f);
            _evt.Raise(0.001f);
            Assert.AreEqual(0.001f, last, 0.00001f);
        }

        // ── Unregister ────────────────────────────────────────────────────────

        [Test]
        public void Raise_AfterUnregister_DoesNotInvokeCallback()
        {
            int callCount = 0;
            Action<float> cb = v => callCount++;
            _evt.RegisterCallback(cb);
            _evt.UnregisterCallback(cb);
            _evt.Raise(1f);
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Unregister_UnknownCallback_DoesNotThrow()
        {
            Action<float> cb = v => { };
            Assert.DoesNotThrow(() => _evt.UnregisterCallback(cb));
        }

        [Test]
        public void Raise_AfterPartialUnregister_OnlyInvokesRemainingCallback()
        {
            int a = 0, b = 0;
            Action<float> cbA = v => a++;
            Action<float> cbB = v => b++;
            _evt.RegisterCallback(cbA);
            _evt.RegisterCallback(cbB);
            _evt.UnregisterCallback(cbA);
            _evt.Raise(1f);
            Assert.AreEqual(0, a);
            Assert.AreEqual(1, b);
        }

        // ── Duplicate guard ───────────────────────────────────────────────────

        [Test]
        public void RegisterCallback_Duplicate_InvokedOncePerRaise()
        {
            int callCount = 0;
            Action<float> cb = v => callCount++;
            _evt.RegisterCallback(cb);
            _evt.RegisterCallback(cb); // duplicate — ignored
            _evt.Raise(1f);
            Assert.AreEqual(1, callCount);
        }

        // ── Safe unregister during iteration ─────────────────────────────────

        [Test]
        public void Raise_SelfRemovingCallback_DoesNotThrow()
        {
            Action<float> selfRemovingCb = null;
            selfRemovingCb = v => _evt.UnregisterCallback(selfRemovingCb);
            _evt.RegisterCallback(selfRemovingCb);
            Assert.DoesNotThrow(() => _evt.Raise(0f));
        }

        [Test]
        public void Raise_SelfRemovingCallback_NotCalledOnSubsequentRaise()
        {
            int callCount = 0;
            Action<float> cb = null;
            cb = v =>
            {
                callCount++;
                _evt.UnregisterCallback(cb);
            };
            _evt.RegisterCallback(cb);
            _evt.Raise(1f);
            _evt.Raise(1f);
            Assert.AreEqual(1, callCount);
        }
    }
}
