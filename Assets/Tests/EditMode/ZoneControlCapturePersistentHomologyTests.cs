using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePersistentHomologyTests
    {
        private static ZoneControlCapturePersistentHomologySO CreateSO(
            int barsNeeded      = 6,
            int killPerBot      = 2,
            int bonusPerPersist = 3760)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePersistentHomologySO>();
            typeof(ZoneControlCapturePersistentHomologySO)
                .GetField("_barsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, barsNeeded);
            typeof(ZoneControlCapturePersistentHomologySO)
                .GetField("_killPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, killPerBot);
            typeof(ZoneControlCapturePersistentHomologySO)
                .GetField("_bonusPerPersist", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPersist);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePersistentHomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePersistentHomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Bars_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Bars, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PersistCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PersistCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBars()
        {
            var so = CreateSO(barsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Bars, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(barsNeeded: 3, bonusPerPersist: 3760);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(3760));
            Assert.That(so.PersistCount, Is.EqualTo(1));
            Assert.That(so.Bars,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(barsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_KillsBars()
        {
            var so = CreateSO(barsNeeded: 6, killPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bars, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(barsNeeded: 6, killPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bars, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BarProgress_Clamped()
        {
            var so = CreateSO(barsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.BarProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPersistentHomologyPersisted_FiresEvent()
        {
            var so    = CreateSO(barsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePersistentHomologySO)
                .GetField("_onPersistentHomologyPersisted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(barsNeeded: 2, bonusPerPersist: 3760);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Bars,             Is.EqualTo(0));
            Assert.That(so.PersistCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultiplePersistences_Accumulate()
        {
            var so = CreateSO(barsNeeded: 2, bonusPerPersist: 3760);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.PersistCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7520));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PersistentHomologySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PersistentHomologySO, Is.Null);
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
            typeof(ZoneControlCapturePersistentHomologyController)
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
