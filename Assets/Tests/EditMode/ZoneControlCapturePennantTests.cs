using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePennantTests
    {
        private static ZoneControlCapturePennantSO CreateSO(
            int stripesNeeded  = 5,
            int tearPerBot     = 1,
            int bonusPerPennant = 775)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePennantSO>();
            typeof(ZoneControlCapturePennantSO)
                .GetField("_stripesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stripesNeeded);
            typeof(ZoneControlCapturePennantSO)
                .GetField("_tearPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, tearPerBot);
            typeof(ZoneControlCapturePennantSO)
                .GetField("_bonusPerPennant", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPennant);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePennantController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePennantController>();
        }

        [Test]
        public void SO_FreshInstance_Stripes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Stripes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PennantCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PennantCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStripes()
        {
            var so = CreateSO(stripesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Stripes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_RaisesAtThreshold()
        {
            var so    = CreateSO(stripesNeeded: 3, bonusPerPennant: 775);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(775));
            Assert.That(so.PennantCount,  Is.EqualTo(1));
            Assert.That(so.Stripes,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(stripesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_TearsStripes()
        {
            var so = CreateSO(stripesNeeded: 5, tearPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stripes, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(stripesNeeded: 5, tearPerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stripes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StripeProgress_Clamped()
        {
            var so = CreateSO(stripesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.StripeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPennantRaised_FiresEvent()
        {
            var so    = CreateSO(stripesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePennantSO)
                .GetField("_onPennantRaised", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(stripesNeeded: 2, bonusPerPennant: 775);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Stripes,           Is.EqualTo(0));
            Assert.That(so.PennantCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultiplePennants_Accumulate()
        {
            var so = CreateSO(stripesNeeded: 2, bonusPerPennant: 775);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.PennantCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1550));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PennantSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PennantSO, Is.Null);
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
            typeof(ZoneControlCapturePennantController)
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
