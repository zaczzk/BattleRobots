using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLanglandsCorrespondenceTests
    {
        private static ZoneControlCaptureLanglandsCorrespondenceSO CreateSO(
            int matchingPairsNeeded         = 6,
            int lFunctionObstructionsPerBot = 2,
            int bonusPerCorrespondence      = 4405)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLanglandsCorrespondenceSO>();
            typeof(ZoneControlCaptureLanglandsCorrespondenceSO)
                .GetField("_matchingPairsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, matchingPairsNeeded);
            typeof(ZoneControlCaptureLanglandsCorrespondenceSO)
                .GetField("_lFunctionObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, lFunctionObstructionsPerBot);
            typeof(ZoneControlCaptureLanglandsCorrespondenceSO)
                .GetField("_bonusPerCorrespondence", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCorrespondence);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLanglandsCorrespondenceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLanglandsCorrespondenceController>();
        }

        [Test]
        public void SO_FreshInstance_MatchingPairs_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MatchingPairs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CorrespondenceCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CorrespondenceCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesMatchingPairs()
        {
            var so = CreateSO(matchingPairsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.MatchingPairs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(matchingPairsNeeded: 3, bonusPerCorrespondence: 4405);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                   Is.EqualTo(4405));
            Assert.That(so.CorrespondenceCount,  Is.EqualTo(1));
            Assert.That(so.MatchingPairs,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(matchingPairsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesObstructions()
        {
            var so = CreateSO(matchingPairsNeeded: 6, lFunctionObstructionsPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.MatchingPairs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(matchingPairsNeeded: 6, lFunctionObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.MatchingPairs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MatchingPairProgress_Clamped()
        {
            var so = CreateSO(matchingPairsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.MatchingPairProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLanglandsCorrespondenceEstablished_FiresEvent()
        {
            var so    = CreateSO(matchingPairsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLanglandsCorrespondenceSO)
                .GetField("_onLanglandsCorrespondenceEstablished", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(matchingPairsNeeded: 2, bonusPerCorrespondence: 4405);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.MatchingPairs,       Is.EqualTo(0));
            Assert.That(so.CorrespondenceCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCorrespondences_Accumulate()
        {
            var so = CreateSO(matchingPairsNeeded: 2, bonusPerCorrespondence: 4405);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CorrespondenceCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(8810));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LanglandsCorrespondenceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LanglandsCorrespondenceSO, Is.Null);
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
            typeof(ZoneControlCaptureLanglandsCorrespondenceController)
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
