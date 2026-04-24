using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCategoryStackTests
    {
        private static ZoneControlCaptureCategoryStackSO CreateSO(
            int patchesNeeded  = 6,
            int obstructPerBot = 2,
            int bonusPerDescend = 3625)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCategoryStackSO>();
            typeof(ZoneControlCaptureCategoryStackSO)
                .GetField("_patchesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, patchesNeeded);
            typeof(ZoneControlCaptureCategoryStackSO)
                .GetField("_obstructPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, obstructPerBot);
            typeof(ZoneControlCaptureCategoryStackSO)
                .GetField("_bonusPerDescend", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDescend);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCategoryStackController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCategoryStackController>();
        }

        [Test]
        public void SO_FreshInstance_Patches_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Patches, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DescendCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DescendCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPatches()
        {
            var so = CreateSO(patchesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Patches, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(patchesNeeded: 3, bonusPerDescend: 3625);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(3625));
            Assert.That(so.DescendCount,  Is.EqualTo(1));
            Assert.That(so.Patches,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(patchesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ObstructsPatches()
        {
            var so = CreateSO(patchesNeeded: 6, obstructPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Patches, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(patchesNeeded: 6, obstructPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Patches, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PatchProgress_Clamped()
        {
            var so = CreateSO(patchesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PatchProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCategoryStackDescended_FiresEvent()
        {
            var so    = CreateSO(patchesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCategoryStackSO)
                .GetField("_onCategoryStackDescended", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(patchesNeeded: 2, bonusPerDescend: 3625);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Patches,           Is.EqualTo(0));
            Assert.That(so.DescendCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDescents_Accumulate()
        {
            var so = CreateSO(patchesNeeded: 2, bonusPerDescend: 3625);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DescendCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7250));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CategoryStackSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CategoryStackSO, Is.Null);
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
            typeof(ZoneControlCaptureCategoryStackController)
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
