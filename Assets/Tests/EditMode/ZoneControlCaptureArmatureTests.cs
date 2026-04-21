using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureArmatureTests
    {
        private static ZoneControlCaptureArmatureSO CreateSO(
            int coilsNeeded    = 5,
            int shortPerBot    = 1,
            int bonusPerWinding = 1345)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureArmatureSO>();
            typeof(ZoneControlCaptureArmatureSO)
                .GetField("_coilsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, coilsNeeded);
            typeof(ZoneControlCaptureArmatureSO)
                .GetField("_shortPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, shortPerBot);
            typeof(ZoneControlCaptureArmatureSO)
                .GetField("_bonusPerWinding", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerWinding);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureArmatureController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureArmatureController>();
        }

        [Test]
        public void SO_FreshInstance_Coils_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Coils, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_WindingCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.WindingCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCoils()
        {
            var so = CreateSO(coilsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Coils, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CoilsAtThreshold()
        {
            var so    = CreateSO(coilsNeeded: 3, bonusPerWinding: 1345);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(1345));
            Assert.That(so.WindingCount,  Is.EqualTo(1));
            Assert.That(so.Coils,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(coilsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesCoils()
        {
            var so = CreateSO(coilsNeeded: 5, shortPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Coils, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(coilsNeeded: 5, shortPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Coils, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CoilProgress_Clamped()
        {
            var so = CreateSO(coilsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CoilProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnArmatureWound_FiresEvent()
        {
            var so    = CreateSO(coilsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureArmatureSO)
                .GetField("_onArmatureWound", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(coilsNeeded: 2, bonusPerWinding: 1345);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Coils,             Is.EqualTo(0));
            Assert.That(so.WindingCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleWindings_Accumulate()
        {
            var so = CreateSO(coilsNeeded: 2, bonusPerWinding: 1345);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.WindingCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2690));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ArmatureSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ArmatureSO, Is.Null);
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
            typeof(ZoneControlCaptureArmatureController)
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
