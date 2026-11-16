using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackpackAutoBattle.Prototype
{
    // Like MainMenu, this entry scene reuses the authored gameplay content and shared settings.
    public sealed class PrototypeTitleEntry : MonoBehaviour
    {
        private static bool _requested;
        internal static bool ConsumeRequest()
        {
            bool requested = _requested;
            _requested = false;
            return requested;
        }
        private IEnumerator Start()
        {
            _requested = true;
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
        }
    }
}
