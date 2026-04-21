using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCauldronTests
    {
        private static ZoneControlCaptureCauldronSO CreateSO(
            int ingredientsNeeded = 5,
            int dilutePerBot      = 1,
            int bonusPerBrew      = 925)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCauldronSO>();
            typeof(ZoneControlCaptureCauldronSO)
                .GetField("_ingredientsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ingredientsNeeded);
            typeof(ZoneControlCaptureCauldronSO)
                .GetField("_dilutePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dilutePerBot);
            typeof(ZoneControlCaptureCauldronSO)
                .GetField("_bonusPerBrew", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBrew);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCauldronController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCauldronController>();
        }

        [Test]
        public void SO_FreshInstance_Ingredients_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Ingredients, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BrewCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BrewCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesIngredients()
        {
            var so = CreateSO(ingredientsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Ingredients, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IngredientsAtThreshold()
        {
            var so    = CreateSO(ingredientsNeeded: 3, bonusPerBrew: 925);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(925));
            Assert.That(so.BrewCount,   Is.EqualTo(1));
            Assert.That(so.Ingredients, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(ingredientsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DilutesIngredients()
        {
            var so = CreateSO(ingredientsNeeded: 5, dilutePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ingredients, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(ingredientsNeeded: 5, dilutePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ingredients, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IngredientProgress_Clamped()
        {
            var so = CreateSO(ingredientsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.IngredientProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCauldronBrewed_FiresEvent()
        {
            var so    = CreateSO(ingredientsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCauldronSO)
                .GetField("_onCauldronBrewed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(ingredientsNeeded: 2, bonusPerBrew: 925);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Ingredients,       Is.EqualTo(0));
            Assert.That(so.BrewCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBrews_Accumulate()
        {
            var so = CreateSO(ingredientsNeeded: 2, bonusPerBrew: 925);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BrewCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1850));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CauldronSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CauldronSO, Is.Null);
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
            typeof(ZoneControlCaptureCauldronController)
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
