using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBifunctorTests
    {
        private static ZoneControlCaptureBifunctorSO CreateSO(
            int pairsNeeded  = 5,
            int splitPerBot  = 1,
            int bonusPerBimap = 2395)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBifunctorSO>();
            typeof(ZoneControlCaptureBifunctorSO)
                .GetField("_pairsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, pairsNeeded);
            typeof(ZoneControlCaptureBifunctorSO)
                .GetField("_splitPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, splitPerBot);
            typeof(ZoneControlCaptureBifunctorSO)
                .GetField("_bonusPerBimap", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBimap);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBifunctorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBifunctorController>();
        }

        [Test]
        public void SO_FreshInstance_Pairs_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Pairs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BimapCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BimapCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPairs()
        {
            var so = CreateSO(pairsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Pairs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(pairsNeeded: 3, bonusPerBimap: 2395);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(2395));
            Assert.That(so.BimapCount,  Is.EqualTo(1));
            Assert.That(so.Pairs,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(pairsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesPairs()
        {
            var so = CreateSO(pairsNeeded: 5, splitPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Pairs, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(pairsNeeded: 5, splitPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Pairs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PairProgress_Clamped()
        {
            var so = CreateSO(pairsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.PairProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBifunctorBimapped_FiresEvent()
        {
            var so    = CreateSO(pairsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBifunctorSO)
                .GetField("_onBifunctorBimapped", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(pairsNeeded: 2, bonusPerBimap: 2395);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Pairs,             Is.EqualTo(0));
            Assert.That(so.BimapCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBimaps_Accumulate()
        {
            var so = CreateSO(pairsNeeded: 2, bonusPerBimap: 2395);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BimapCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4790));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BifunctorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BifunctorSO, Is.Null);
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
            typeof(ZoneControlCaptureBifunctorController)
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
