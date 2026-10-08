using System;
using System.Linq;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackpackAutoBattle.Editor
{
    public static class PrototypeSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Prototype_Backpack.unity";

        [MenuItem("Tools/Backpack AutoBattle/Create Backpack Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ItemData[] itemCatalog = PrototypeItemDataBuilder.CreateOrUpdateItems();

            var prototypeObject = new GameObject("Backpack Prototype");
            BackpackPrototypeController controller = prototypeObject.AddComponent<BackpackPrototypeController>();
            AssignItemCatalog(controller, itemCatalog);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            Selection.activeGameObject = prototypeObject;
            Debug.Log($"Created backpack prototype scene at {ScenePath}");
        }

        [MenuItem("Tools/Backpack AutoBattle/Refresh Prototype Item Data")]
        public static void RefreshPrototypeContent()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ItemData[] itemCatalog = PrototypeItemDataBuilder.CreateOrUpdateItems();
            BackpackPrototypeController controller = UnityEngine.Object.FindFirstObjectByType<BackpackPrototypeController>();

            if (controller == null)
            {
                var prototypeObject = new GameObject("Backpack Prototype");
                controller = prototypeObject.AddComponent<BackpackPrototypeController>();
            }

            AssignItemCatalog(controller, itemCatalog);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddSceneToBuildSettings();
            Debug.Log($"Refreshed {itemCatalog.Length} ItemData assets and updated {ScenePath}");
        }

        private static void AssignItemCatalog(BackpackPrototypeController controller, ItemData[] itemCatalog)
        {
            if (itemCatalog == null || itemCatalog.Any(itemData => itemData == null))
            {
                throw new InvalidOperationException("The prototype ItemData catalog contains a missing asset reference.");
            }

            var serializedController = new SerializedObject(controller);
            serializedController.Update();
            SerializedProperty catalogProperty = serializedController.FindProperty("_itemCatalog");
            catalogProperty.arraySize = itemCatalog.Length;

            for (int index = 0; index < itemCatalog.Length; index++)
            {
                catalogProperty.GetArrayElementAtIndex(index).objectReferenceValue = itemCatalog[index];
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            var scenes = existingScenes
                .Where(scene => !string.Equals(scene.path, ScenePath))
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
