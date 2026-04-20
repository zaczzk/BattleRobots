using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRelicTests
    {
        private static ZoneControlCaptureRelicSO CreateSO(
            int fragmentsNeeded     = 5,
            int damagePerBot        = 1,
            int bonusPerRestoration = 475)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRelicSO>();
            typeof(ZoneControlCaptureRelicSO)
                .GetField("_fragmentsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, fragmentsNeeded);
            typeof(ZoneControlCaptureRelicSO)
                .GetField("_damagePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, damagePerBot);
            typeof(ZoneControlCaptureRelicSO)
                .GetField("_bonusPerRestoration", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRestoration);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRelicController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRelicController>();
        }

        [Test]
        public void SO_FreshInstance_Fragments_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Fragments, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RestorationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RestorationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFragments()
        {
            var so = CreateSO(fragmentsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Fragments, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_RestoresAtThreshold()
        {
            var so    = CreateSO(fragmentsNeeded: 3, bonusPerRestoration: 475);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(475));
            Assert.That(so.RestorationCount, Is.EqualTo(1));
            Assert.That(so.Fragments,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileGathering()
        {
            var so    = CreateSO(fragmentsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DamagesFragments()
        {
            var so = CreateSO(fragmentsNeeded: 5, damagePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Fragments, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(fragmentsNeeded: 5, damagePerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Fragments, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FragmentProgress_Clamped()
        {
            var so = CreateSO(fragmentsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.FragmentProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnRelicRestored_FiresEvent()
        {
            var so    = CreateSO(fragmentsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRelicSO)
                .GetField("_onRelicRestored", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(fragmentsNeeded: 2, bonusPerRestoration: 475);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Fragments,         Is.EqualTo(0));
            Assert.That(so.RestorationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRestorations_Accumulate()
        {
            var so = CreateSO(fragmentsNeeded: 2, bonusPerRestoration: 475);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RestorationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(950));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RelicSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RelicSO, Is.Null);
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
            typeof(ZoneControlCaptureRelicController)
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
