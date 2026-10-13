using BackpackAutoBattle.Shop;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Editor
{
    [CustomEditor(typeof(ShopRarityTableData))]
    public sealed class ShopRarityTableDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(
                "각 라운드의 일반/고급/희귀/영웅 등장 가중치입니다. 합계 100을 권장합니다. 전설은 상점에서 등장하지 않습니다.",
                MessageType.Info);

            SerializedProperty rounds = serializedObject.FindProperty("_rounds");
            for (int index = 0; index < rounds.arraySize; index++)
            {
                SerializedProperty weights = rounds.GetArrayElementAtIndex(index);
                SerializedProperty common = weights.FindPropertyRelative("_common");
                SerializedProperty uncommon = weights.FindPropertyRelative("_uncommon");
                SerializedProperty rare = weights.FindPropertyRelative("_rare");
                SerializedProperty epic = weights.FindPropertyRelative("_epic");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Round {index + 1}", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(common, new GUIContent("일반"));
                EditorGUILayout.PropertyField(uncommon, new GUIContent("고급"));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(rare, new GUIContent("희귀"));
                EditorGUILayout.PropertyField(epic, new GUIContent("영웅"));
                EditorGUILayout.EndHorizontal();

                float total = common.floatValue + uncommon.floatValue + rare.floatValue + epic.floatValue;
                Color previousColor = GUI.color;
                GUI.color = Mathf.Approximately(total, 100f) ? new Color(0.72f, 1f, 0.72f) : new Color(1f, 0.76f, 0.58f);
                EditorGUILayout.LabelField($"합계: {total:0.##}%");
                GUI.color = previousColor;
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
