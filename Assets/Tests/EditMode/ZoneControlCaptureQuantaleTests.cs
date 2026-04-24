using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureQuantaleTests
    {
        private static ZoneControlCaptureQuantaleSO CreateSO(
            int compositesNeeded = 5,
            int decomposePerBot  = 1,
            int bonusPerCompose  = 3310)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureQuantaleSO>();
            typeof(ZoneControlCaptureQuantaleSO)
                .GetField("_compositesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, compositesNeeded);
            typeof(ZoneControlCaptureQuantaleSO)
                .GetField("_decomposePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, decomposePerBot);
            typeof(ZoneControlCaptureQuantaleSO)
                .GetField("_bonusPerCompose", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCompose);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureQuantaleController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureQuantaleController>();
        }

        [Test]
        public void SO_FreshInstance_Composites_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Composites, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ComposeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ComposeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesComposites()
        {
            var so = CreateSO(compositesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Composites, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(compositesNeeded: 3, bonusPerCompose: 3310);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(3310));
            Assert.That(so.ComposeCount, Is.EqualTo(1));
            Assert.That(so.Composites,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(compositesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesComposites()
        {
            var so = CreateSO(compositesNeeded: 5, decomposePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Composites, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(compositesNeeded: 5, decomposePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Composites, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CompositeProgress_Clamped()
        {
            var so = CreateSO(compositesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CompositeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnQuantaleComposed_FiresEvent()
        {
            var so    = CreateSO(compositesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureQuantaleSO)
                .GetField("_onQuantaleComposed", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(compositesNeeded: 2, bonusPerCompose: 3310);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Composites,        Is.EqualTo(0));
            Assert.That(so.ComposeCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCompositions_Accumulate()
        {
            var so = CreateSO(compositesNeeded: 2, bonusPerCompose: 3310);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ComposeCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6620));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_QuantaleSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.QuantaleSO, Is.Null);
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
            typeof(ZoneControlCaptureQuantaleController)
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
