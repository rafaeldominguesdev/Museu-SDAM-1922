using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseumModerna
{
    [InitializeOnLoad]
    public static class AmbientDetailsValidation
    {
        private const string Key = "MuseumAmbientValidation";
        static AmbientDetailsValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, false);
                new GameObject("AmbientRuntimeValidation").AddComponent<AmbientRuntimeValidation>();
            };
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
    }
}
