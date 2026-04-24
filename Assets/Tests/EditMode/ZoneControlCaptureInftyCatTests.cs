using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureInftyCatTests
    {
        private static ZoneControlCaptureInftyCatSO CreateSO(
            int homotopiesNeeded = 5,
            int coherencePerBot  = 1,
            int bonusPerCompose  = 3565)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureInftyCatSO>();
            typeof(ZoneControlCaptureInftyCatSO)
                .GetField("_homotopiesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, homotopiesNeeded);
            typeof(ZoneControlCaptureInftyCatSO)
                .GetField("_coherencePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, coherencePerBot);
            typeof(ZoneControlCaptureInftyCatSO)
                .GetField("_bonusPerCompose", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCompose);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureInftyCatController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureInftyCatController>();
        }

        [Test]
        public void SO_FreshInstance_Homotopies_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Homotopies, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ComposeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ComposeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesHomotopies()
        {
            var so = CreateSO(homotopiesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Homotopies, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(homotopiesNeeded: 3, bonusPerCompose: 3565);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(3565));
            Assert.That(so.ComposeCount, Is.EqualTo(1));
            Assert.That(so.Homotopies,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(homotopiesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReducesHomotopies()
        {
            var so = CreateSO(homotopiesNeeded: 5, coherencePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Homotopies, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(homotopiesNeeded: 5, coherencePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Homotopies, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_HomotopyProgress_Clamped()
        {
            var so = CreateSO(homotopiesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.HomotopyProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnInftyCatComposed_FiresEvent()
        {
            var so    = CreateSO(homotopiesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureInftyCatSO)
                .GetField("_onInftyCatComposed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(homotopiesNeeded: 2, bonusPerCompose: 3565);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Homotopies,        Is.EqualTo(0));
            Assert.That(so.ComposeCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCompositions_Accumulate()
        {
            var so = CreateSO(homotopiesNeeded: 2, bonusPerCompose: 3565);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ComposeCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7130));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_InftyCatSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.InftyCatSO, Is.Null);
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
            typeof(ZoneControlCaptureInftyCatController)
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
