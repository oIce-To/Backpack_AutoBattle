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
                new("Item_ThornArmor.asset", "thorn_armor", "가시 갑옷", "", ItemCategory.Armor, ItemRarity.Rare,
                    5, new Color(.20f, .45f, .27f), true,
                    new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }),
                new("Item_VampiricCharm.asset", "vampiric_charm", "흡혈의 부적", "", ItemCategory.Accessory, ItemRarity.Rare,
                    5, new Color(.72f, .18f, .26f), false, new[] { Vector2Int.zero }),
                new("Item_PrecisionCharm.asset", "precision_charm", "정밀의 부적", "", ItemCategory.Accessory, ItemRarity.Uncommon,
                    4, new Color(.35f, .68f, .5f), false, new[] { Vector2Int.zero }),
                new("Item_FocusCharm.asset", "focus_charm", "집중의 부적", "", ItemCategory.Accessory, ItemRarity.Uncommon,
                    4, new Color(.35f, .68f, .5f), false, new[] { Vector2Int.zero }),
                new("Item_BlindingDust.asset", "blinding_dust", "실명의 가루", "", ItemCategory.Accessory, ItemRarity.Uncommon,
                    4, new Color(.35f, .36f, .5f), false, new[] { Vector2Int.zero }),
                new("Item_AgilityCharm.asset", "agility_charm", "민첩의 부적", "", ItemCategory.Accessory, ItemRarity.Uncommon,
                    4, new Color(.35f, .68f, .8f), false, new[] { Vector2Int.zero }),
                new("Item_Whetstone.asset", "whetstone", "숫돌", "무기를 손질하는 작은 숫돌입니다.",
                    ItemCategory.Accessory, ItemRarity.Common, 2, new Color(0.52f, 0.55f, 0.58f), true,
                    new[] { Vector2Int.zero }),
                new(
                    "Item_TrainingBlade.asset",
                    "training_blade",
                    "훈련용 검",
                    "길쭉한 기본 무기입니다.",
                    ItemCategory.Weapon,
                    ItemRarity.Common,
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
                    ItemRarity.Uncommon,
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
                    ItemRarity.Rare,
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
                    ItemRarity.Uncommon,
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
                    ItemRarity.Common,
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
                    ItemRarity.Rare,
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
                    ItemRarity.Epic,
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
                    ItemRarity.Rare,
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

            if (itemData != null)
            {
                return itemData;
            }

            itemData = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(itemData, assetPath);

            var serializedItem = new SerializedObject(itemData);
            serializedItem.FindProperty("_itemId").stringValue = definition.ItemId;
            serializedItem.FindProperty("_displayName").stringValue = definition.DisplayName;
            serializedItem.FindProperty("_description").stringValue = definition.Description;
            serializedItem.FindProperty("_category").enumValueIndex = (int)definition.Category;
            serializedItem.FindProperty("_rarity").enumValueIndex = (int)definition.Rarity;
            serializedItem.FindProperty("_purchasePrice").intValue = definition.PurchasePrice;
            serializedItem.FindProperty("_displayColor").colorValue = definition.DisplayColor;
            serializedItem.FindProperty("_canRotate").boolValue = definition.CanRotate;

            if (definition.Category == ItemCategory.Weapon)
            {
                WeaponStats stats = GetDefaultWeaponStats(definition.ItemId);
                SerializedProperty weaponStats = serializedItem.FindProperty("_weaponStats");
                weaponStats.FindPropertyRelative("_attackDamage").floatValue = stats.AttackDamage;
                weaponStats.FindPropertyRelative("_cooldownSeconds").floatValue = stats.CooldownSeconds;
                weaponStats.FindPropertyRelative("_accuracyPercent").floatValue = stats.AccuracyPercent;
                weaponStats.FindPropertyRelative("_criticalChancePercent").floatValue = stats.CriticalChancePercent;
                weaponStats.FindPropertyRelative("_criticalDamagePercent").floatValue = stats.CriticalDamagePercent;
                ConfigureScaling(serializedItem.FindProperty("_attackScaling"), StatReference.Attack, stats.AttackDamage / 10f);
                ConfigureActivation(serializedItem, stats.CooldownSeconds, ItemEffectKind.Attack);
                if (definition.ItemId == "fire_staff") ConfigurePeriodicStatus(serializedItem, StatusEffectKind.Burn, 2);
                serializedItem.FindProperty("_tags").intValue = (int)(definition.ItemId == "fire_staff" ? ItemTags.Magic
                    : definition.ItemId == "throwing_axes" ? ItemTags.Ranged : ItemTags.Melee);
            }
            else if (definition.ItemId == "wooden_shield")
            {
                ConfigureActivation(serializedItem, 2f, ItemEffectKind.GainArmor);
                ConfigureScaling(serializedItem.FindProperty("_activation").FindPropertyRelative("_effects").GetArrayElementAtIndex(0)
                    .FindPropertyRelative("_amountScaling"), StatReference.MaximumHealth, .05f);
            }
            else if (definition.ItemId == "poison_vial")
            {
                ConfigureActivation(serializedItem, 3f, ItemEffectKind.ApplyStatus);
                serializedItem.FindProperty("_activation").FindPropertyRelative("_effects").arraySize = 0;
                ConfigurePeriodicStatus(serializedItem, StatusEffectKind.Poison, 2);
            }
            else if (definition.ItemId is "swift_charm" or "precision_charm" or "focus_charm" or "blinding_dust" or "agility_charm" or "thorn_armor" or "vampiric_charm")
            {
                var grant = definition.ItemId switch
                {
                    "precision_charm" => (StatusEffectKind.Precision, 20, StatusTarget.Self),
                    "focus_charm" => (StatusEffectKind.Focus, 20, StatusTarget.Self),
                    "blinding_dust" => (StatusEffectKind.Blindness, 20, StatusTarget.Opponent),
                    "agility_charm" => (StatusEffectKind.Agility, 30, StatusTarget.Self),
                    "thorn_armor" => (StatusEffectKind.Thorns, 3, StatusTarget.Self),
                    "vampiric_charm" => (StatusEffectKind.Lifesteal, 3, StatusTarget.Self),
                    _ => (StatusEffectKind.Haste, 10, StatusTarget.Self)
                };
                SerializedProperty statuses = serializedItem.FindProperty("_battleStartStatuses");
                statuses.arraySize = 1;
                SerializedProperty status = statuses.GetArrayElementAtIndex(0);
                status.FindPropertyRelative("_kind").enumValueIndex = (int)grant.Item1;
                status.FindPropertyRelative("_target").enumValueIndex = (int)grant.Item3;
                status.FindPropertyRelative("_stacks").intValue = grant.Item2;
            }
            else if (definition.ItemId == "whetstone")
            {
                SerializedProperty effects = serializedItem.FindProperty("_placementEffects");
                effects.arraySize = 1;
                SerializedProperty effect = effects.GetArrayElementAtIndex(0);
                SerializedProperty cells = effect.FindPropertyRelative("_cells");
                cells.arraySize = 2;
                cells.GetArrayElementAtIndex(0).vector2IntValue = Vector2Int.left;
                cells.GetArrayElementAtIndex(1).vector2IntValue = Vector2Int.right;
                SerializedProperty filter = effect.FindPropertyRelative("_targetFilter");
                filter.FindPropertyRelative("_filterByCategory").boolValue = true;
                filter.FindPropertyRelative("_category").enumValueIndex = (int)ItemCategory.Weapon;
                filter.FindPropertyRelative("_requiredTags").intValue = (int)ItemTags.Melee;
                effect.FindPropertyRelative("_attackDamageBonus").floatValue = 1f;
                ConfigureScaling(effect.FindPropertyRelative("_attackScaling"), StatReference.Attack, .1f);
            }

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

        public static void ConfigureActivation(SerializedObject item, float seconds, ItemEffectKind kind)
        {
            SerializedProperty activation = item.FindProperty("_activation");
            activation.FindPropertyRelative("_enabled").boolValue = true;
            activation.FindPropertyRelative("_cooldownSeconds").floatValue = seconds;
            SerializedProperty effects = activation.FindPropertyRelative("_effects");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).FindPropertyRelative("_kind").enumValueIndex = (int)kind;
            effects.GetArrayElementAtIndex(0).FindPropertyRelative("_amount").floatValue = 1f;
        }

        public static void ConfigureScaling(SerializedProperty scaling, StatReference stat, float coefficient)
        {
            scaling.FindPropertyRelative("_stat").enumValueIndex = (int)stat;
            scaling.FindPropertyRelative("_coefficient").floatValue = coefficient;
        }

        private static void ConfigurePeriodicStatus(SerializedObject item, StatusEffectKind kind, int stacks)
        {
            var effects = item.FindProperty("_activation").FindPropertyRelative("_effects");
            int index = effects.arraySize; effects.arraySize++;
            var effect = effects.GetArrayElementAtIndex(index);
            effect.FindPropertyRelative("_kind").enumValueIndex = (int)ItemEffectKind.ApplyStatus;
            effect.FindPropertyRelative("_trigger").enumValueIndex = (int)(kind == StatusEffectKind.Burn ? ItemEffectTrigger.OnHit : ItemEffectTrigger.Activation);
            var status = effect.FindPropertyRelative("_status");
            status.FindPropertyRelative("_kind").enumValueIndex = (int)kind;
            status.FindPropertyRelative("_target").enumValueIndex = (int)StatusTarget.Opponent;
            status.FindPropertyRelative("_stacks").intValue = stacks;
        }

        private static void EnsureFolder(string parentFolder, string folderName)
        {
            string path = $"{parentFolder}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
        }

        private static WeaponStats GetDefaultWeaponStats(string itemId)
        {
            return itemId switch
            {
                "training_blade" => new WeaponStats(5f, 2f, 90f, 10f, 200f),
                "iron_dagger" => new WeaponStats(2f, 1f, 95f, 15f, 180f),
                "throwing_axes" => new WeaponStats(8f, 3f, 85f, 20f, 200f),
                "fire_staff" => new WeaponStats(12f, 3.5f, 90f, 10f, 220f),
                _ => new WeaponStats()
            };
        }

        private sealed class ItemDefinition
        {
            public ItemDefinition(
                string fileName,
                string itemId,
                string displayName,
                string description,
                ItemCategory category,
                ItemRarity rarity,
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
                Rarity = rarity;
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
            public ItemRarity Rarity { get; }
            public int PurchasePrice { get; }
            public Color DisplayColor { get; }
            public bool CanRotate { get; }
            public Vector2Int[] OccupiedCells { get; }
        }
    }
}
