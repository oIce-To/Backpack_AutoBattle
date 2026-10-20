using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Shop;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypeSceneTests
    {
        private const string ScenePath = "Assets/Scenes/Prototype_Backpack.unity";

        [Test]
        public void PrototypeScene_ExistsAndContainsOneController()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Assert.That(sceneAsset, Is.Not.Null, $"Prototype scene was not found at {ScenePath}.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BackpackPrototypeController[] controllers = Object.FindObjectsByType<BackpackPrototypeController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert.That(scene.isLoaded, Is.True);
            Assert.That(controllers, Has.Length.EqualTo(1));
            Assert.That(controllers[0].ItemCatalog.Count, Is.EqualTo(13));
            Assert.That(controllers[0].ItemCatalog, Is.All.Not.Null);
            Assert.That(controllers[0].Stage, Is.Not.Null);
            Assert.That(controllers[0].StageRoundCount, Is.EqualTo(12));
            Assert.That(controllers[0].CurrentEnemyEncounter.DisplayName, Is.EqualTo("고블린"));
            Assert.That(controllers[0].ItemCatalog, Has.No.Member(controllers[0].CurrentEnemyEncounter.Items[0].Item));
            Assert.That(controllers[0].ShopRarityTable, Is.Not.Null);
            Assert.That(controllers[0].ShopRarityTable.Rounds, Has.Count.EqualTo(ShopRarityTableData.MaximumRound));

            RoundShopRarityWeights round1 = controllers[0].ShopRarityTable.GetWeights(1);
            Assert.That(round1.Common, Is.EqualTo(70f));
            Assert.That(round1.Uncommon, Is.EqualTo(20f));
            Assert.That(round1.Rare, Is.EqualTo(10f));
            Assert.That(round1.Epic, Is.Zero);
            Assert.That(round1.Total, Is.EqualTo(100f));

            RoundShopRarityWeights round10 = controllers[0].ShopRarityTable.GetWeights(10);
            Assert.That(round10.Common, Is.EqualTo(15f));
            Assert.That(round10.Uncommon, Is.EqualTo(25f));
            Assert.That(round10.Rare, Is.EqualTo(40f));
            Assert.That(round10.Epic, Is.EqualTo(20f));
            Assert.That(round10.Total, Is.EqualTo(100f));
        }
    }
}
