using BackpackAutoBattle.Items;
using UnityEditor;

namespace BackpackAutoBattle.Editor
{
    [CustomEditor(typeof(FusionRecipeData))]
    public sealed class FusionRecipeDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var recipe = (FusionRecipeData)target;
            if (recipe.ValidationError != null) EditorGUILayout.HelpBox(recipe.ValidationError, MessageType.Error);
            EditorGUILayout.HelpBox("중심 아이템 1개 + 추가 재료 목록의 수량을 소비합니다. 모든 추가 재료는 중심 아이템에 상하좌우로 인접해야 합니다. 결과는 중심 아이템의 위치/회전을 사용합니다.\n씬의 Backpack Prototype > Fusion Recipes에 등록하세요. 목록 위쪽 레시피가 우선됩니다.", MessageType.Info);
            if (recipe.ResultItem != null && !recipe.ResultItem.ExcludeFromShop)
                EditorGUILayout.HelpBox("합성 전용 아이템이라면 결과 ItemData의 Exclude From Shop을 켜세요.", MessageType.Warning);
        }
    }
}
