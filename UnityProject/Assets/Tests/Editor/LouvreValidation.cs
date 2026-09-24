using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseumModerna
{
    [InitializeOnLoad]
    public static class LouvreValidation
    {
        private const string RunningKey = "MuseumLouvreValidation";

        static LouvreValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(RunningKey, false)) return;
                SessionState.SetBool(RunningKey, false);
                new GameObject("LouvreRuntimeValidation").AddComponent<LouvreRuntimeValidation>();
            };
        }

        // Run after LouvreMuseumSetup.Apply has saved the exhibition. No -quit: PlayMode exits the process.
        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
                CheckSavedMeshes();
                SessionState.SetBool(RunningKey, true);
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                SessionState.SetBool(RunningKey, false);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void CheckSavedMeshes()
        {
            var checkedMeshes = new HashSet<Mesh>();
            long vertices = 0;
            foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var renderer = filter.GetComponent<Renderer>();
                var mesh = filter.sharedMesh;
                if (renderer == null || !renderer.enabled || mesh == null || !checkedMeshes.Add(mesh)) continue;
                RequireFinite(mesh.bounds.center, mesh.name + " bounds center");
                RequireFinite(mesh.bounds.size, mesh.name + " bounds size");
                // Runs before PlayMode/static batching, so the saved asset data is checked
                // directly and combined renderer bounds cannot conceal a malformed vertex.
                foreach (var vertex in mesh.vertices)
                {
                    RequireFinite(vertex, mesh.name + " vertex");
                    vertices++;
                }
            }
            if (checkedMeshes.Count == 0) throw new InvalidOperationException("[LouvreValidation] No active saved meshes to validate.");
            Debug.Log("[LouvreValidation] Saved mesh bounds and vertices are finite: PASS (" + checkedMeshes.Count +
                " meshes, " + vertices + " vertices)");
        }

        private static void RequireFinite(Vector3 value, string label)
        {
            if (float.IsNaN(value.x) || float.IsInfinity(value.x) ||
                float.IsNaN(value.y) || float.IsInfinity(value.y) ||
                float.IsNaN(value.z) || float.IsInfinity(value.z))
                throw new InvalidOperationException("[LouvreValidation] Non-finite geometry: " + label);
        }
    }
}
