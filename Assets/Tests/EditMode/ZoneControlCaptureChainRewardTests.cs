using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T464: <see cref="ZoneControlCaptureChainRewardSO"/> and
    /// <see cref="ZoneControlCaptureChainRewardController"/>.
    ///
    /// ZoneControlCaptureChainRewardTests (12):
    ///   SO_FreshInstance_CurrentChain_Zero                                           x1
    ///   SO_FreshInstance_ChainCount_Zero                                             x1
    ///   SO_RecordPlayerCapture_IncrementsChain                                       x1
    ///   SO_RecordPlayerCapture_BelowTarget_NoCompletion                              x1
    ///   SO_RecordPlayerCapture_ReachesTarget_CompletesChain                          x1
    ///   SO_RecordPlayerCapture_AfterCompletion_ResetsChain                           x1
    ///   SO_RecordPlayerCapture_AccumulatesTotalBonus                                 x1
    ///   SO_BreakChain_ResetsCurrentChain                                             x1
    ///   SO_ChainProgress_ReflectsRatio                                               x1
    ///   SO_Reset_ClearsAll                                                           x1
    ///   Controller_FreshInstance_ChainRewardSO_Null                                  x1
    ///   Controller_Refresh_NullSO_HidesPanel                                         x1
    /// </summary>
    public sealed class ZoneControlCaptureChainRewardTests
    {
        private static ZoneControlCaptureChainRewardSO CreateSO(int chainTarget = 5, int bonusPerChain = 150)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureChainRewardSO>();
            typeof(ZoneControlCaptureChainRewardSO)
                .GetField("_chainTarget", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chainTarget);
            typeof(ZoneControlCaptureChainRewardSO)
                .GetField("_bonusPerChain", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerChain);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureChainRewardController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureChainRewardController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentChain_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentChain, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ChainCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ChainCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsChain()
        {
            var so = CreateSO(chainTarget: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CurrentChain, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowTarget_NoCompletion()
        {
            var so = CreateSO(chainTarget: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ChainCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachesTarget_CompletesChain()
        {
            var so = CreateSO(chainTarget: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ChainCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AfterCompletion_ResetsChain()
        {
            var so = CreateSO(chainTarget: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentChain, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTotalBonus()
        {
            var so = CreateSO(chainTarget: 3, bonusPerChain: 150);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(150));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BreakChain_ResetsCurrentChain()
        {
            var so = CreateSO(chainTarget: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.BreakChain();
            Assert.That(so.CurrentChain, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ChainProgress_ReflectsRatio()
        {
            var so = CreateSO(chainTarget: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ChainProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(chainTarget: 3, bonusPerChain: 150);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentChain,      Is.EqualTo(0));
            Assert.That(so.ChainCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ChainRewardSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ChainRewardSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureChainRewardController)
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
