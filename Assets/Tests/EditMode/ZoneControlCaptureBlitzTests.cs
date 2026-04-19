using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBlitzTests
    {
        private static ZoneControlCaptureBlitzSO CreateSO(int blitzTarget = 5, int bonusPerBlitz = 300)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBlitzSO>();
            typeof(ZoneControlCaptureBlitzSO)
                .GetField("_blitzTarget", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, blitzTarget);
            typeof(ZoneControlCaptureBlitzSO)
                .GetField("_bonusPerBlitz", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBlitz);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBlitzController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBlitzController>();
        }

        [Test]
        public void SO_FreshInstance_BlitzCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BlitzCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CurrentStreak_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsStreak()
        {
            var so = CreateSO(blitzTarget: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentStreak, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachingTarget_ReturnsBonus()
        {
            var so = CreateSO(blitzTarget: 3, bonusPerBlitz: 300);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachingTarget_IncrementsBlitzCount()
        {
            var so = CreateSO(blitzTarget: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.BlitzCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ResetsStreakAfterBlitz()
        {
            var so = CreateSO(blitzTarget: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresEvent()
        {
            var so    = CreateSO(blitzTarget: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBlitzSO)
                .GetField("_onBlitz", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_ResetsStreak()
        {
            var so = CreateSO(blitzTarget: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BlitzProgress_Computed()
        {
            var so = CreateSO(blitzTarget: 4);
            Assert.That(so.BlitzProgress, Is.EqualTo(0f).Within(0.001f));
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.BlitzProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(blitzTarget: 2, bonusPerBlitz: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentStreak,     Is.EqualTo(0));
            Assert.That(so.BlitzCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BlitzSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BlitzSO, Is.Null);
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
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureBlitzController)
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
