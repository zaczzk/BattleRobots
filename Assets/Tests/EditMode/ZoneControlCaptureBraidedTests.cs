using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBraidedTests
    {
        private static ZoneControlCaptureBraidedSO CreateSO(
            int braidsNeeded  = 5,
            int unbraidPerBot = 1,
            int bonusPerBraid = 3115)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBraidedSO>();
            typeof(ZoneControlCaptureBraidedSO)
                .GetField("_braidsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, braidsNeeded);
            typeof(ZoneControlCaptureBraidedSO)
                .GetField("_unbraidPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, unbraidPerBot);
            typeof(ZoneControlCaptureBraidedSO)
                .GetField("_bonusPerBraid", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBraid);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBraidedController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBraidedController>();
        }

        [Test]
        public void SO_FreshInstance_Braids_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Braids, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BraidCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BraidCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBraids()
        {
            var so = CreateSO(braidsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Braids, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(braidsNeeded: 3, bonusPerBraid: 3115);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(3115));
            Assert.That(so.BraidCount,  Is.EqualTo(1));
            Assert.That(so.Braids,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(braidsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesBraids()
        {
            var so = CreateSO(braidsNeeded: 5, unbraidPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Braids, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(braidsNeeded: 5, unbraidPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Braids, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BraidProgress_Clamped()
        {
            var so = CreateSO(braidsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.BraidProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBraided_FiresEvent()
        {
            var so    = CreateSO(braidsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBraidedSO)
                .GetField("_onBraided", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(braidsNeeded: 2, bonusPerBraid: 3115);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Braids,            Is.EqualTo(0));
            Assert.That(so.BraidCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBraidings_Accumulate()
        {
            var so = CreateSO(braidsNeeded: 2, bonusPerBraid: 3115);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BraidCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6230));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BraidedSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BraidedSO, Is.Null);
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
            typeof(ZoneControlCaptureBraidedController)
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
