using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T459: <see cref="ZoneControlMatchPeakSO"/> and
    /// <see cref="ZoneControlMatchPeakController"/>.
    ///
    /// ZoneControlMatchPeakTests (12):
    ///   SO_FreshInstance_CurrentStreak_Zero                                        x1
    ///   SO_FreshInstance_PeakStreak_Zero                                           x1
    ///   SO_RecordPlayerCapture_IncrementsCurrentStreak                             x1
    ///   SO_RecordBotCapture_ResetsCurrentStreak                                    x1
    ///   SO_RecordPlayerCapture_UpdatesPeak                                         x1
    ///   SO_RecordPlayerCapture_DoesNotDecreasePeak                                 x1
    ///   SO_RecordBotCapture_DoesNotResetPeak                                       x1
    ///   SO_FirstCapture_PeakBecomesOne                                             x1
    ///   SO_Reset_ClearsAll                                                         x1
    ///   Controller_FreshInstance_PeakSO_Null                                       x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                  x1
    ///   Controller_Refresh_NullSO_HidesPanel                                       x1
    /// </summary>
    public sealed class ZoneControlMatchPeakTests
    {
        private static ZoneControlMatchPeakSO CreateSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchPeakSO>();
            so.Reset();
            return so;
        }

        private static ZoneControlMatchPeakController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchPeakController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentStreak_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PeakStreak_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PeakStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsCurrentStreak()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentStreak, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ResetsCurrentStreak()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_UpdatesPeak()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PeakStreak, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_DoesNotDecreasePeak()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PeakStreak, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DoesNotResetPeak()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PeakStreak, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstCapture_PeakBecomesOne()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.PeakStreak, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentStreak, Is.EqualTo(0));
            Assert.That(so.PeakStreak,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PeakSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PeakSO, Is.Null);
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
            typeof(ZoneControlMatchPeakController)
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
