using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureETaleCohomologyTests
    {
        private static ZoneControlCaptureETaleCohomologySO CreateSO(
            int stalksNeeded           = 6,
            int breakPerBot            = 2,
            int bonusPerSheafification = 3820)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureETaleCohomologySO>();
            typeof(ZoneControlCaptureETaleCohomologySO)
                .GetField("_stalksNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stalksNeeded);
            typeof(ZoneControlCaptureETaleCohomologySO)
                .GetField("_breakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, breakPerBot);
            typeof(ZoneControlCaptureETaleCohomologySO)
                .GetField("_bonusPerSheafification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSheafification);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureETaleCohomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureETaleCohomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Stalks_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Stalks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SheafifyCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SheafifyCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStalks()
        {
            var so = CreateSO(stalksNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Stalks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(stalksNeeded: 3, bonusPerSheafification: 3820);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(3820));
            Assert.That(so.SheafifyCount,   Is.EqualTo(1));
            Assert.That(so.Stalks,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(stalksNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_CollapesStalks()
        {
            var so = CreateSO(stalksNeeded: 6, breakPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stalks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(stalksNeeded: 6, breakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stalks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StalkProgress_Clamped()
        {
            var so = CreateSO(stalksNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.StalkProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnETaleSheafified_FiresEvent()
        {
            var so    = CreateSO(stalksNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureETaleCohomologySO)
                .GetField("_onETaleSheafified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(stalksNeeded: 2, bonusPerSheafification: 3820);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Stalks,            Is.EqualTo(0));
            Assert.That(so.SheafifyCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSheafifications_Accumulate()
        {
            var so = CreateSO(stalksNeeded: 2, bonusPerSheafification: 3820);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SheafifyCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7640));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EtaleSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EtaleSO, Is.Null);
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
            typeof(ZoneControlCaptureETaleCohomologyController)
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
