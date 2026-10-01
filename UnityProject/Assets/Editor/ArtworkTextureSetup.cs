using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Instala reproduções verificadas sem alterar molduras, circulação ou fichas de interação.</summary>
    public static class ArtworkTextureSetup
    {
        [Serializable] public sealed class Entry
        {
            public string exhibitId, texturePath, title, artist, periodo, tecnica, description;
            public string sourcePage, imageUrl, credit;
            public int year;
        }
        [Serializable] private sealed class Manifest { public Entry[] items; }
        private const string ManifestPath = "Assets/Resources/MuseumArtworkImages.json";
        private const string MaterialFolder = "Assets/Artworks/Materials";

        [MenuItem("Museum/Visual/Corrigir iluminacao das pinturas")]
        public static void ApplyDisplayLighting()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            ApplyDisplayLightingToScene();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Falha ao salvar iluminação das obras.");
        }

        public static void ApplyDisplayLightingToScene()
        {
            var exhibits = UnityEngine.Object.FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None)
                .Where(e => e.PaintingData != null).ToDictionary(e => e.PaintingData.uniqueId);
            int count = 0;
            foreach (var entry in Read())
            {
                if (!exhibits.TryGetValue(entry.exhibitId, out var exhibit)) continue;
                var renderer = exhibit.ArtworkRenderer != null ? exhibit.ArtworkRenderer : exhibit.GetComponent<Renderer>();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.texturePath);
                if (renderer == null || texture == null) throw new InvalidOperationException("Superfície ou imagem ausente: " + entry.exhibitId);
                // A cena antiga usava o material bege do passe-partout para a própria imagem.
                // Um material por obra elimina essa tintura e mantém a iluminação neutra.
                renderer.sharedMaterial = ImageMaterial(entry, texture);
                exhibit.SetArtworkRenderer(renderer);
                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(exhibit);
                count++;
            }
            MuseumVisitRepair.ConfigurePictureLights();
            Debug.Log("[ArtworkLighting] " + count + " pinturas com material neutro e focos direcionados nas luminárias.");
        }

        public static Entry[] Read()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath);
            return json == null ? Array.Empty<Entry>() : JsonUtility.FromJson<Manifest>(json.text).items ?? Array.Empty<Entry>();
        }

        [MenuItem("Museum/Visual/Aplicar imagens das obras verificadas")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var entries = Read();
            if (entries.Length == 0) throw new InvalidOperationException("O manifesto de imagens das obras está vazio.");
            foreach (var entry in entries) ConfigureTexture(entry.texturePath);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            CuratorialCatalog.Install();
            ApplyWallImages();
            var architecture = GameObject.Find("Museum_LouvreStyle");
            if (architecture == null) throw new InvalidOperationException("Arquitetura do museu ausente.");
            // Recria também as quatro faces das ilhas com as mesmas imagens e fichas.
            LouvreAtmosphereSetup.ApplyTo(architecture.transform);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Falha ao salvar as reproduções.");
            Debug.Log($"[ArtworkImages] {entries.Length} obras aplicadas com proporções preservadas, mipmaps e ASTC 6x6 para Android.");
        }

        public static void ApplyWallImages()
        {
            var entries = Read();
            if (entries.Length == 0) return;
            var exhibits = UnityEngine.Object.FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None)
                .Where(e => e.PaintingData != null).ToDictionary(e => e.PaintingData.uniqueId);
            var ids = new HashSet<string>();
            foreach (var entry in entries)
            {
                if (!ids.Add(entry.exhibitId)) throw new InvalidOperationException("ID de imagem duplicado: " + entry.exhibitId);
                if (!exhibits.TryGetValue(entry.exhibitId, out var exhibit))
                    throw new InvalidOperationException("Ficha não encontrada para a imagem: " + entry.exhibitId);
                var legacy = GameObject.Find("Expografia_" + entry.exhibitId);
                if (legacy == null) throw new InvalidOperationException("Quadro não encontrado: " + entry.exhibitId);
                var source = legacy.transform.Find("Composicao_cenografica");
                if (source != null) source.gameObject.SetActive(false);
                var renderer = CreateImage(legacy.transform, entry, 2.38f, 1.87f, .025f);
                exhibit.PaintingData.paintingTexture = (Texture2D)renderer.sharedMaterial.mainTexture;
                exhibit.SetArtworkRenderer(renderer);
                EditorUtility.SetDirty(exhibit.PaintingData);
                EditorUtility.SetDirty(exhibit);

                // O cubo original continua sendo o passe-partout e o alvo físico do olhar.
                var backing = exhibit.GetComponent<Renderer>();
                if (backing != null)
                    backing.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Guide_Papel.mat");
                foreach (var caption in legacy.GetComponentsInChildren<TextMesh>(true).Where(t => t.transform.parent == legacy.transform))
                {
                    bool footnote = caption.name == "Credito_reproducao" ||
                        caption.text.StartsWith("PAINEL DIDÁTICO", StringComparison.Ordinal) ||
                        caption.text.StartsWith("REPRODUÇÃO", StringComparison.Ordinal);
                    if (footnote)
                    {
                        caption.name = "Credito_reproducao";
                        caption.text = "REPRODUÇÃO DIGITAL  /  " + entry.credit;
                        caption.characterSize = .023f * .25f;
                    }
                    else caption.text = exhibit.PaintingData.title + "\n" + exhibit.PaintingData.artist + " | " + exhibit.PaintingData.Periodo;
                }
            }
        }

        public static bool TryApplyFace(Transform parent, PaintingExhibit exhibit, float width, float height)
        {
            var entry = Read().FirstOrDefault(e => e.exhibitId == exhibit.PaintingData.uniqueId);
            if (entry == null) return false;
            CreateImage(parent, entry, width, height, .026f);
            return true;
        }

        private static Renderer CreateImage(Transform parent, Entry entry, float width, float height, float depth)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.texturePath);
            if (texture == null) throw new InvalidOperationException("Textura não encontrada: " + entry.texturePath);
            var previous = parent.Find("Reproducao_obra");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var image = GameObject.CreatePrimitive(PrimitiveType.Quad);
            image.name = "Reproducao_obra";
            image.transform.SetParent(parent, false);
            image.transform.localPosition = new Vector3(0, 0, depth);
            // O observador está no lado +Z do quadro. Rotacionar também os UVs
            // mantém a imagem legível da esquerda para a direita, como a legenda.
            image.transform.localRotation = Quaternion.Euler(0, 180, 0);
            const float border = .06f;
            float scale = Mathf.Min((width - 2 * border) / texture.width, (height - 2 * border) / texture.height);
            image.transform.localScale = new Vector3(texture.width * scale, texture.height * scale, 1);
            UnityEngine.Object.DestroyImmediate(image.GetComponent<Collider>());
            image.isStatic = true;
            var renderer = image.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = ImageMaterial(entry, texture);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Material ImageMaterial(Entry entry, Texture2D texture)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Artworks")) AssetDatabase.CreateFolder("Assets", "Artworks");
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Artworks", "Materials");
            string path = MaterialFolder + "/Obra_" + entry.exhibitId + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = Color.white;
            material.mainTexture = texture;
            material.mainTextureScale = Vector2.one;
            material.mainTextureOffset = Vector2.zero;
            material.SetFloat("_Metallic", 0);
            material.SetFloat("_Glossiness", .08f);
            material.SetTexture("_EmissionMap", texture);
            // Preenchimento neutro mantém a leitura das pinturas mesmo no trecho escuro
            // do ciclo dia/noite. A emissão usa a própria textura, preservando os pretos.
            material.SetColor("_EmissionColor", new Color(.55f, .55f, .55f));
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static void ConfigureTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Arquivo de imagem não importável: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 2;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            var android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.compressionQuality = 100;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }
    }
}
