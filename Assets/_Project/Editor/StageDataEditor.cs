using System;
using BackpackAutoBattle.Enemies;
using BackpackAutoBattle.Stages;
using UnityEditor;
using UnityEngine;

namespace BackpackAutoBattle.Editor
{
    [CustomEditor(typeof(StageData))]
    public sealed class StageDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_stageId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_displayName"));
            EditorGUILayout.HelpBox("각 스테이지는 12라운드입니다. 적 가방은 모두 6×6으로 고정됩니다.\nOverrides에서 체크한 수치만 변경되며 원본 적/아이템은 수정되지 않습니다.", MessageType.Info);
            SerializedProperty rounds = serializedObject.FindProperty("_rounds");
            if (rounds.arraySize != StageData.RoundsPerStage)
            {
                EditorGUILayout.PropertyField(rounds, true);
            }
            else
            {
                for (int index = 0; index < rounds.arraySize; index++)
                {
                    string summary = "적 미지정";
                    try
                    {
                        var snapshot = ((StageData)target).GetRound(index + 1).CreateEnemySnapshot();
                        summary = $"{snapshot.DisplayName} · HP {snapshot.MaximumHealth:0.#}";
                    }
                    catch (InvalidOperationException) { }
                    EditorGUILayout.PropertyField(rounds.GetArrayElementAtIndex(index), new GUIContent($"ROUND {index + 1} · {summary}"), true);
                }
            }
            serializedObject.ApplyModifiedProperties();
            foreach (string error in ((StageData)target).GetValidationErrors()) EditorGUILayout.HelpBox(error, MessageType.Error);
        }
    }

    [CustomEditor(typeof(EnemyEncounterData))]
    public sealed class EnemyEncounterDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("가방: 6×6 고정. 좌표 (0, 0)은 왼쪽 위이며 X는 오른쪽, Y는 아래쪽입니다.\nSlot Id는 이 구성 안에서 고유해야 합니다. 라운드 보정은 이 ID로 장비를 찾습니다.", MessageType.Info);
            try { ((EnemyEncounterData)target).CreateCombatantSnapshot(); }
            catch (InvalidOperationException exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Error); }
        }
    }

    [CustomPropertyDrawer(typeof(EnemyStatOverride))]
    public sealed class EnemyStatOverrideDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect content = EditorGUI.PrefixLabel(position, label);
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            SerializedProperty enabled = property.FindPropertyRelative("_enabled");
            enabled.boolValue = EditorGUI.Toggle(new Rect(content.x, content.y, 20f, content.height), enabled.boolValue);
            using (new EditorGUI.DisabledScope(!enabled.boolValue))
                EditorGUI.PropertyField(new Rect(content.x + 24f, content.y, Mathf.Max(0f, content.width - 24f), content.height), property.FindPropertyRelative("_value"), GUIContent.none);
            EditorGUI.indentLevel = oldIndent;
            EditorGUI.EndProperty();
        }
    }
}
