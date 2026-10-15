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
            var result = new StatusEffectData[2];
            for (int index = 0; index < result.Length; index++)
            {
                string path = $"{folder}/Status_{(StatusEffectKind)index}.asset";
                result[index] = AssetDatabase.LoadAssetAtPath<StatusEffectData>(path);
                if (result[index] != null) continue;
                result[index] = ScriptableObject.CreateInstance<StatusEffectData>();
                var data = new SerializedObject(result[index]);
                data.FindProperty("_kind").enumValueIndex = index;
                data.FindProperty("_displayName").stringValue = index == 0 ? "신속" : "빙결";
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(result[index], path);
            }
            AssetDatabase.SaveAssets();
            return result;
        }
    }
}
