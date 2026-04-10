using System;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="AudioEvent"/> ScriptableObject.
    ///
    /// Covers:
    ///   • Default property values (Volume, PitchMin, PitchMax).
    ///   • <see cref="AudioEvent.PickClip"/> with no clips assigned → null.
    ///   • <see cref="AudioEvent.Raise"/> callback invocation: no-throw, invoke,
    ///     payload (self reference), multi-subscriber.
    ///   • <see cref="AudioEvent.UnregisterCallback"/> removes subscriber.
    ///   • Duplicate-register guard — callback invoked exactly once per Raise.
    ///   • Safe self-unregister during iteration (reverse-iteration guarantee).
    ///
    /// Mirrors the patterns established in VoidGameEventTests and IntGameEventTests.
    /// </summary>
    public class AudioEventTests
    {
        private AudioEvent _evt;

        [SetUp]
        public void SetUp()
        {
            _evt = ScriptableObject.CreateInstance<AudioEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_evt);
            _evt = null;
        }

        // ── Default property values ────────────────────────────────────────────

        [Test]
        public void FreshInstance_Volume_IsOne()
        {
            // Inspector: Range(0, 1); default 1f.
            Assert.AreEqual(1f, _evt.Volume, 0.0001f);
        }

        [Test]
        public void FreshInstance_PitchMin_IsInValidRange()
        {
            // Inspector: Range(0.5f, 2f); default 0.9f.
            Assert.GreaterOrEqual(_evt.PitchMin, 0.5f);
            Assert.LessOrEqual(_evt.PitchMin, 2f);
        }

        [Test]
        public void FreshInstance_PitchMax_IsInValidRange()
        {
            // Inspector: Range(0.5f, 2f); default 1.1f.
            Assert.GreaterOrEqual(_evt.PitchMax, 0.5f);
            Assert.LessOrEqual(_evt.PitchMax, 2f);
        }

        [Test]
        public void FreshInstance_PitchMax_IsGreaterOrEqualToPitchMin()
        {
            // Ensures AudioManager can call Random.Range(PitchMin, PitchMax) safely.
            Assert.GreaterOrEqual(_evt.PitchMax, _evt.PitchMin);
        }

        // ── PickClip ───────────────────────────────────────────────────────────

        [Test]
        public void PickClip_WithNoClipsAssigned_ReturnsNull()
        {
            // Default CreateInstance has no clips; PickClip must not throw.
            AudioClip result = null;
            Assert.DoesNotThrow(() => result = _evt.PickClip());
            Assert.IsNull(result);
        }

        // ── Raise — basic invocation ────────────────────────────────────────────

        [Test]
        public void Raise_WithNoCallbacks_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _evt.Raise());
        }

        [Test]
        public void Raise_InvokesRegisteredCallback()
        {
            int callCount = 0;
            _evt.RegisterCallback(_ => callCount++);
            _evt.Raise();
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Raise_PassesEventSelfAsPayload()
        {
            // AudioManager receives the AudioEvent SO so it can read Volume/PitchMin/PitchMax.
            AudioEvent received = null;
            _evt.RegisterCallback(ae => received = ae);
            _evt.Raise();
            Assert.AreSame(_evt, received);
        }

        [Test]
        public void Raise_InvokesAllRegisteredCallbacks()
        {
            int a = 0, b = 0, c = 0;
            _evt.RegisterCallback(_ => a++);
            _evt.RegisterCallback(_ => b++);
            _evt.RegisterCallback(_ => c++);
            _evt.Raise();
            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
            Assert.AreEqual(1, c);
        }

        [Test]
        public void Raise_InvokesCallbackEveryTime()
        {
            int callCount = 0;
            _evt.RegisterCallback(_ => callCount++);
            _evt.Raise();
            _evt.Raise();
            _evt.Raise();
            Assert.AreEqual(3, callCount);
        }

        // ── Unregister ─────────────────────────────────────────────────────────

        [Test]
        public void Unregister_RemovesCallback_NotInvokedOnSubsequentRaise()
        {
            int callCount = 0;
            Action<AudioEvent> cb = _ => callCount++;
            _evt.RegisterCallback(cb);
            _evt.UnregisterCallback(cb);
            _evt.Raise();
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Unregister_UnknownCallback_DoesNotThrow()
        {
            Action<AudioEvent> cb = _ => { };
            Assert.DoesNotThrow(() => _evt.UnregisterCallback(cb));
        }

        [Test]
        public void Unregister_AfterPartialRemoval_OnlyRemainingCallbackInvoked()
        {
            int a = 0, b = 0;
            Action<AudioEvent> cbA = _ => a++;
            Action<AudioEvent> cbB = _ => b++;
            _evt.RegisterCallback(cbA);
            _evt.RegisterCallback(cbB);
            _evt.UnregisterCallback(cbA);
            _evt.Raise();
            Assert.AreEqual(0, a);
            Assert.AreEqual(1, b);
        }

        // ── Duplicate guard ────────────────────────────────────────────────────

        [Test]
        public void RegisterCallback_Duplicate_IsIgnored_CallbackInvokedOnce()
        {
            int callCount = 0;
            Action<AudioEvent> cb = _ => callCount++;
            _evt.RegisterCallback(cb);
            _evt.RegisterCallback(cb);   // duplicate — must be silently ignored
            _evt.Raise();
            Assert.AreEqual(1, callCount);
        }

        // ── Safe self-unregister during iteration ──────────────────────────────

        [Test]
        public void Raise_CallbackUnregistersItself_DoesNotThrow()
        {
            Action<AudioEvent> selfRemoving = null;
            selfRemoving = ae => _evt.UnregisterCallback(selfRemoving);
            _evt.RegisterCallback(selfRemoving);
            Assert.DoesNotThrow(() => _evt.Raise());
        }

        [Test]
        public void Raise_CallbackUnregistersItself_SubsequentRaiseDoesNotInvokeIt()
        {
            int callCount = 0;
            Action<AudioEvent> cb = null;
            cb = ae =>
            {
                callCount++;
                _evt.UnregisterCallback(cb);
            };
            _evt.RegisterCallback(cb);
            _evt.Raise();   // invoke + unregister
            _evt.Raise();   // must not invoke again
            Assert.AreEqual(1, callCount);
        }
    }
}
