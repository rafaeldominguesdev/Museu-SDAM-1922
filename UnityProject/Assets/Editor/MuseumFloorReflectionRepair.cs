using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Reflexo ambiente contínuo, sem uma face clara quadrada seguindo a câmera.</summary>
    public static class MuseumFloorReflectionRepair
    {
        public const string ReflectionPath = "Assets/MuseumStyle/Materials/Reflexo_difuso_da_galeria.asset";

        [MenuItem("Museum/Correcoes/Retirar quadrado de reflexo no piso")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            ConfigureReflection();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Falha ao salvar reflexo do piso.");
            Debug.Log("[FloorReflection] Reflexo uniforme salvo; mapa antigo preservado para comparação.");
        }

        public static void ConfigureReflection()
        {
            // O mapa anterior tinha a face +Y muito mais clara que as laterais,
            // sem mipmaps: seu limite aparecia como um quadrado ao redor dos pés.
            // Todas as faces e níveis agora são constantes; a luz direta ainda
            // produz brilhos naturais nos metais e nos materiais do museu.
            var map = new Cubemap(16, TextureFormat.RGB24, true)
            {
                name = "Reflexo_difuso_da_galeria",
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[16 * 16];
            for (int i=0;i<pixels.Length;i++) pixels[i] = new Color(.50f, .47f, .41f);
            for (int face=0;face<6;face++) map.SetPixels(pixels,(CubemapFace)face);
            map.Apply(true);
            var saved = AssetDatabase.LoadAssetAtPath<Cubemap>(ReflectionPath);
            if (saved == null) { AssetDatabase.CreateAsset(map,ReflectionPath); saved=map; }
            else { EditorUtility.CopySerialized(map,saved); UnityEngine.Object.DestroyImmediate(map); EditorUtility.SetDirty(saved); }
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = saved;
            RenderSettings.reflectionIntensity = .75f;
        }
    }
}
