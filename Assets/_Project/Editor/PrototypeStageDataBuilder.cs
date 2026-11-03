using System;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Items;
using BackpackAutoBattle.Prototype;
using BackpackAutoBattle.Stages;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BackpackAutoBattle.Editor
{
    /// <summary>Seed demonstration data once. Re-running never replaces authored stage rounds or encounter stats.</summary>
    public static class PrototypeStageDataBuilder
    {
        public const string StagePath = "Assets/_Project/Data/Stages/Stage_01.asset";
        public const string FinalStagePath = "Assets/_Project/Data/Stages/Stage_05.asset";

        [MenuItem("Tools/Backpack AutoBattle/Setup Stage Prototype Data")]
        public static void SetupScene()
        {
            var scene = EditorSceneManager.OpenScene(PrototypeSceneBuilder.ScenePath, OpenSceneMode.Single);
            // Opening a scene can unload newly created, not-yet-referenced SOs. Resolve assets afterwards.
            StageData stage = CreateOrLoad();
            var controller = UnityEngine.Object.FindFirstObjectByType<BackpackPrototypeController>();
            if (controller == null) throw new InvalidOperationException("Prototype controller is missing.");
            var serialized = new SerializedObject(controller);
            serialized.Update();
            // First migration replaces the old always-goblin field; preserve an explicit freeze-test selection.
            if (serialized.FindProperty("_stageData").objectReferenceValue == null)
            {
                serialized.FindProperty("_stageData").objectReferenceValue = stage;
                if (controller.CurrentEnemyEncounter != null && controller.CurrentEnemyEncounter.EncounterId == "goblin")
                    serialized.FindProperty("_debugEnemyOverride").objectReferenceValue = null;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (controller.Stage == null) throw new InvalidOperationException("Stage reference was not assigned to the scene.");
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Stage data ready: 12 rounds per stage, all enemies have fixed 6x6 backpacks.");
        }

        public static StageData CreateOrLoad()
        {
            EnemyEncounterData goblin = PrototypeEnemyDataBuilder.CreateOrLoad();
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Stages")) AssetDatabase.CreateFolder("Assets/_Project/Data", "Stages");
            ItemData club = AssetDatabase.LoadAssetAtPath<ItemData>(PrototypeEnemyDataBuilder.ClubPath);
            ItemData shield = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Project/Data/Items/Item_WoodenShield.asset");
            if (shield == null) throw new InvalidOperationException("The wooden shield ItemData is missing.");
            EnemyEncounterData guarded = Encounter("Encounter_GoblinGuard", "goblin_guard", goblin.Enemy, 50, club, shield);
            EnemyData captainType = PrototypeEnemyDataBuilder.CreateType("EnemyType_GoblinCaptain", "goblin_captain", "고블린 대장");
            EnemyEncounterData captain = Encounter("Encounter_GoblinCaptain", "goblin_captain", captainType, 85, club, shield);
            StageData stage = null;
            for (int number = 1; number <= 5; number++)
            {
                string title = number == 1 ? "1스테이지 · 고블린 시험장" : $"{number}스테이지 · 고블린 전선";
                StageData created = CreateStage($"Assets/_Project/Data/Stages/Stage_{number:00}.asset", $"stage_{number:00}",
                    title, goblin, guarded, captain, number);
                created.ValidateOrThrow();
                if (number == 1) stage = created;
            }
            AssetDatabase.SaveAssets();
            stage.ValidateOrThrow();
            return stage;
        }

        private static EnemyEncounterData Encounter(string fileName, string id, EnemyData enemy, int health,
            ItemData club, ItemData shield, float? damage = null)
        {
            string path = $"Assets/_Project/Data/Enemies/{fileName}.asset";
            var result = AssetDatabase.LoadAssetAtPath<EnemyEncounterData>(path);
            if (result != null) return result;
            result = ScriptableObject.CreateInstance<EnemyEncounterData>();
            var data = new SerializedObject(result);
            data.FindProperty("_encounterId").stringValue = id;
            data.FindProperty("_enemy").objectReferenceValue = enemy;
            data.FindProperty("_maximumHealth").intValue = health;
            SerializedProperty items = data.FindProperty("_items");
            items.arraySize = shield != null ? 2 : 1;
            Placement(items.GetArrayElementAtIndex(0), "club", club, new Vector2Int(1, 0));
            if (damage.HasValue) Stat(items.GetArrayElementAtIndex(0).FindPropertyRelative("_stats"), "_attackDamage", damage.Value);
            if (shield != null) Placement(items.GetArrayElementAtIndex(1), "shield", shield, new Vector2Int(3, 1));
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        private static void Placement(SerializedProperty property, string slotId, ItemData item, Vector2Int origin)
        {
            property.FindPropertyRelative("_slotId").stringValue = slotId;
            property.FindPropertyRelative("_item").objectReferenceValue = item;
            property.FindPropertyRelative("_origin").vector2IntValue = origin;
            property.FindPropertyRelative("_rotation").enumValueIndex = 0;
        }

        private static StageData CreateStage(string path, string id, string displayName, EnemyEncounterData basic,
            EnemyEncounterData guarded, EnemyEncounterData boss, int stageNumber)
        {
            var result = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (result != null) return result;
            result = ScriptableObject.CreateInstance<StageData>();
            var data = new SerializedObject(result);
            data.FindProperty("_stageId").stringValue = id;
            data.FindProperty("_displayName").stringValue = displayName;
            SerializedProperty rounds = data.FindProperty("_rounds");
            rounds.arraySize = StageData.RoundsPerStage;
            float scale = 1f + (stageNumber - 1) * .4f;
            for (int index = 0; index < rounds.arraySize; index++)
            {
                bool isBoss = index == rounds.arraySize - 1;
                SerializedProperty round = rounds.GetArrayElementAtIndex(index);
                round.FindPropertyRelative("_encounter").objectReferenceValue = isBoss ? boss : index >= 4 && index < 8 ? guarded : basic;
                round.FindPropertyRelative("_isBoss").boolValue = isBoss;
                SerializedProperty modifier = round.FindPropertyRelative("_overrides");
                modifier.FindPropertyRelative("_overrideMaximumHealth").boolValue = true;
                modifier.FindPropertyRelative("_maximumHealth").intValue = Mathf.RoundToInt((30 + index * 5) * scale);
                SerializedProperty items = modifier.FindPropertyRelative("_items");
                items.arraySize = 1;
                SerializedProperty item = items.GetArrayElementAtIndex(0);
                item.FindPropertyRelative("_slotId").stringValue = "club";
                Stat(item.FindPropertyRelative("_stats"), "_attackDamage", Mathf.RoundToInt((5 + (index + 1) / 2) * scale));
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        private static void Stat(SerializedProperty stats, string field, float value)
        {
            SerializedProperty modifier = stats.FindPropertyRelative(field);
            modifier.FindPropertyRelative("_enabled").boolValue = true;
            modifier.FindPropertyRelative("_value").floatValue = value;
        }
    }
}
