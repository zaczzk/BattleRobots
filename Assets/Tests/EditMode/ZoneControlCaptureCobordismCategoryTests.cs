using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCobordismCategoryTests
    {
        private static ZoneControlCaptureCobordismCategorySO CreateSO(
            int bordismsNeeded      = 5,
            int singularitiesPerBot = 1,
            int bonusPerComposition = 4120)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCobordismCategorySO>();
            typeof(ZoneControlCaptureCobordismCategorySO)
                .GetField("_bordismsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bordismsNeeded);
            typeof(ZoneControlCaptureCobordismCategorySO)
                .GetField("_singularitiesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, singularitiesPerBot);
            typeof(ZoneControlCaptureCobordismCategorySO)
                .GetField("_bonusPerComposition", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerComposition);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCobordismCategoryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCobordismCategoryController>();
        }

        [Test]
        public void SO_FreshInstance_Bordisms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Bordisms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CompositionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CompositionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBordisms()
        {
            var so = CreateSO(bordismsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Bordisms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(bordismsNeeded: 3, bonusPerComposition: 4120);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(4120));
            Assert.That(so.CompositionCount, Is.EqualTo(1));
            Assert.That(so.Bordisms,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(bordismsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesSingularities()
        {
            var so = CreateSO(bordismsNeeded: 5, singularitiesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bordisms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bordismsNeeded: 5, singularitiesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bordisms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BordismProgress_Clamped()
        {
            var so = CreateSO(bordismsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.BordismProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCobordismCategoryComposed_FiresEvent()
        {
            var so    = CreateSO(bordismsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCobordismCategorySO)
                .GetField("_onCobordismCategoryComposed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(bordismsNeeded: 2, bonusPerComposition: 4120);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Bordisms,          Is.EqualTo(0));
            Assert.That(so.CompositionCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCompositions_Accumulate()
        {
            var so = CreateSO(bordismsNeeded: 2, bonusPerComposition: 4120);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CompositionCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8240));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CobordismCategorySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CobordismCategorySO, Is.Null);
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
            typeof(ZoneControlCaptureCobordismCategoryController)
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
