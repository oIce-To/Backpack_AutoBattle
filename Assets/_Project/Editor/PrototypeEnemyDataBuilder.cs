using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Items;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Editor
{
    public static class PrototypeEnemyDataBuilder
    {
        public const string EnemyPath = "Assets/_Project/Data/Enemies/Enemy_Goblin.asset";
        public const string ClubPath = "Assets/_Project/Data/Enemies/Items/Item_GoblinClub.asset";

        public static EnemyEncounterData CreateOrLoad()
        {
            EnsureFolder("Assets/_Project/Data", "Enemies");
            EnsureFolder("Assets/_Project/Data/Enemies", "Items");
            ItemData club = AssetDatabase.LoadAssetAtPath<ItemData>(ClubPath);
            if (club == null)
            {
                club = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(club, ClubPath);
                var item = new SerializedObject(club);
                item.FindProperty("_itemId").stringValue = "goblin_club";
                item.FindProperty("_displayName").stringValue = "고블린 몽둥이";
                item.FindProperty("_description").stringValue = "고블린이 사용하는 적 전용 몽둥이.";
                item.FindProperty("_category").enumValueIndex = (int)ItemCategory.Weapon;
                item.FindProperty("_purchasePrice").intValue = 0;
                item.FindProperty("_displayColor").colorValue = new Color(0.63f, 0.38f, 0.16f, 1f);
                SerializedProperty shape = item.FindProperty("_occupiedCells");
                shape.arraySize = 2;
                shape.GetArrayElementAtIndex(0).vector2IntValue = Vector2Int.zero;
                shape.GetArrayElementAtIndex(1).vector2IntValue = new Vector2Int(0, 1);
                SerializedProperty stats = item.FindProperty("_weaponStats");
                stats.FindPropertyRelative("_attackDamage").floatValue = 5f;
                PrototypeItemDataBuilder.ConfigureScaling(item.FindProperty("_attackScaling"), StatReference.Attack, .5f);
                stats.FindPropertyRelative("_cooldownSeconds").floatValue = 2f;
                stats.FindPropertyRelative("_accuracyPercent").floatValue = 100f;
                stats.FindPropertyRelative("_criticalChancePercent").floatValue = 0f;
                stats.FindPropertyRelative("_criticalDamagePercent").floatValue = 200f;
                PrototypeItemDataBuilder.ConfigureActivation(item, 2f, ItemEffectKind.Attack);
                item.FindProperty("_tags").intValue = (int)ItemTags.Melee;
                item.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(club);
            }

            EnemyData species = CreateType("EnemyType_Goblin", "goblin", "고블린");
            EnemyEncounterData enemy = AssetDatabase.LoadAssetAtPath<EnemyEncounterData>(EnemyPath);
            if (enemy == null)
            {
                enemy = ScriptableObject.CreateInstance<EnemyEncounterData>();
                AssetDatabase.CreateAsset(enemy, EnemyPath);
                var data = new SerializedObject(enemy);
                data.FindProperty("_encounterId").stringValue = "goblin";
                data.FindProperty("_maximumHealth").intValue = 30;
                SerializedProperty items = data.FindProperty("_items");
                items.arraySize = 1;
                SerializedProperty placement = items.GetArrayElementAtIndex(0);
                placement.FindPropertyRelative("_item").objectReferenceValue = club;
                placement.FindPropertyRelative("_origin").vector2IntValue = new Vector2Int(1, 0);
                placement.FindPropertyRelative("_rotation").enumValueIndex = 0;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(enemy);
            }

            // Upgrade existing prototype encounters without changing health, items, positions or item stats.
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyEncounterData", new[] { "Assets/_Project/Data/Enemies" }))
            {
                var existing = AssetDatabase.LoadAssetAtPath<EnemyEncounterData>(AssetDatabase.GUIDToAssetPath(guid));
                var data = new SerializedObject(existing);
                if (existing.Enemy == null) data.FindProperty("_enemy").objectReferenceValue = species;
                SerializedProperty placements = data.FindProperty("_items");
                for (int index = 0; index < placements.arraySize; index++)
                {
                    SerializedProperty placement = placements.GetArrayElementAtIndex(index);
                    SerializedProperty slotId = placement.FindPropertyRelative("_slotId");
                    if (string.IsNullOrWhiteSpace(slotId.stringValue))
                        slotId.stringValue = index == 0 ? "club" : $"slot_{index}";
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return enemy;
        }

        public static EnemyData CreateType(string fileName, string id, string displayName)
        {
            string path = $"Assets/_Project/Data/Enemies/{fileName}.asset";
            EnemyData result = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (result != null) return result;
            result = ScriptableObject.CreateInstance<EnemyData>();
            var data = new SerializedObject(result);
            data.FindProperty("_enemyId").stringValue = id;
            data.FindProperty("_displayName").stringValue = displayName;
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
