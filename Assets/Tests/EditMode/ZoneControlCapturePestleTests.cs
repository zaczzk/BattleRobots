using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePestleTests
    {
        private static ZoneControlCapturePestleSO CreateSO(
            int poundsNeeded  = 5,
            int scatterPerBot = 1,
            int bonusPerBatch = 1090)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePestleSO>();
            typeof(ZoneControlCapturePestleSO)
                .GetField("_poundsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, poundsNeeded);
            typeof(ZoneControlCapturePestleSO)
                .GetField("_scatterPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, scatterPerBot);
            typeof(ZoneControlCapturePestleSO)
                .GetField("_bonusPerBatch", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBatch);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePestleController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePestleController>();
        }

        [Test]
        public void SO_FreshInstance_Pounds_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Pounds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BatchCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BatchCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPounds()
        {
            var so = CreateSO(poundsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Pounds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_PoundsAtThreshold()
        {
            var so    = CreateSO(poundsNeeded: 3, bonusPerBatch: 1090);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(1090));
            Assert.That(so.BatchCount,   Is.EqualTo(1));
            Assert.That(so.Pounds,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(poundsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesPounds()
        {
            var so = CreateSO(poundsNeeded: 5, scatterPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Pounds, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(poundsNeeded: 5, scatterPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Pounds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PoundProgress_Clamped()
        {
            var so = CreateSO(poundsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.PoundProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPestleProcessed_FiresEvent()
        {
            var so    = CreateSO(poundsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePestleSO)
                .GetField("_onPestleProcessed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(poundsNeeded: 2, bonusPerBatch: 1090);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Pounds,            Is.EqualTo(0));
            Assert.That(so.BatchCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBatches_Accumulate()
        {
            var so = CreateSO(poundsNeeded: 2, bonusPerBatch: 1090);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BatchCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2180));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PestleSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PestleSO, Is.Null);
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
            typeof(ZoneControlCapturePestleController)
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
