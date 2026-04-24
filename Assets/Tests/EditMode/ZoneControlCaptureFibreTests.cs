using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFibreTests
    {
        private static ZoneControlCaptureFibreSO CreateSO(
            int fibresNeeded       = 6,
            int trivializePerBot   = 1,
            int bonusPerProjection = 3490)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFibreSO>();
            typeof(ZoneControlCaptureFibreSO)
                .GetField("_fibresNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, fibresNeeded);
            typeof(ZoneControlCaptureFibreSO)
                .GetField("_trivializePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, trivializePerBot);
            typeof(ZoneControlCaptureFibreSO)
                .GetField("_bonusPerProjection", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerProjection);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFibreController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFibreController>();
        }

        [Test]
        public void SO_FreshInstance_Fibres_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Fibres, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ProjectionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ProjectionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFibres()
        {
            var so = CreateSO(fibresNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Fibres, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(fibresNeeded: 3, bonusPerProjection: 3490);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(3490));
            Assert.That(so.ProjectionCount,  Is.EqualTo(1));
            Assert.That(so.Fibres,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(fibresNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_TriviializesFibres()
        {
            var so = CreateSO(fibresNeeded: 6, trivializePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Fibres, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(fibresNeeded: 6, trivializePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Fibres, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FibreProgress_Clamped()
        {
            var so = CreateSO(fibresNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.FibreProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFibreProjected_FiresEvent()
        {
            var so    = CreateSO(fibresNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFibreSO)
                .GetField("_onFibreProjected", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(fibresNeeded: 2, bonusPerProjection: 3490);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Fibres,            Is.EqualTo(0));
            Assert.That(so.ProjectionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleProjections_Accumulate()
        {
            var so = CreateSO(fibresNeeded: 2, bonusPerProjection: 3490);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ProjectionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6980));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FibreSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FibreSO, Is.Null);
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
            typeof(ZoneControlCaptureFibreController)
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
