using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseumModerna
{
    [InitializeOnLoad]
    public static class PhysicsValidation
    {
        private const string Key = "MuseumPhysicsValidation";
        static PhysicsValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, false);
                new GameObject("PhysicsRuntimeValidation").AddComponent<PhysicsRuntimeValidation>();
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
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }
    }
}
