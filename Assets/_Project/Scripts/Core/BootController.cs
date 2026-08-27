using UnityEngine;

namespace MeraBrand.Expo.Core
{
    public sealed class BootController : MonoBehaviour
    {
        [SerializeField] private bool loadMainMenuOnStart = true;

        private void Start()
        {
            if (loadMainMenuOnStart && SceneLoader.Instance != null)
                SceneLoader.Instance.LoadMainMenu();
        }
    }
}
