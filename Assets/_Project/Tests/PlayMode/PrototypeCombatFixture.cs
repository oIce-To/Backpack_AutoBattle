using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackpackAutoBattle.Tests.Prototype
{
    // Tests for shop interactions, training-kit effects and fusion use a fixed loadout, independent of starter equipment.
    internal static class PrototypeCombatFixture
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        internal static IEnumerator LoadWithTrainingKit()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            UseTrainingKit(Object.FindFirstObjectByType<BackpackPrototypeController>(), CharacterStatsSnapshot.Default);
            yield return null;
        }

        internal static void UseTrainingKit(BackpackPrototypeController controller, CharacterStatsSnapshot stats)
        {
            var type = typeof(BackpackPrototypeController);
            var views = (Dictionary<string, BackpackItemView>)type.GetField("_itemViews", PrivateInstance).GetValue(controller);
            foreach (var view in views.Values)
            {
                view.gameObject.SetActive(false);
                Object.Destroy(view.gameObject);
            }
            views.Clear();
            int size = BackpackPrototypeController.GridSize;
            type.GetField("_grid", PrivateInstance).SetValue(controller, new BackpackGrid(size, size));
            var runField = type.GetField("_run", PrivateInstance);
            var run = (PrototypeRunState)runField.GetValue(controller);
            runField.SetValue(controller, new PrototypeRunState(run.Gold, run.StageId, stats,
                run.PlayerName, run.StartingLives, run.StageName, run.ClearExperience));
            Assert.That(PlaceItem(controller, "training_blade", "starter_training_blade", new Vector2Int(0, 0)), Is.True);
            Assert.That(PlaceItem(controller, "wooden_shield", "starter_wooden_shield", new Vector2Int(1, 0)), Is.True);
            Assert.That(PlaceItem(controller, "swift_charm", "starter_swift_charm", new Vector2Int(3, 3)), Is.True);
        }

        internal static bool PlaceItem(BackpackPrototypeController controller, string itemId, string instanceId, Vector2Int origin)
        {
            var method = typeof(BackpackPrototypeController).GetMethod("TryPlaceItem", PrivateInstance);
            var item = controller.ItemCatalog.Single(data => data.ItemId == itemId);
            return (bool)method.Invoke(controller, new object[] { item, instanceId, origin });
        }
    }
}
