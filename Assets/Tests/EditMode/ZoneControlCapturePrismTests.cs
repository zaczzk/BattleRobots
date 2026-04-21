using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePrismTests
    {
        private static ZoneControlCapturePrismSO CreateSO(
            int fragmentsNeeded    = 5,
            int splitPerBot        = 1,
            int bonusPerRefraction = 865)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePrismSO>();
            typeof(ZoneControlCapturePrismSO)
                .GetField("_fragmentsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, fragmentsNeeded);
            typeof(ZoneControlCapturePrismSO)
                .GetField("_splitPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, splitPerBot);
            typeof(ZoneControlCapturePrismSO)
                .GetField("_bonusPerRefraction", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRefraction);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePrismController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePrismController>();
        }

        [Test]
        public void SO_FreshInstance_Fragments_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Fragments, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RefractionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RefractionCount, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_RefractsAtThreshold()
        {
            var so    = CreateSO(fragmentsNeeded: 3, bonusPerRefraction: 865);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(865));
            Assert.That(so.RefractionCount,  Is.EqualTo(1));
            Assert.That(so.Fragments,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(fragmentsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_SplitsFragments()
        {
            var so = CreateSO(fragmentsNeeded: 5, splitPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Fragments, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(fragmentsNeeded: 5, splitPerBot: 10);
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
        public void SO_OnPrismRefracted_FiresEvent()
        {
            var so    = CreateSO(fragmentsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePrismSO)
                .GetField("_onPrismRefracted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(fragmentsNeeded: 2, bonusPerRefraction: 865);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Fragments,         Is.EqualTo(0));
            Assert.That(so.RefractionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRefractions_Accumulate()
        {
            var so = CreateSO(fragmentsNeeded: 2, bonusPerRefraction: 865);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RefractionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1730));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PrismSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PrismSO, Is.Null);
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
            typeof(ZoneControlCapturePrismController)
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
