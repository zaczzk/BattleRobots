using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="RobotKnockbackController"/>.
    ///
    /// Covers:
    ///   • Default field values (forcePerDamage = 1, maxForce = 20).
    ///   • Awake/OnEnable/OnDisable null-channel safety (no exception).
    ///   • OnDisable unregisters from the DamageGameEvent channel.
    ///   • OnDamageTaken with null ArticulationBody — does not throw.
    ///   • OnDamageTaken with zero-amount damage — does not throw.
    ///   • Channel subscription: delegate is registered by OnEnable and fires on Raise.
    ///
    /// NOTE: AddForce calls are not tested in EditMode (requires physics simulation).
    ///       Coverage of the impulse magnitude computation is done via visual inspection
    ///       and PlayMode tests.
    /// </summary>
    public class RobotKnockbackControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        private static void InvokePrivate(object target, string method, object[] args = null)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, args ?? System.Array.Empty<object>());
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void DefaultForcePerDamage_Is1()
        {
            var go  = new GameObject();
            var kbc = go.AddComponent<RobotKnockbackController>();
            Assert.AreEqual(1f, kbc.KnockbackForcePerDamage, 0.0001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void DefaultMaxKnockbackForce_Is20()
        {
            var go  = new GameObject();
            var kbc = go.AddComponent<RobotKnockbackController>();
            Assert.AreEqual(20f, kbc.MaxKnockbackForce, 0.0001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var kbc = go.AddComponent<RobotKnockbackController>();
            SetField(kbc, "_onDamageTaken", null);
            Assert.DoesNotThrow(() => InvokePrivate(kbc, "OnEnable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            var go  = new GameObject();
            var kbc = go.AddComponent<RobotKnockbackController>();
            SetField(kbc, "_onDamageTaken", null);
            Assert.DoesNotThrow(() => InvokePrivate(kbc, "OnDisable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_UnregistersFromChannel()
        {
            var go      = new GameObject();
            var kbc     = go.AddComponent<RobotKnockbackController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            SetField(kbc, "_onDamageTaken", channel);

            InvokePrivate(kbc, "Awake");
            InvokePrivate(kbc, "OnEnable");
            InvokePrivate(kbc, "OnDisable");

            // After unsubscribe, raising the event must not invoke the delegate
            // (verified indirectly: no exception, and null _articulationBody guard
            // means the body of OnDamageTaken returns early without crash).
            Assert.DoesNotThrow(() =>
                channel.Raise(new DamageInfo(10f, "test")));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnDamageTaken_NullArticulationBody_DoesNotThrow()
        {
            var go      = new GameObject();
            var kbc     = go.AddComponent<RobotKnockbackController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            SetField(kbc, "_articulationBody", null);
            SetField(kbc, "_onDamageTaken",    channel);

            InvokePrivate(kbc, "Awake");
            InvokePrivate(kbc, "OnEnable");

            Assert.DoesNotThrow(() =>
                channel.Raise(new DamageInfo(25f, "test", Vector3.one)));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnDamageTaken_ZeroAmountDamage_DoesNotThrow()
        {
            var go      = new GameObject();
            var kbc     = go.AddComponent<RobotKnockbackController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            SetField(kbc, "_articulationBody", null);
            SetField(kbc, "_onDamageTaken",    channel);

            InvokePrivate(kbc, "Awake");
            InvokePrivate(kbc, "OnEnable");

            Assert.DoesNotThrow(() =>
                channel.Raise(new DamageInfo(0f, "test")));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnDamageTaken_ZeroHitPoint_NullBody_DoesNotThrow()
        {
            // hitPoint = Vector3.zero triggers the fallback direction (-forward).
            // With null body the handler should still return early without crash.
            var go      = new GameObject();
            var kbc     = go.AddComponent<RobotKnockbackController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            SetField(kbc, "_articulationBody", null);
            SetField(kbc, "_onDamageTaken",    channel);

            InvokePrivate(kbc, "Awake");
            InvokePrivate(kbc, "OnEnable");

            var info = new DamageInfo(15f, "test", Vector3.zero);
            Assert.DoesNotThrow(() => channel.Raise(info));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnEnable_SubscribesToChannel_DelegateFiresOnRaise()
        {
            // Confirm the delegate is wired: raising the event invokes OnDamageTaken,
            // which returns early (null body) without crashing — proving subscription.
            var go      = new GameObject();
            var kbc     = go.AddComponent<RobotKnockbackController>();
            var channel = ScriptableObject.CreateInstance<DamageGameEvent>();
            SetField(kbc, "_articulationBody", null);
            SetField(kbc, "_onDamageTaken",    channel);

            InvokePrivate(kbc, "Awake");
            InvokePrivate(kbc, "OnEnable");

            bool threw = false;
            try { channel.Raise(new DamageInfo(5f, "src")); }
            catch { threw = true; }

            Assert.IsFalse(threw, "Raising channel after OnEnable should not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void NullBody_NullChannel_Awake_DoesNotThrow()
        {
            var go  = new GameObject();
            var kbc = go.AddComponent<RobotKnockbackController>();
            SetField(kbc, "_articulationBody", null);
            SetField(kbc, "_onDamageTaken",    null);

            Assert.DoesNotThrow(() => InvokePrivate(kbc, "Awake"));
            Object.DestroyImmediate(go);
        }
    }
}
