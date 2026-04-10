using System;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DamageGameEvent"/> (GameEvent&lt;DamageInfo&gt;).
    ///
    /// Verifies that the DamageInfo struct payload is delivered intact — all three
    /// fields (amount, sourceId, hitPoint) — to every registered callback.
    /// Also exercises the duplicate-registration guard and safe unregistration
    /// during iteration, which are inherited from the base GameEvent&lt;T&gt; class.
    ///
    /// This event type is the backbone of the damage pipeline: RobotAIController
    /// raises it, DamageGameEventListener → DamageReceiver → HealthSO consume it.
    /// </summary>
    public class DamageGameEventTests
    {
        private DamageGameEvent _evt;

        [SetUp]
        public void SetUp()
        {
            _evt = ScriptableObject.CreateInstance<DamageGameEvent>();
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
            var info = new DamageInfo(10f, "enemy", Vector3.zero);
            Assert.DoesNotThrow(() => _evt.Raise(info));
        }

        [Test]
        public void Raise_InvokesRegisteredCallback()
        {
            int callCount = 0;
            _evt.RegisterCallback(d => callCount++);
            _evt.Raise(new DamageInfo(5f));
            Assert.AreEqual(1, callCount);
        }

        // ── Payload field delivery ────────────────────────────────────────────

        [Test]
        public void Raise_DeliversCorrectAmount()
        {
            float received = -1f;
            _evt.RegisterCallback(d => received = d.amount);
            _evt.Raise(new DamageInfo(42f));
            Assert.AreEqual(42f, received, 0.0001f);
        }

        [Test]
        public void Raise_ZeroAmount_IsDeliveredCorrectly()
        {
            float received = -1f;
            _evt.RegisterCallback(d => received = d.amount);
            _evt.Raise(new DamageInfo(0f));
            Assert.AreEqual(0f, received, 0.0001f);
        }

        [Test]
        public void Raise_DeliversCorrectSourceId()
        {
            string received = null;
            _evt.RegisterCallback(d => received = d.sourceId);
            _evt.Raise(new DamageInfo(10f, "robot_boss"));
            Assert.AreEqual("robot_boss", received);
        }

        [Test]
        public void Raise_EmptySourceId_IsDeliveredAsEmptyString()
        {
            string received = null;
            _evt.RegisterCallback(d => received = d.sourceId);
            _evt.Raise(new DamageInfo(10f, ""));
            Assert.AreEqual(string.Empty, received);
        }

        [Test]
        public void Raise_DeliversCorrectHitPoint()
        {
            Vector3 received = Vector3.zero;
            _evt.RegisterCallback(d => received = d.hitPoint);
            var expected = new Vector3(1f, 2f, 3f);
            _evt.Raise(new DamageInfo(10f, "", expected));
            Assert.AreEqual(expected, received);
        }

        [Test]
        public void Raise_DeliversFullPayload_ToAllCallbacks()
        {
            float a = -1f, b = -1f;
            _evt.RegisterCallback(d => a = d.amount);
            _evt.RegisterCallback(d => b = d.amount);
            _evt.Raise(new DamageInfo(99f));
            Assert.AreEqual(99f, a, 0.0001f);
            Assert.AreEqual(99f, b, 0.0001f);
        }

        // ── Unregister ────────────────────────────────────────────────────────

        [Test]
        public void Raise_AfterUnregister_DoesNotInvokeCallback()
        {
            int callCount = 0;
            Action<DamageInfo> cb = d => callCount++;
            _evt.RegisterCallback(cb);
            _evt.UnregisterCallback(cb);
            _evt.Raise(new DamageInfo(10f));
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Unregister_UnknownCallback_DoesNotThrow()
        {
            Action<DamageInfo> cb = d => { };
            Assert.DoesNotThrow(() => _evt.UnregisterCallback(cb));
        }

        // ── Duplicate guard ───────────────────────────────────────────────────

        [Test]
        public void RegisterCallback_Duplicate_InvokedOncePerRaise()
        {
            int callCount = 0;
            Action<DamageInfo> cb = d => callCount++;
            _evt.RegisterCallback(cb);
            _evt.RegisterCallback(cb); // duplicate — ignored
            _evt.Raise(new DamageInfo(10f));
            Assert.AreEqual(1, callCount);
        }

        // ── Safe unregistration during iteration ──────────────────────────────

        [Test]
        public void Raise_SelfRemovingCallback_DoesNotThrow()
        {
            Action<DamageInfo> selfRemovingCb = null;
            selfRemovingCb = d => _evt.UnregisterCallback(selfRemovingCb);
            _evt.RegisterCallback(selfRemovingCb);
            Assert.DoesNotThrow(() => _evt.Raise(new DamageInfo(5f)));
        }

        [Test]
        public void Raise_SelfRemovingCallback_NotCalledOnSubsequentRaise()
        {
            int callCount = 0;
            Action<DamageInfo> cb = null;
            cb = d =>
            {
                callCount++;
                _evt.UnregisterCallback(cb);
            };
            _evt.RegisterCallback(cb);
            _evt.Raise(new DamageInfo(1f));
            _evt.Raise(new DamageInfo(1f));
            Assert.AreEqual(1, callCount);
        }
    }
}
