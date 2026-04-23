using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureProductTests
    {
        private static ZoneControlCaptureProductSO CreateSO(
            int factorsNeeded  = 5,
            int splitPerBot    = 1,
            int bonusPerProduct = 2890)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureProductSO>();
            typeof(ZoneControlCaptureProductSO)
                .GetField("_factorsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, factorsNeeded);
            typeof(ZoneControlCaptureProductSO)
                .GetField("_splitPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, splitPerBot);
            typeof(ZoneControlCaptureProductSO)
                .GetField("_bonusPerProduct", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerProduct);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureProductController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureProductController>();
        }

        [Test]
        public void SO_FreshInstance_Factors_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Factors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ProductCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ProductCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFactors()
        {
            var so = CreateSO(factorsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Factors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(factorsNeeded: 3, bonusPerProduct: 2890);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(2890));
            Assert.That(so.ProductCount,  Is.EqualTo(1));
            Assert.That(so.Factors,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(factorsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesFactors()
        {
            var so = CreateSO(factorsNeeded: 5, splitPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Factors, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(factorsNeeded: 5, splitPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Factors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FactorProgress_Clamped()
        {
            var so = CreateSO(factorsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.FactorProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnProductFormed_FiresEvent()
        {
            var so    = CreateSO(factorsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureProductSO)
                .GetField("_onProductFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(factorsNeeded: 2, bonusPerProduct: 2890);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Factors,           Is.EqualTo(0));
            Assert.That(so.ProductCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleProducts_Accumulate()
        {
            var so = CreateSO(factorsNeeded: 2, bonusPerProduct: 2890);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ProductCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5780));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ProductSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ProductSO, Is.Null);
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
            typeof(ZoneControlCaptureProductController)
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
