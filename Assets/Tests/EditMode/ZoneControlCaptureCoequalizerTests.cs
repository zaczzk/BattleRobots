using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCoequalizerTests
    {
        private static ZoneControlCaptureCoequalizerSO CreateSO(
            int classesNeeded       = 5,
            int dissolvePerBot      = 1,
            int bonusPerCoequalizer = 2800)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCoequalizerSO>();
            typeof(ZoneControlCaptureCoequalizerSO)
                .GetField("_classesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, classesNeeded);
            typeof(ZoneControlCaptureCoequalizerSO)
                .GetField("_dissolvePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dissolvePerBot);
            typeof(ZoneControlCaptureCoequalizerSO)
                .GetField("_bonusPerCoequalizer", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCoequalizer);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCoequalizerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCoequalizerController>();
        }

        [Test]
        public void SO_FreshInstance_Classes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Classes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CoequalizerCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CoequalizerCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesClasses()
        {
            var so = CreateSO(classesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Classes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(classesNeeded: 3, bonusPerCoequalizer: 2800);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                 Is.EqualTo(2800));
            Assert.That(so.CoequalizerCount,  Is.EqualTo(1));
            Assert.That(so.Classes,            Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(classesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesClasses()
        {
            var so = CreateSO(classesNeeded: 5, dissolvePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Classes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(classesNeeded: 5, dissolvePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Classes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ClassProgress_Clamped()
        {
            var so = CreateSO(classesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ClassProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCoequalizerFormed_FiresEvent()
        {
            var so    = CreateSO(classesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCoequalizerSO)
                .GetField("_onCoequalizerFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(classesNeeded: 2, bonusPerCoequalizer: 2800);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Classes,           Is.EqualTo(0));
            Assert.That(so.CoequalizerCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCoequalizers_Accumulate()
        {
            var so = CreateSO(classesNeeded: 2, bonusPerCoequalizer: 2800);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CoequalizerCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5600));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CoequalizerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CoequalizerSO, Is.Null);
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
            typeof(ZoneControlCaptureCoequalizerController)
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
