using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTribeTests
    {
        private static ZoneControlCaptureTribeSO CreateSO(
            int capturesPerTier = 2, int maxTier = 5, int bonusPerTierCapture = 35)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTribeSO>();
            typeof(ZoneControlCaptureTribeSO)
                .GetField("_capturesPerTier", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesPerTier);
            typeof(ZoneControlCaptureTribeSO)
                .GetField("_maxTier", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxTier);
            typeof(ZoneControlCaptureTribeSO)
                .GetField("_bonusPerTierCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTierCapture);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTribeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTribeController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentTier_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentTier, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalBonusAwarded_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtTierZero_ReturnsZeroBonus()
        {
            var so    = CreateSO(capturesPerTier: 2, maxTier: 3, bonusPerTierCapture: 35);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AdvancesTier_AfterCapturesPerTier()
        {
            var so = CreateSO(capturesPerTier: 2, maxTier: 3, bonusPerTierCapture: 10);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentTier, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_TierOneCapture_AccumulatesBonus()
        {
            var so = CreateSO(capturesPerTier: 2, maxTier: 3, bonusPerTierCapture: 20);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(20));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_DoesNotExceedMaxTier()
        {
            var so = CreateSO(capturesPerTier: 1, maxTier: 2, bonusPerTierCapture: 10);
            for (int i = 0; i < 10; i++) so.RecordPlayerCapture();
            Assert.That(so.CurrentTier, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresMaxTierEvent()
        {
            var so    = CreateSO(capturesPerTier: 1, maxTier: 1, bonusPerTierCapture: 10);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTribeSO)
                .GetField("_onMaxTier", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_ResetsTier()
        {
            var so = CreateSO(capturesPerTier: 1, maxTier: 3, bonusPerTierCapture: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentTier, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ResetsTierProgress()
        {
            var so = CreateSO(capturesPerTier: 2, maxTier: 3, bonusPerTierCapture: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TierProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(capturesPerTier: 1, maxTier: 2, bonusPerTierCapture: 30);
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentTier,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.TierProgress,      Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TribeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TribeSO, Is.Null);
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
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureTribeController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);
            panel.SetActive(true);
            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_WithSO_ShowsPanel()
        {
            var ctrl  = CreateController();
            var so    = CreateSO();
            var panel = new GameObject();
            typeof(ZoneControlCaptureTribeController)
                .GetField("_tribeSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlCaptureTribeController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);
            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.True);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }
    }
}
