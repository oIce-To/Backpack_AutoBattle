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

        public static EnemyData CreateOrLoad()
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
                stats.FindPropertyRelative("_cooldownSeconds").floatValue = 2f;
                stats.FindPropertyRelative("_accuracyPercent").floatValue = 100f;
                stats.FindPropertyRelative("_criticalChancePercent").floatValue = 0f;
                stats.FindPropertyRelative("_criticalDamagePercent").floatValue = 200f;
                PrototypeItemDataBuilder.ConfigureActivation(item, 2f, ItemEffectKind.Attack);
                item.FindProperty("_tags").intValue = (int)ItemTags.Melee;
                item.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(club);
            }

            EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyPath);
            if (enemy == null)
            {
                enemy = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(enemy, EnemyPath);
                var data = new SerializedObject(enemy);
                data.FindProperty("_enemyId").stringValue = "goblin";
                data.FindProperty("_displayName").stringValue = "고블린";
                data.FindProperty("_maximumHealth").intValue = 30;
                data.FindProperty("_backpackWidth").intValue = 3;
                data.FindProperty("_backpackHeight").intValue = 3;
                SerializedProperty items = data.FindProperty("_items");
                items.arraySize = 1;
                SerializedProperty placement = items.GetArrayElementAtIndex(0);
                placement.FindPropertyRelative("_item").objectReferenceValue = club;
                placement.FindPropertyRelative("_origin").vector2IntValue = new Vector2Int(1, 0);
                placement.FindPropertyRelative("_rotation").enumValueIndex = 0;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(enemy);
            }

            AssetDatabase.SaveAssets();
            return enemy;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
