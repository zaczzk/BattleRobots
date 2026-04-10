using System;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="IntGameEvent"/> (and by extension
    /// <see cref="GameEvent{T}"/> with a value-type payload).
    ///
    /// Mirrors the structure of VoidGameEventTests but also verifies that the
    /// correct integer payload is delivered to each callback.
    /// </summary>
    public class IntGameEventTests
    {
        private IntGameEvent _evt;

        [SetUp]
        public void SetUp()
        {
            _evt = ScriptableObject.CreateInstance<IntGameEvent>();
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
            Assert.DoesNotThrow(() => _evt.Raise(42));
        }

        [Test]
        public void Raise_InvokesRegisteredCallback()
        {
            int callCount = 0;
            _evt.RegisterCallback(v => callCount++);
            _evt.Raise(1);
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Raise_DeliversCorrectPayload()
        {
            int received = -1;
            _evt.RegisterCallback(v => received = v);
            _evt.Raise(99);
            Assert.AreEqual(99, received);
        }

        [Test]
        public void Raise_DeliversLatestPayload_OnEachCall()
        {
            int last = -1;
            _evt.RegisterCallback(v => last = v);
            _evt.Raise(10);
            Assert.AreEqual(10, last);
            _evt.Raise(20);
            Assert.AreEqual(20, last);
            _evt.Raise(30);
            Assert.AreEqual(30, last);
        }

        [Test]
        public void Raise_DeliversPayload_ToAllCallbacks()
        {
            int a = -1, b = -1;
            _evt.RegisterCallback(v => a = v);
            _evt.RegisterCallback(v => b = v);
            _evt.Raise(77);
            Assert.AreEqual(77, a);
            Assert.AreEqual(77, b);
        }

        [Test]
        public void Raise_ZeroPayload_IsDeliveredCorrectly()
        {
            int received = -1;
            _evt.RegisterCallback(v => received = v);
            _evt.Raise(0);
            Assert.AreEqual(0, received);
        }

        [Test]
        public void Raise_NegativePayload_IsDeliveredCorrectly()
        {
            int received = 0;
            _evt.RegisterCallback(v => received = v);
            _evt.Raise(-500);
            Assert.AreEqual(-500, received);
        }

        // ── Unregister ────────────────────────────────────────────────────────

        [Test]
        public void Raise_AfterUnregister_DoesNotInvokeCallback()
        {
            int callCount = 0;
            Action<int> cb = v => callCount++;
            _evt.RegisterCallback(cb);
            _evt.UnregisterCallback(cb);
            _evt.Raise(1);
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Unregister_UnknownCallback_DoesNotThrow()
        {
            Action<int> cb = v => { };
            Assert.DoesNotThrow(() => _evt.UnregisterCallback(cb));
        }

        [Test]
        public void Raise_AfterPartialUnregister_OnlyInvokesRemainingCallbacks()
        {
            int a = 0, b = 0;
            Action<int> cbA = v => a++;
            Action<int> cbB = v => b++;
            _evt.RegisterCallback(cbA);
            _evt.RegisterCallback(cbB);
            _evt.UnregisterCallback(cbA);
            _evt.Raise(1);
            Assert.AreEqual(0, a);
            Assert.AreEqual(1, b);
        }

        // ── Duplicate guard ───────────────────────────────────────────────────

        [Test]
        public void RegisterCallback_Duplicate_InvokedOncePerRaise()
        {
            int callCount = 0;
            Action<int> cb = v => callCount++;
            _evt.RegisterCallback(cb);
            _evt.RegisterCallback(cb); // duplicate — ignored
            _evt.Raise(1);
            Assert.AreEqual(1, callCount);
        }

        // ── Safe unregister during iteration ─────────────────────────────────

        [Test]
        public void Raise_CallbackUnregistersItself_DoesNotThrow()
        {
            Action<int> selfRemovingCb = null;
            selfRemovingCb = v => _evt.UnregisterCallback(selfRemovingCb);
            _evt.RegisterCallback(selfRemovingCb);
            Assert.DoesNotThrow(() => _evt.Raise(0));
        }

        [Test]
        public void Raise_SelfRemovingCallback_NotCalledOnSubsequentRaise()
        {
            int callCount = 0;
            Action<int> cb = null;
            cb = v =>
            {
                callCount++;
                _evt.UnregisterCallback(cb);
            };
            _evt.RegisterCallback(cb);
            _evt.Raise(1);
            _evt.Raise(1);
            Assert.AreEqual(1, callCount);
        }
    }
}
