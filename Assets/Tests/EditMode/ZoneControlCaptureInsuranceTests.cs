using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureInsuranceTests
    {
        private static ZoneControlCaptureInsuranceSO CreateSO(int fill = 50, int maxPool = 500, float payout = 0.5f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureInsuranceSO>();
            typeof(ZoneControlCaptureInsuranceSO)
                .GetField("_fillPerCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, fill);
            typeof(ZoneControlCaptureInsuranceSO)
                .GetField("_maxPool", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxPool);
            typeof(ZoneControlCaptureInsuranceSO)
                .GetField("_payoutFraction", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, payout);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureInsuranceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureInsuranceController>();
        }

        [Test]
        public void SO_FreshInstance_Pool_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Pool, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FillsPool()
        {
            var so = CreateSO(fill: 100);
            so.RecordPlayerCapture();
            Assert.That(so.Pool, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CapsAtMax()
        {
            var so = CreateSO(fill: 300, maxPool: 500);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.Pool, Is.EqualTo(500));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_PaysPayout()
        {
            var so = CreateSO(fill: 200, maxPool: 500, payout: 0.5f);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.LastPayout, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_EmptyPool_NoPayout()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.LastPayout, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AccumulatesTotalPaidOut()
        {
            var so = CreateSO(fill: 200, payout: 0.5f);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TotalPaidOut, Is.GreaterThan(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_FiresEvent()
        {
            var so    = CreateSO(fill: 100, payout: 1f);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureInsuranceSO)
                .GetField("_onInsurancePayout", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(fill: 100);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.Pool,         Is.EqualTo(0));
            Assert.That(so.TotalPaidOut, Is.EqualTo(0));
            Assert.That(so.LastPayout,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_InsuranceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.InsuranceSO, Is.Null);
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
            typeof(ZoneControlCaptureInsuranceController)
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
