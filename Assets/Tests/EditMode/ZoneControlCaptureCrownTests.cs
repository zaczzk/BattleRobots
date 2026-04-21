using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCrownTests
    {
        private static ZoneControlCaptureCrownSO CreateSO(
            int jewelsNeeded       = 7,
            int removePerBot       = 2,
            int bonusPerCoronation = 670)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCrownSO>();
            typeof(ZoneControlCaptureCrownSO)
                .GetField("_jewelsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, jewelsNeeded);
            typeof(ZoneControlCaptureCrownSO)
                .GetField("_removePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, removePerBot);
            typeof(ZoneControlCaptureCrownSO)
                .GetField("_bonusPerCoronation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCoronation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCrownController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCrownController>();
        }

        [Test]
        public void SO_FreshInstance_Jewels_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Jewels, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CoronationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CoronationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesJewels()
        {
            var so = CreateSO(jewelsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Jewels, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CoronatesAtThreshold()
        {
            var so    = CreateSO(jewelsNeeded: 3, bonusPerCoronation: 670);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(670));
            Assert.That(so.CoronationCount,   Is.EqualTo(1));
            Assert.That(so.Jewels,            Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(jewelsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesJewels()
        {
            var so = CreateSO(jewelsNeeded: 7, removePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Jewels, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(jewelsNeeded: 7, removePerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Jewels, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_JewelProgress_Clamped()
        {
            var so = CreateSO(jewelsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.JewelProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCrownCoronated_FiresEvent()
        {
            var so    = CreateSO(jewelsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCrownSO)
                .GetField("_onCrownCoronated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(jewelsNeeded: 2, bonusPerCoronation: 670);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Jewels,            Is.EqualTo(0));
            Assert.That(so.CoronationCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCoronations_Accumulate()
        {
            var so = CreateSO(jewelsNeeded: 2, bonusPerCoronation: 670);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CoronationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1340));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CrownSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CrownSO, Is.Null);
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
            typeof(ZoneControlCaptureCrownController)
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
