using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDiodeTests
    {
        private static ZoneControlCaptureDiodeSO CreateSO(
            int junctionsNeeded    = 6,
            int reversePerBot      = 2,
            int bonusPerConduction = 1585)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDiodeSO>();
            typeof(ZoneControlCaptureDiodeSO)
                .GetField("_junctionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, junctionsNeeded);
            typeof(ZoneControlCaptureDiodeSO)
                .GetField("_reversePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, reversePerBot);
            typeof(ZoneControlCaptureDiodeSO)
                .GetField("_bonusPerConduction", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerConduction);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDiodeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDiodeController>();
        }

        [Test]
        public void SO_FreshInstance_Junctions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Junctions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ConductionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConductionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesJunctions()
        {
            var so = CreateSO(junctionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Junctions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_JunctionsAtThreshold()
        {
            var so    = CreateSO(junctionsNeeded: 3, bonusPerConduction: 1585);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(1585));
            Assert.That(so.ConductionCount,  Is.EqualTo(1));
            Assert.That(so.Junctions,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(junctionsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReversesJunctions()
        {
            var so = CreateSO(junctionsNeeded: 6, reversePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Junctions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(junctionsNeeded: 6, reversePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Junctions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_JunctionProgress_Clamped()
        {
            var so = CreateSO(junctionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.JunctionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDiodeConducted_FiresEvent()
        {
            var so    = CreateSO(junctionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDiodeSO)
                .GetField("_onDiodeConducted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(junctionsNeeded: 2, bonusPerConduction: 1585);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Junctions,         Is.EqualTo(0));
            Assert.That(so.ConductionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleConductions_Accumulate()
        {
            var so = CreateSO(junctionsNeeded: 2, bonusPerConduction: 1585);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ConductionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3170));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DiodeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DiodeSO, Is.Null);
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
            typeof(ZoneControlCaptureDiodeController)
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
