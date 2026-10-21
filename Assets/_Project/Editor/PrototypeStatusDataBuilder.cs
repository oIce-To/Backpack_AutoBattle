using BackpackAutoBattle.Items;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Editor
{
    public static class PrototypeStatusDataBuilder
    {
        public static StatusEffectData[] CreateOrLoad()
        {
            const string folder = "Assets/_Project/Data/StatusEffects";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Project/Data", "StatusEffects");
            var result = new StatusEffectData[System.Enum.GetValues(typeof(StatusEffectKind)).Length];
            for (int index = 0; index < result.Length; index++)
            {
                string path = $"{folder}/Status_{(StatusEffectKind)index}.asset";
                result[index] = AssetDatabase.LoadAssetAtPath<StatusEffectData>(path);
                if (result[index] != null) continue;
                result[index] = ScriptableObject.CreateInstance<StatusEffectData>();
                var data = new SerializedObject(result[index]);
                data.FindProperty("_kind").enumValueIndex = index;
                data.FindProperty("_displayName").stringValue = StatusEffectRules.DisplayName((StatusEffectKind)index);
                if ((StatusEffectKind)index is StatusEffectKind.Burn or StatusEffectKind.Poison)
                {
                    PrototypeItemDataBuilder.ConfigureScaling(data.FindProperty("_damageScaling"), StatReference.Attack,
                        (StatusEffectKind)index == StatusEffectKind.Burn ? .12f : .1f);
                    data.FindProperty("_stacksLostPerTick").intValue = (StatusEffectKind)index == StatusEffectKind.Burn ? 1 : 0;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(result[index], path);
            }
            AssetDatabase.SaveAssets();
            return result;
        }
    }
}
