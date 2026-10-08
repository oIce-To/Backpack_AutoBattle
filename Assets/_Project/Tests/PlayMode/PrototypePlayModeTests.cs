using System.Collections;
using System.Linq;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypePlayModeTests
    {
        [UnityTest]
        public IEnumerator PrototypeScene_CreatesInteractiveBackpackInterface()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return loadOperation;
            yield return null;

            BackpackPrototypeController controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            BackpackItemView[] items = Object.FindObjectsByType<BackpackItemView>(FindObjectsSortMode.None);
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            PrototypeShopSlotView[] shopSlots = Object.FindObjectsByType<PrototypeShopSlotView>(FindObjectsSortMode.None);

            Assert.That(controller, Is.Not.Null);
            Assert.That(canvas, Is.Not.Null);
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(items, Has.Length.EqualTo(3));
            Assert.That(items.All(item => item.RectTransform.pivot == new Vector2(0f, 1f)), Is.True);
            Assert.That(controller.ItemCatalog.Count, Is.EqualTo(8));
            Assert.That(shopSlots, Has.Length.EqualTo(4));
            Assert.That(controller.CurrentShopItems.Count, Is.EqualTo(4));
            Assert.That(controller.CurrentShopItems.Distinct().Count(), Is.EqualTo(4));

            controller.RefreshShop();
            Assert.That(controller.CurrentShopItems.Count, Is.EqualTo(4));
            Assert.That(controller.CurrentShopItems.Distinct().Count(), Is.EqualTo(4));

            int gridCellCount = controller
                .GetComponentsInChildren<Transform>(true)
                .Count(child => child.name.StartsWith("Grid Cell "));
            Assert.That(gridCellCount, Is.EqualTo(36));
        }
    }
}
