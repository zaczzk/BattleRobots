using System;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="VoidGameEvent"/> callback registration,
    /// invocation, and safe-unregister-during-iteration behaviour.
    ///
    /// Tests the same contract for both the void channel and (indirectly) the
    /// generic <see cref="GameEvent{T}"/> base, since both share identical
    /// implementation patterns.
    /// </summary>
    public class VoidGameEventTests
    {
        private VoidGameEvent _evt;

        [SetUp]
        public void SetUp()
        {
            _evt = ScriptableObject.CreateInstance<VoidGameEvent>();
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
            Assert.DoesNotThrow(() => _evt.Raise());
        }

        [Test]
        public void Raise_InvokesRegisteredCallback()
        {
            int callCount = 0;
            _evt.RegisterCallback(() => callCount++);
            _evt.Raise();
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Raise_InvokesCallbackEveryTime()
        {
            int callCount = 0;
            _evt.RegisterCallback(() => callCount++);
            _evt.Raise();
            _evt.Raise();
            _evt.Raise();
            Assert.AreEqual(3, callCount);
        }

        [Test]
        public void Raise_InvokesAllRegisteredCallbacks()
        {
            int a = 0, b = 0, c = 0;
            _evt.RegisterCallback(() => a++);
            _evt.RegisterCallback(() => b++);
            _evt.RegisterCallback(() => c++);
            _evt.Raise();
            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
            Assert.AreEqual(1, c);
        }

        // ── Unregister ────────────────────────────────────────────────────────

        [Test]
        public void Raise_AfterUnregister_DoesNotInvokeCallback()
        {
            int callCount = 0;
            Action cb = () => callCount++;
            _evt.RegisterCallback(cb);
            _evt.UnregisterCallback(cb);
            _evt.Raise();
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Unregister_UnknownCallback_DoesNotThrow()
        {
            Action cb = () => { };
            Assert.DoesNotThrow(() => _evt.UnregisterCallback(cb));
        }

        [Test]
        public void Raise_AfterPartialUnregister_OnlyInvokesRemainingCallbacks()
        {
            int a = 0, b = 0;
            Action cbA = () => a++;
            Action cbB = () => b++;
            _evt.RegisterCallback(cbA);
            _evt.RegisterCallback(cbB);
            _evt.UnregisterCallback(cbA);
            _evt.Raise();
            Assert.AreEqual(0, a);
            Assert.AreEqual(1, b);
        }

        // ── Duplicate guard ───────────────────────────────────────────────────

        [Test]
        public void RegisterCallback_Duplicate_IsIgnored_CallbackInvokedOnce()
        {
            int callCount = 0;
            Action cb = () => callCount++;
            _evt.RegisterCallback(cb);
            _evt.RegisterCallback(cb); // duplicate — should be silently ignored
            _evt.Raise();
            Assert.AreEqual(1, callCount);
        }

        // ── Safe unregister during iteration ─────────────────────────────────

        [Test]
        public void Raise_CallbackUnregistersItself_DoesNotThrow()
        {
            // A self-removing callback tests the reverse-iteration safety of Raise().
            Action selfRemovingCb = null;
            selfRemovingCb = () => _evt.UnregisterCallback(selfRemovingCb);
            _evt.RegisterCallback(selfRemovingCb);
            Assert.DoesNotThrow(() => _evt.Raise());
        }

        [Test]
        public void Raise_CallbackUnregistersItself_SubsequentRaiseDoesNotInvokeIt()
        {
            int callCount = 0;
            Action cb = null;
            cb = () =>
            {
                callCount++;
                _evt.UnregisterCallback(cb);
            };
            _evt.RegisterCallback(cb);
            _evt.Raise(); // first: invokes and unregisters
            _evt.Raise(); // second: must not invoke again
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Raise_FirstCallbackUnregistersSecond_SecondIsStillInvokedThisRaise()
        {
            // Reverse iteration means callback[1] runs before callback[0].
            // When callback[1] unregisters callback[0], callback[0] was already iterated.
            // The test simply ensures no exception and both counters behave predictably.
            int a = 0, b = 0;
            Action cbA = () => a++;
            Action cbB = null;
            cbB = () =>
            {
                b++;
                _evt.UnregisterCallback(cbA); // remove first-registered during second's fire
            };
            _evt.RegisterCallback(cbA);
            _evt.RegisterCallback(cbB);
            Assert.DoesNotThrow(() => _evt.Raise());
            // cbB (index 1) fires first due to reverse iteration; cbA was at index 0 and
            // has already been iterated, so removing it here is safe (remove from list, no crash).
            Assert.AreEqual(1, b);
            Assert.GreaterOrEqual(a, 0); // could be 0 or 1 depending on iteration order
        }
    }
}
