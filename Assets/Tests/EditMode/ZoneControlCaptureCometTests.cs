using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCometTests
    {
        private static ZoneControlCaptureCometSO CreateSO(
            int tailsNeeded    = 4,
            int dissipatePerBot = 1,
            int bonusPerBlaze  = 850)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCometSO>();
            typeof(ZoneControlCaptureCometSO)
                .GetField("_tailsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, tailsNeeded);
            typeof(ZoneControlCaptureCometSO)
                .GetField("_dissipatePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dissipatePerBot);
            typeof(ZoneControlCaptureCometSO)
                .GetField("_bonusPerBlaze", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBlaze);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCometController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCometController>();
        }

        [Test]
        public void SO_FreshInstance_Tails_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Tails, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BlazeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BlazeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTails()
        {
            var so = CreateSO(tailsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.Tails, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BlazesAtThreshold()
        {
            var so    = CreateSO(tailsNeeded: 3, bonusPerBlaze: 850);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(850));
            Assert.That(so.BlazeCount, Is.EqualTo(1));
            Assert.That(so.Tails,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(tailsNeeded: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DissipatesTails()
        {
            var so = CreateSO(tailsNeeded: 4, dissipatePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Tails, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(tailsNeeded: 4, dissipatePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Tails, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TailProgress_Clamped()
        {
            var so = CreateSO(tailsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.TailProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCometBlazed_FiresEvent()
        {
            var so    = CreateSO(tailsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCometSO)
                .GetField("_onCometBlazed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(tailsNeeded: 2, bonusPerBlaze: 850);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Tails,             Is.EqualTo(0));
            Assert.That(so.BlazeCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBlazes_Accumulate()
        {
            var so = CreateSO(tailsNeeded: 2, bonusPerBlaze: 850);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BlazeCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1700));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CometSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CometSO, Is.Null);
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
            typeof(ZoneControlCaptureCometController)
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
