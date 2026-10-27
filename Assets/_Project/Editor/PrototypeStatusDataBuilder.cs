using System.Linq;
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
            // Enrage is derived from combat time and deliberately has no editable item-status SO.
            var kinds = System.Enum.GetValues(typeof(StatusEffectKind)).Cast<StatusEffectKind>()
                .Where(kind => kind != StatusEffectKind.Enrage).ToArray();
            var result = new StatusEffectData[kinds.Length];
            for (int index = 0; index < result.Length; index++)
            {
                StatusEffectKind kind = kinds[index];
                string path = $"{folder}/Status_{kind}.asset";
                result[index] = AssetDatabase.LoadAssetAtPath<StatusEffectData>(path);
                if (result[index] != null) continue;
                result[index] = ScriptableObject.CreateInstance<StatusEffectData>();
                var data = new SerializedObject(result[index]);
                data.FindProperty("_kind").enumValueIndex = (int)kind;
                data.FindProperty("_displayName").stringValue = StatusEffectRules.DisplayName(kind);
                if (kind is StatusEffectKind.Burn or StatusEffectKind.Poison or StatusEffectKind.Thorns or StatusEffectKind.Lifesteal)
                {
                    PrototypeItemDataBuilder.ConfigureScaling(data.FindProperty("_damageScaling"), StatReference.Attack,
                        kind == StatusEffectKind.Burn ? .12f : .1f);
                    data.FindProperty("_stacksLostPerTick").intValue = kind == StatusEffectKind.Burn ? 1 : 0;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(result[index], path);
            }
            AssetDatabase.SaveAssets();
            return result;
        }
    }
}
