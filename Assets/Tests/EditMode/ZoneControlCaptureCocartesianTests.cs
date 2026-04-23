using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCocartesianTests
    {
        private static ZoneControlCaptureCocartesianSO CreateSO(
            int injectionsNeeded     = 7,
            int collapsePerBot       = 2,
            int bonusPerCodiagonalize = 3145)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCocartesianSO>();
            typeof(ZoneControlCaptureCocartesianSO)
                .GetField("_injectionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, injectionsNeeded);
            typeof(ZoneControlCaptureCocartesianSO)
                .GetField("_collapsePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, collapsePerBot);
            typeof(ZoneControlCaptureCocartesianSO)
                .GetField("_bonusPerCodiagonalize", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCodiagonalize);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCocartesianController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCocartesianController>();
        }

        [Test]
        public void SO_FreshInstance_Injections_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Injections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CodiagonalizeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CodiagonalizeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesInjections()
        {
            var so = CreateSO(injectionsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Injections, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(injectionsNeeded: 3, bonusPerCodiagonalize: 3145);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                   Is.EqualTo(3145));
            Assert.That(so.CodiagonalizeCount,   Is.EqualTo(1));
            Assert.That(so.Injections,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(injectionsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesInjections()
        {
            var so = CreateSO(injectionsNeeded: 7, collapsePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Injections, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(injectionsNeeded: 7, collapsePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Injections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_InjectionProgress_Clamped()
        {
            var so = CreateSO(injectionsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.InjectionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCodiagonalized_FiresEvent()
        {
            var so    = CreateSO(injectionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCocartesianSO)
                .GetField("_onCodiagonalized", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(injectionsNeeded: 2, bonusPerCodiagonalize: 3145);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Injections,         Is.EqualTo(0));
            Assert.That(so.CodiagonalizeCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCodiagonalizations_Accumulate()
        {
            var so = CreateSO(injectionsNeeded: 2, bonusPerCodiagonalize: 3145);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CodiagonalizeCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(6290));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CocartesianSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CocartesianSO, Is.Null);
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
            typeof(ZoneControlCaptureCocartesianController)
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
