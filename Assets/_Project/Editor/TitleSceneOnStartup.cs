using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackpackAutoBattle.Editor
{
    internal static class TitleSceneOnStartup
    {
        private const string SessionKey = "BackpackAutoBattle.TitleSceneOnStartup.Handled";

        [InitializeOnLoadMethod]
        private static void Schedule()
        {
            if (!Application.isBatchMode && !SessionState.GetBool(SessionKey, false))
                EditorApplication.delayCall += OpenTitleScene;
        }

        private static void OpenTitleScene()
        {
            SessionState.SetBool(SessionKey, true);
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;

            const string path = PrototypeSceneBuilder.TitleScenePath;
            if (SceneManager.sceneCount == 1 && SceneManager.GetActiveScene().path == path) return;

            // A Unity recovery session can contain scenes that have never been saved.
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }
}
