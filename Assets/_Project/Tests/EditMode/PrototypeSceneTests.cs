using BackpackAutoBattle.Prototype;
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
            Assert.That(controllers[0].ItemCatalog.Count, Is.EqualTo(8));
            Assert.That(controllers[0].ItemCatalog, Is.All.Not.Null);
        }
    }
}
