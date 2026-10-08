using System.Collections.Generic;
using BackpackAutoBattle.Items;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Editor
{
    public static class PrototypeItemDataBuilder
    {
        public const string ItemFolder = "Assets/_Project/Data/Items";

        public static ItemData[] CreateOrUpdateItems()
        {
            EnsureFolder("Assets/_Project", "Data");
            EnsureFolder("Assets/_Project/Data", "Items");

            ItemDefinition[] definitions =
            {
                new(
                    "Item_TrainingBlade.asset",
                    "training_blade",
                    "훈련용 검",
                    "길쭉한 기본 무기입니다.",
                    ItemCategory.Weapon,
                    4,
                    new Color(0.18f, 0.48f, 0.80f, 1f),
                    true,
                    new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }),
                new(
                    "Item_WoodenShield.asset",
                    "wooden_shield",
                    "나무 방패",
                    "넓은 공간을 차지하는 기본 방어구입니다.",
                    ItemCategory.Armor,
                    5,
                    new Color(0.70f, 0.43f, 0.18f, 1f),
                    true,
                    new[]
                    {
                        new Vector2Int(0, 0), new Vector2Int(1, 0),
                        new Vector2Int(0, 1), new Vector2Int(1, 1)
                    }),
                new(
                    "Item_SwiftCharm.asset",
                    "swift_charm",
                    "신속의 부적",
                    "L자 형태의 신속 계열 장신구입니다.",
                    ItemCategory.Accessory,
                    6,
                    new Color(0.56f, 0.28f, 0.76f, 1f),
                    true,
                    new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }),
                new(
                    "Item_IronDagger.asset",
                    "iron_dagger",
                    "철 단검",
                    "작고 값싼 근접 무기입니다.",
                    ItemCategory.Weapon,
                    3,
                    new Color(0.24f, 0.66f, 0.68f, 1f),
                    true,
                    new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) }),
                new(
                    "Item_HealingPotion.asset",
                    "healing_potion",
                    "회복 물약",
                    "한 칸만 사용하는 회복 소모품입니다.",
                    ItemCategory.Consumable,
                    3,
                    new Color(0.78f, 0.20f, 0.28f, 1f),
                    false,
                    new[] { Vector2Int.zero }),
                new(
                    "Item_ThrowingAxes.asset",
                    "throwing_axes",
                    "쌍도끼",
                    "가로 두 칸을 사용하는 공격 무기입니다.",
                    ItemCategory.Weapon,
                    5,
                    new Color(0.83f, 0.55f, 0.18f, 1f),
                    true,
                    new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }),
                new(
                    "Item_FireStaff.asset",
                    "fire_staff",
                    "화염 지팡이",
                    "독특한 형태의 화상 계열 무기입니다.",
                    ItemCategory.Weapon,
                    8,
                    new Color(0.90f, 0.30f, 0.12f, 1f),
                    true,
                    new[]
                    {
                        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                        new Vector2Int(1, 1)
                    }),
                new(
                    "Item_PoisonVial.asset",
                    "poison_vial",
                    "맹독 약병",
                    "중독 빌드를 위한 실험용 재료입니다.",
                    ItemCategory.Material,
                    6,
                    new Color(0.32f, 0.72f, 0.24f, 1f),
                    true,
                    new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) })
            };

            var items = new List<ItemData>(definitions.Length);
            foreach (ItemDefinition definition in definitions)
            {
                items.Add(CreateOrUpdateItem(definition));
            }

            AssetDatabase.SaveAssets();

            items.Clear();
            foreach (ItemDefinition definition in definitions)
            {
                string assetPath = $"{ItemFolder}/{definition.FileName}";
                AssetDatabase.ImportAsset(
                    assetPath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }

            foreach (ItemDefinition definition in definitions)
            {
                string assetPath = $"{ItemFolder}/{definition.FileName}";
                ItemData loadedItem = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

                if (loadedItem == null)
                {
                    throw new System.InvalidOperationException($"Could not reload ItemData at {assetPath}.");
                }

                items.Add(loadedItem);
            }

            return items.ToArray();
        }

        private static ItemData CreateOrUpdateItem(ItemDefinition definition)
        {
            string assetPath = $"{ItemFolder}/{definition.FileName}";
            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

            if (itemData == null)
            {
                itemData = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(itemData, assetPath);
            }

            var serializedItem = new SerializedObject(itemData);
            serializedItem.FindProperty("_itemId").stringValue = definition.ItemId;
            serializedItem.FindProperty("_displayName").stringValue = definition.DisplayName;
            serializedItem.FindProperty("_description").stringValue = definition.Description;
            serializedItem.FindProperty("_category").enumValueIndex = (int)definition.Category;
            serializedItem.FindProperty("_purchasePrice").intValue = definition.PurchasePrice;
            serializedItem.FindProperty("_displayColor").colorValue = definition.DisplayColor;
            serializedItem.FindProperty("_canRotate").boolValue = definition.CanRotate;

            SerializedProperty occupiedCells = serializedItem.FindProperty("_occupiedCells");
            occupiedCells.arraySize = definition.OccupiedCells.Length;
            for (int index = 0; index < definition.OccupiedCells.Length; index++)
            {
                occupiedCells.GetArrayElementAtIndex(index).vector2IntValue = definition.OccupiedCells[index];
            }

            serializedItem.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(itemData);
            return itemData;
        }

        private static void EnsureFolder(string parentFolder, string folderName)
        {
            string path = $"{parentFolder}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        private sealed class ItemDefinition
        {
            public ItemDefinition(
                string fileName,
                string itemId,
                string displayName,
                string description,
                ItemCategory category,
                int purchasePrice,
                Color displayColor,
                bool canRotate,
                Vector2Int[] occupiedCells)
            {
                FileName = fileName;
                ItemId = itemId;
                DisplayName = displayName;
                Description = description;
                Category = category;
                PurchasePrice = purchasePrice;
                DisplayColor = displayColor;
                CanRotate = canRotate;
                OccupiedCells = occupiedCells;
            }

            public string FileName { get; }
            public string ItemId { get; }
            public string DisplayName { get; }
            public string Description { get; }
            public ItemCategory Category { get; }
            public int PurchasePrice { get; }
            public Color DisplayColor { get; }
            public bool CanRotate { get; }
            public Vector2Int[] OccupiedCells { get; }
        }
    }
}
