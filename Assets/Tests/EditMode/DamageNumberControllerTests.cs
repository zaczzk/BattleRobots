using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DamageNumberController"/>.
    ///
    /// Covers:
    ///   • Fresh instance has null config and NextIsCrit = false.
    ///   • OnEnable / OnDisable with all-null channels — no exception.
    ///   • OnDisable clears the NextIsCrit flag.
    ///   • _onCriticalHit channel raise sets NextIsCrit = true.
    ///   • Subsequent _onDamageTaken raise (null config) clears NextIsCrit.
    ///   • OnDisable unregisters from both channels (no crash after unsubscribe).
    ///   • OnDamageTaken with null config — no exception.
    ///   • OnCriticalHit + OnDamageTaken sequence — flag consumed on first hit.
    /// </summary>
    public class DamageNumberControllerTests
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
        public void FreshInstance_Config_IsNull()
        {
            var go  = new GameObject();
            var dnc = go.AddComponent<DamageNumberController>();
            Assert.IsNull(dnc.Config);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FreshInstance_NextIsCrit_IsFalse()
        {
            var go  = new GameObject();
            var dnc = go.AddComponent<DamageNumberController>();
            Assert.IsFalse(dnc.NextIsCrit);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var dnc = go.AddComponent<DamageNumberController>();
            SetField(dnc, "_config",       null);
            SetField(dnc, "_onDamageTaken", null);
            SetField(dnc, "_onCriticalHit", null);
            SetField(dnc, "_spawnAnchor",   null);

            InvokePrivate(dnc, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(dnc, "OnEnable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var dnc = go.AddComponent<DamageNumberController>();
            SetField(dnc, "_config",       null);
            SetField(dnc, "_onDamageTaken", null);
            SetField(dnc, "_onCriticalHit", null);

            InvokePrivate(dnc, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(dnc, "OnDisable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_ClearsNextIsCritFlag()
        {
            var go  = new GameObject();
            var dnc = go.AddComponent<DamageNumberController>();
            SetField(dnc, "_onDamageTaken", null);
            SetField(dnc, "_onCriticalHit", null);

            InvokePrivate(dnc, "Awake");
            // Manually set the flag via the crit delegate path.
            InvokePrivate(dnc, "OnCriticalHit");
            Assert.IsTrue(dnc.NextIsCrit, "Pre-condition: flag must be set.");

            InvokePrivate(dnc, "OnDisable");
            Assert.IsFalse(dnc.NextIsCrit, "OnDisable must clear the stale crit flag.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CritChannel_Raise_SetsNextIsCrit_True()
        {
            var go      = new GameObject();
            var dnc     = go.AddComponent<DamageNumberController>();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(dnc, "_onDamageTaken", null);
            SetField(dnc, "_onCriticalHit", channel);

            InvokePrivate(dnc, "Awake");
            InvokePrivate(dnc, "OnEnable");

            channel.Raise();

            Assert.IsTrue(dnc.NextIsCrit, "Raising _onCriticalHit must set NextIsCrit = true.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void DamageChannel_NullConfig_DoesNotThrow_CritFlagFalse()
        {
            var go          = new GameObject();
            var dnc         = go.AddComponent<DamageNumberController>();
            var dmgChannel  = ScriptableObject.CreateInstance<DamageGameEvent>();
            SetField(dnc, "_config",        null);
            SetField(dnc, "_onDamageTaken", dmgChannel);
            SetField(dnc, "_onCriticalHit", null);

            InvokePrivate(dnc, "Awake");
            InvokePrivate(dnc, "OnEnable");

            Assert.DoesNotThrow(() => dmgChannel.Raise(new DamageInfo(20f, "src")));
            Assert.IsFalse(dnc.NextIsCrit);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(dmgChannel);
        }

        [Test]
        public void CritThenDamage_FlagConsumedOnFirstHit()
        {
            var go          = new GameObject();
            var dnc         = go.AddComponent<DamageNumberController>();
            var dmgChannel  = ScriptableObject.CreateInstance<DamageGameEvent>();
            var critChannel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(dnc, "_config",        null);   // no config: SpawnNumber no-ops
            SetField(dnc, "_onDamageTaken", dmgChannel);
            SetField(dnc, "_onCriticalHit", critChannel);

            InvokePrivate(dnc, "Awake");
            InvokePrivate(dnc, "OnEnable");

            // Simulate crit-then-damage sequence.
            critChannel.Raise();
            Assert.IsTrue(dnc.NextIsCrit, "After crit raise, flag must be set.");

            dmgChannel.Raise(new DamageInfo(30f, "src"));
            Assert.IsFalse(dnc.NextIsCrit, "Flag must be consumed by the damage event.");

            // Second damage without preceding crit must also leave flag false.
            dmgChannel.Raise(new DamageInfo(10f, "src"));
            Assert.IsFalse(dnc.NextIsCrit);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(dmgChannel);
            Object.DestroyImmediate(critChannel);
        }

        [Test]
        public void OnDisable_UnregistersFromBothChannels()
        {
            var go          = new GameObject();
            var dnc         = go.AddComponent<DamageNumberController>();
            var dmgChannel  = ScriptableObject.CreateInstance<DamageGameEvent>();
            var critChannel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(dnc, "_config",        null);
            SetField(dnc, "_onDamageTaken", dmgChannel);
            SetField(dnc, "_onCriticalHit", critChannel);

            InvokePrivate(dnc, "Awake");
            InvokePrivate(dnc, "OnEnable");
            InvokePrivate(dnc, "OnDisable");

            // Raising both channels after unsubscribe must not throw.
            Assert.DoesNotThrow(() => critChannel.Raise());
            Assert.DoesNotThrow(() => dmgChannel.Raise(new DamageInfo(5f, "src")));
            Assert.IsFalse(dnc.NextIsCrit, "Flag must remain false after unsubscribed raise.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(dmgChannel);
            Object.DestroyImmediate(critChannel);
        }
    }
}
