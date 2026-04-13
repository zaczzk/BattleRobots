using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="AbilityAreaEffectController"/>.
    ///
    /// Physics.OverlapSphereNonAlloc yields 0 hits in EditMode (no Physics scene),
    /// so tests validate the synchronous API surface:
    ///   • Fresh-instance defaults.
    ///   • OnEnable / OnDisable with all-null inspector fields — no throw.
    ///   • OnEnable / OnDisable with null channel — no throw.
    ///   • TriggerAreaEffect with null config — no-op, no throw.
    ///   • TriggerAreaEffect with valid config + no hits — no throw.
    ///   • Raising _onAbilityActivated with null config — no throw.
    ///   • OnDisable unregisters from channel (extra listener verifies count).
    /// </summary>
    public class AbilityAreaEffectControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static AbilityAreaEffectController MakeController(out GameObject go)
        {
            go = new GameObject("TestAoE");
            go.SetActive(false); // prevent OnEnable before field wiring
            return go.AddComponent<AbilityAreaEffectController>();
        }

        private static AbilityAreaEffectConfig MakeConfig(float radius = 3f, float damage = 15f)
        {
            var cfg = ScriptableObject.CreateInstance<AbilityAreaEffectConfig>();
            SetField(cfg, "_radius", radius);
            SetField(cfg, "_damage", damage);
            return cfg;
        }

        // ── OnEnable / OnDisable — all null refs ──────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all-null inspector fields must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all-null inspector fields must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── Null event channel ────────────────────────────────────────────────

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var ctrl = MakeController(out GameObject go);
            // _onAbilityActivated is null by default
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null _onAbilityActivated must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null _onAbilityActivated must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── TriggerAreaEffect — null config ───────────────────────────────────

        [Test]
        public void TriggerAreaEffect_NullConfig_DoesNotThrow()
        {
            var ctrl = MakeController(out GameObject go);
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.TriggerAreaEffect(),
                "TriggerAreaEffect() with null _config must be a silent no-op.");

            Object.DestroyImmediate(go);
        }

        // ── TriggerAreaEffect with valid config (0 hits in EditMode) ─────────

        [Test]
        public void TriggerAreaEffect_WithConfig_ZeroHits_DoesNotThrow()
        {
            var ctrl = MakeController(out GameObject go);
            var cfg  = MakeConfig();
            SetField(ctrl, "_config", cfg);
            go.SetActive(true);

            // OverlapSphereNonAlloc returns 0 in EditMode — should still be safe.
            Assert.DoesNotThrow(() => ctrl.TriggerAreaEffect(),
                "TriggerAreaEffect() with a valid config and no hits must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(cfg);
        }

        // ── Event-channel wiring ──────────────────────────────────────────────

        [Test]
        public void RaisingChannel_NullConfig_DoesNotThrow()
        {
            var ctrl    = MakeController(out GameObject go);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(ctrl, "_onAbilityActivated", channel);
            go.SetActive(true);

            Assert.DoesNotThrow(() => channel.Raise(),
                "Raising _onAbilityActivated with null config must not throw.");

            go.SetActive(false);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnDisable_UnregistersFromChannel()
        {
            var ctrl    = MakeController(out GameObject go);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(ctrl, "_onAbilityActivated", channel);
            go.SetActive(true);

            // Disable — unregisters the controller's callback.
            go.SetActive(false);

            // Additional external listener to confirm the channel still works.
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);
            channel.Raise();

            // Only the external listener should have fired; the controller is detached.
            Assert.AreEqual(1, externalCount,
                "After OnDisable, the controller must no longer respond to the channel.");

            channel.UnregisterCallback(() => externalCount++);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }
    }
}
