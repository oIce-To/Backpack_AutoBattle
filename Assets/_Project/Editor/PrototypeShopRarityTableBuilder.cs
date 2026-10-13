using BackpackAutoBattle.Shop;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Editor
{
    public static class PrototypeShopRarityTableBuilder
    {
        public const string AssetPath = "Assets/_Project/Data/Shop/ShopRarityTable_Default.asset";

        public static ShopRarityTableData CreateOrLoad()
        {
            EnsureFolder("Assets/_Project/Data", "Shop");
            ShopRarityTableData table = AssetDatabase.LoadAssetAtPath<ShopRarityTableData>(AssetPath);
            if (table != null)
            {
                return table;
            }

            table = ScriptableObject.CreateInstance<ShopRarityTableData>();
            AssetDatabase.CreateAsset(table, AssetPath);

            float[,] defaults =
            {
                { 70f, 20f, 10f, 0f },
                { 65f, 22f, 12f, 1f },
                { 58f, 25f, 15f, 2f },
                { 52f, 27f, 18f, 3f },
                { 45f, 30f, 20f, 5f },
                { 38f, 30f, 25f, 7f },
                { 32f, 29f, 29f, 10f },
                { 25f, 28f, 34f, 13f },
                { 20f, 27f, 37f, 16f },
                { 15f, 25f, 40f, 20f },
                { 12f, 23f, 42f, 23f },
                { 10f, 20f, 43f, 27f }
            };

            var serializedTable = new SerializedObject(table);
            SerializedProperty rounds = serializedTable.FindProperty("_rounds");
            rounds.arraySize = ShopRarityTableData.MaximumRound;
            for (int round = 0; round < ShopRarityTableData.MaximumRound; round++)
            {
                SerializedProperty weights = rounds.GetArrayElementAtIndex(round);
                weights.FindPropertyRelative("_common").floatValue = defaults[round, 0];
                weights.FindPropertyRelative("_uncommon").floatValue = defaults[round, 1];
                weights.FindPropertyRelative("_rare").floatValue = defaults[round, 2];
                weights.FindPropertyRelative("_epic").floatValue = defaults[round, 3];
            }

            serializedTable.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<ShopRarityTableData>(AssetPath);
        }

        private static void EnsureFolder(string parentFolder, string folderName)
        {
            string path = $"{parentFolder}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }
    }
}
