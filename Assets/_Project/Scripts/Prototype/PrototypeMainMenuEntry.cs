using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Application entry uses the authored prototype scene's shared content, without duplicating catalogs.</summary>
    public sealed class PrototypeMainMenuEntry : MonoBehaviour
    {
        private static bool _mainMenuRequested;
        internal static bool ConsumeMainMenuRequest()
        {
            bool requested = _mainMenuRequested;
            _mainMenuRequested = false;
            return requested;
        }
        private IEnumerator Start()
        {
            _mainMenuRequested = true;
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
        }
    }
}
