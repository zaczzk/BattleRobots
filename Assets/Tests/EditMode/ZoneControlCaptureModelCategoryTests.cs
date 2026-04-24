using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureModelCategoryTests
    {
        private static ZoneControlCaptureModelCategorySO CreateSO(
            int weakEquivNeeded  = 6,
            int breakPerBot      = 2,
            int bonusPerLocalize = 3700)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureModelCategorySO>();
            typeof(ZoneControlCaptureModelCategorySO)
                .GetField("_weakEquivNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, weakEquivNeeded);
            typeof(ZoneControlCaptureModelCategorySO)
                .GetField("_breakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, breakPerBot);
            typeof(ZoneControlCaptureModelCategorySO)
                .GetField("_bonusPerLocalize", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLocalize);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureModelCategoryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureModelCategoryController>();
        }

        [Test]
        public void SO_FreshInstance_WeakEquivs_Zero()
        {
            var so = CreateSO();
            Assert.That(so.WeakEquivs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LocalizeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LocalizeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesWeakEquivs()
        {
            var so = CreateSO(weakEquivNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.WeakEquivs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(weakEquivNeeded: 3, bonusPerLocalize: 3700);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(3700));
            Assert.That(so.LocalizeCount,  Is.EqualTo(1));
            Assert.That(so.WeakEquivs,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(weakEquivNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BreaksFactorizationAxioms()
        {
            var so = CreateSO(weakEquivNeeded: 6, breakPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.WeakEquivs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(weakEquivNeeded: 6, breakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.WeakEquivs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WeakEquivProgress_Clamped()
        {
            var so = CreateSO(weakEquivNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.WeakEquivProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnModelCategoryLocalized_FiresEvent()
        {
            var so    = CreateSO(weakEquivNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureModelCategorySO)
                .GetField("_onModelCategoryLocalized", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(weakEquivNeeded: 2, bonusPerLocalize: 3700);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.WeakEquivs,        Is.EqualTo(0));
            Assert.That(so.LocalizeCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLocalizations_Accumulate()
        {
            var so = CreateSO(weakEquivNeeded: 2, bonusPerLocalize: 3700);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.LocalizeCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7400));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ModelCategorySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ModelCategorySO, Is.Null);
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
            typeof(ZoneControlCaptureModelCategoryController)
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
