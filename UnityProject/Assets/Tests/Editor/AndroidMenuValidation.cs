using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseumModerna
{
    [InitializeOnLoad]
    public static class AndroidMenuValidation
    {
        private const string Key = "MuseumAndroidMenuValidation";
        static AndroidMenuValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, false);
                new GameObject("AndroidMenuValidation").AddComponent<AndroidMenuRuntimeValidation>();
            };
        }
        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
                SessionState.SetBool(Key, true);
                EditorApplication.isPlaying = true;
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
