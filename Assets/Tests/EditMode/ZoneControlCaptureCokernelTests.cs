using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCokernelTests
    {
        private static ZoneControlCaptureCokernelSO CreateSO(
            int vectorsNeeded    = 5,
            int erasePerBot      = 1,
            int bonusPerCokernel = 2830)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCokernelSO>();
            typeof(ZoneControlCaptureCokernelSO)
                .GetField("_vectorsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, vectorsNeeded);
            typeof(ZoneControlCaptureCokernelSO)
                .GetField("_erasePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, erasePerBot);
            typeof(ZoneControlCaptureCokernelSO)
                .GetField("_bonusPerCokernel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCokernel);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCokernelController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCokernelController>();
        }

        [Test]
        public void SO_FreshInstance_Vectors_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Vectors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CokernelCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CokernelCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesVectors()
        {
            var so = CreateSO(vectorsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Vectors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(vectorsNeeded: 3, bonusPerCokernel: 2830);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(2830));
            Assert.That(so.CokernelCount,  Is.EqualTo(1));
            Assert.That(so.Vectors,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(vectorsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesVectors()
        {
            var so = CreateSO(vectorsNeeded: 5, erasePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Vectors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(vectorsNeeded: 5, erasePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Vectors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VectorProgress_Clamped()
        {
            var so = CreateSO(vectorsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.VectorProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCokernelProjected_FiresEvent()
        {
            var so    = CreateSO(vectorsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCokernelSO)
                .GetField("_onCokernelProjected", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(vectorsNeeded: 2, bonusPerCokernel: 2830);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Vectors,           Is.EqualTo(0));
            Assert.That(so.CokernelCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCokernels_Accumulate()
        {
            var so = CreateSO(vectorsNeeded: 2, bonusPerCokernel: 2830);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CokernelCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5660));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CokernelSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CokernelSO, Is.Null);
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
            typeof(ZoneControlCaptureCokernelController)
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
