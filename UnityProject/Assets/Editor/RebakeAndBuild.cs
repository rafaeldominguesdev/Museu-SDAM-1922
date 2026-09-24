using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MuseumModerna
{
    /// <summary>
    /// Uso único via CLI: rebakeia o Occlusion Culling (a geometria da cena mudou bastante
    /// desde o último bake — ver artifacts/semana-arte-moderna-armadilhas na memória do
    /// projeto: bake desatualizado faz pedaços do museu piscarem) e então gera o APK.
    ///
    /// Unity -batchmode -quit -projectPath UnityProject -executeMethod MuseumModerna.RebakeAndBuild.Run
    /// </summary>
    public static class RebakeAndBuild
    {
        private const string ScenePath = "Assets/Scenes/MuseumScene.unity";

        public static void Run()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
                throw new System.Exception($"[RebakeAndBuild] Não foi possível abrir a cena: {ScenePath}");

            Debug.Log("[RebakeAndBuild] Rebakeando Occlusion Culling...");
            MethodInfo bake = typeof(MuseumOptimizationSetup).GetMethod(
                "BakeOcclusionCulling", BindingFlags.NonPublic | BindingFlags.Static);
            if (bake == null)
                throw new System.Exception("[RebakeAndBuild] Método BakeOcclusionCulling não encontrado.");
            bake.Invoke(null, null);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[RebakeAndBuild] Cena salva após rebake: {saved}");

            Debug.Log("[RebakeAndBuild] Gerando APK...");
            BuildScript.BuildAndroid();
        }
    }
}
