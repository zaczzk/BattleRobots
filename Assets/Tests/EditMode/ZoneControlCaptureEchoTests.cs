using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureEchoTests
    {
        private static ZoneControlCaptureEchoSO CreateSO(float window = 5f, int bonus = 150)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureEchoSO>();
            typeof(ZoneControlCaptureEchoSO)
                .GetField("_echoWindowSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, window);
            typeof(ZoneControlCaptureEchoSO)
                .GetField("_bonusPerEcho", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureEchoController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEchoController>();
        }

        [Test]
        public void SO_FreshInstance_EchoCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.EchoCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstCapture_NoPriorCapture_NoEcho()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            Assert.That(so.EchoCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SecondCapture_WithinWindow_IncrementsEcho()
        {
            var so = CreateSO(window: 5f);
            so.RecordCapture(0f);
            so.RecordCapture(3f);
            Assert.That(so.EchoCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SecondCapture_OutsideWindow_NoEcho()
        {
            var so = CreateSO(window: 5f);
            so.RecordCapture(0f);
            so.RecordCapture(6f);
            Assert.That(so.EchoCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SecondCapture_ExactlyAtWindowEdge_IsEcho()
        {
            var so = CreateSO(window: 5f);
            so.RecordCapture(0f);
            so.RecordCapture(5f);
            Assert.That(so.EchoCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleEchoes_AccumulatesCount()
        {
            var so = CreateSO(window: 5f);
            so.RecordCapture(0f);
            so.RecordCapture(2f);
            so.RecordCapture(4f);
            Assert.That(so.EchoCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Echo_AccumulatesBonus()
        {
            var so = CreateSO(window: 5f, bonus: 100);
            so.RecordCapture(0f);
            so.RecordCapture(2f);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            so.RecordCapture(2f);
            so.Reset();
            Assert.That(so.EchoCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.HasPriorCapture,   Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EchoSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EchoSO, Is.Null);
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
            typeof(ZoneControlCaptureEchoController)
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
