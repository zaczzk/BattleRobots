using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAuraTests
    {
        private static ZoneControlCaptureAuraSO CreateSO(
            float auraPerCapture = 20f, float maxAura = 100f, float threshold = 0.5f,
            float decayRate = 8f, int bonusPerCapture = 75)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAuraSO>();
            typeof(ZoneControlCaptureAuraSO)
                .GetField("_auraPerCapture",  BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, auraPerCapture);
            typeof(ZoneControlCaptureAuraSO)
                .GetField("_maxAura", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxAura);
            typeof(ZoneControlCaptureAuraSO)
                .GetField("_auraThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, threshold);
            typeof(ZoneControlCaptureAuraSO)
                .GetField("_decayRate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, decayRate);
            typeof(ZoneControlCaptureAuraSO)
                .GetField("_bonusPerCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCapture);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAuraController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAuraController>();
        }

        [Test]
        public void SO_FreshInstance_IsAuraActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsAuraActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FillsAura()
        {
            var so = CreateSO(auraPerCapture: 30f, maxAura: 100f);
            so.RecordCapture();
            Assert.That(so.CurrentAura, Is.EqualTo(30f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AboveThreshold_AuraActivates()
        {
            // threshold 0.5 * 100 = 50; fill 60 per capture
            var so = CreateSO(auraPerCapture: 60f, maxAura: 100f, threshold: 0.5f);
            so.RecordCapture();
            Assert.That(so.IsAuraActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AuraActive_ReturnsBonus()
        {
            var so    = CreateSO(auraPerCapture: 60f, maxAura: 100f, threshold: 0.5f, bonusPerCapture: 75);
            int bonus = so.RecordCapture();
            Assert.That(bonus, Is.EqualTo(75));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AuraBelowThreshold_ReturnsZero()
        {
            // threshold 0.9 * 100 = 90; fill 20 per capture — won't activate
            var so    = CreateSO(auraPerCapture: 20f, maxAura: 100f, threshold: 0.9f, bonusPerCapture: 75);
            int bonus = so.RecordCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_DecaysAura()
        {
            var so = CreateSO(auraPerCapture: 100f, maxAura: 100f, threshold: 0.5f, decayRate: 10f);
            so.RecordCapture();
            float before = so.CurrentAura;
            so.Tick(1f);
            Assert.That(so.CurrentAura, Is.LessThan(before));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BelowThreshold_DeactivatesAura()
        {
            // Activate aura, then decay it below threshold
            var so = CreateSO(auraPerCapture: 60f, maxAura: 100f, threshold: 0.5f, decayRate: 100f);
            so.RecordCapture();
            Assert.That(so.IsAuraActive, Is.True);
            so.Tick(1f);
            Assert.That(so.IsAuraActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(auraPerCapture: 60f, maxAura: 100f, threshold: 0.5f);
            so.RecordCapture();
            so.Reset();
            Assert.That(so.CurrentAura,    Is.EqualTo(0f));
            Assert.That(so.IsAuraActive,   Is.False);
            Assert.That(so.TotalAuraBonus, Is.EqualTo(0));
            Assert.That(so.AuraCaptures,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AuraSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AuraSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureAuraController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
