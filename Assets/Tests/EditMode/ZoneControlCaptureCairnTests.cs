using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCairnTests
    {
        private static ZoneControlCaptureCairnSO CreateSO(
            int stonesNeeded    = 6,
            int knockdownPerBot = 2,
            int bonusPerCairn   = 470)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCairnSO>();
            typeof(ZoneControlCaptureCairnSO)
                .GetField("_stonesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stonesNeeded);
            typeof(ZoneControlCaptureCairnSO)
                .GetField("_knockdownPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, knockdownPerBot);
            typeof(ZoneControlCaptureCairnSO)
                .GetField("_bonusPerCairn", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCairn);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCairnController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCairnController>();
        }

        [Test]
        public void SO_FreshInstance_Stones_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Stones, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CairnCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CairnCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStones()
        {
            var so = CreateSO(stonesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Stones, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletesAtThreshold()
        {
            var so    = CreateSO(stonesNeeded: 3, bonusPerCairn: 470);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(470));
            Assert.That(so.CairnCount, Is.EqualTo(1));
            Assert.That(so.Stones,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileStacking()
        {
            var so    = CreateSO(stonesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_KnocksDownStones()
        {
            var so = CreateSO(stonesNeeded: 6, knockdownPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stones, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(stonesNeeded: 6, knockdownPerBot: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stones, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StoneProgress_Clamped()
        {
            var so = CreateSO(stonesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.StoneProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCairnComplete_FiresEvent()
        {
            var so    = CreateSO(stonesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCairnSO)
                .GetField("_onCairnComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(stonesNeeded: 2, bonusPerCairn: 470);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Stones,            Is.EqualTo(0));
            Assert.That(so.CairnCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCairns_Accumulate()
        {
            var so = CreateSO(stonesNeeded: 2, bonusPerCairn: 470);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CairnCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(940));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CairnSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CairnSO, Is.Null);
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
            typeof(ZoneControlCaptureCairnController)
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
