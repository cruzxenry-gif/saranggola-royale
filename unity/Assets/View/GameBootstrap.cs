using UnityEngine;

namespace Saranggola.View
{
    [DisallowMultipleComponent]
    public sealed partial class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindFirstObjectByType<GameBootstrap>() == null)
                new GameObject("Saranggola Royale").AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            BuildGame();
        }

        partial void BuildGame();
    }
}
