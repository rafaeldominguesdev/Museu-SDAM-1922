using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Checks the saved artwork surfaces before static batching and PlayMode.</summary>
    public static class ArtworkImageValidation
    {
        [Serializable] private sealed class Manifest { public Entry[] items; }
        [Serializable] private sealed class Entry
        {
            public string exhibitId, texturePath, title, artist, periodo, tecnica, description;
            public string sourcePage, imageUrl, credit;
            public int year;
        }

        // Batch invocation: -executeMethod MuseumModerna.ArtworkImageValidation.Run
        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
                ValidateSavedScene();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void ValidateSavedScene()
        {
            var asset = Resources.Load<TextAsset>("MuseumArtworkImages");
            Require(asset != null, "The artwork source manifest is included in Resources.");
            var manifest = JsonUtility.FromJson<Manifest>(asset.text);
            Require(manifest != null && manifest.items != null && manifest.items.Length >= 3,
                "The museum contains at least three documented artwork reproductions.");
            Require(manifest.items.Select(item => item.exhibitId).Distinct().Count() == manifest.items.Length,
                "Each artwork mapping has a unique exhibit ID.");

            var exhibits = UnityEngine.Object.FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);
            var references = UnityEngine.Object.FindObjectsByType<ExhibitGuideReference>(FindObjectsSortMode.None);
            int walls = 0, islands = 0;
            foreach (var entry in manifest.items)
            {
                Require(!string.IsNullOrWhiteSpace(entry.sourcePage) && !string.IsNullOrWhiteSpace(entry.imageUrl) &&
                    !string.IsNullOrWhiteSpace(entry.credit), "Image provenance is recorded: " + entry.exhibitId);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(entry.texturePath);
                Require(texture != null, "The documented source image imports successfully: " + entry.exhibitId);
                CheckImport(entry, texture);
                var exhibit = exhibits.SingleOrDefault(item => item.PaintingData != null &&
                    item.PaintingData.uniqueId == entry.exhibitId);
                Require(exhibit != null, "A saved interactive exhibit owns the artwork: " + entry.exhibitId);
                var data = exhibit.PaintingData;
                Require(data.paintingTexture == texture, "The guide and the wall use the same source image: " + entry.exhibitId);
                Require(data.title == entry.title && data.artist == entry.artist && data.year == entry.year &&
                    data.Periodo == entry.periodo, "The guide identifies the displayed artwork: " + entry.exhibitId);
                var backing = exhibit.GetComponent<Renderer>();
                Require(backing != null && backing.sharedMaterial != null && backing.sharedMaterial.mainTexture == null,
                    "The frame backing stays plain instead of stretching the image: " + entry.exhibitId);
                var legacy = GameObject.Find("Expografia_" + entry.exhibitId);
                Require(legacy != null, "The artwork retains its wall display: " + entry.exhibitId);
                var art = legacy.transform.Find("Reproducao_obra");
                Require(art != null && art.gameObject.activeInHierarchy,
                    "The saved reproduction surface is visible: " + entry.exhibitId);
                var composition = legacy.transform.Find("Composicao_cenografica");
                Require(composition == null || !composition.gameObject.activeInHierarchy ||
                    !composition.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled),
                    "The old demonstration geometry does not cover the artwork: " + entry.exhibitId);
                CheckCaptions(legacy.transform, entry);
                CheckSurface(art, legacy.transform, texture, 2.38f - .12f, 1.87f - .12f, entry.exhibitId + " wall");
                walls++;

                foreach (var reference in references.Where(item => item.Source == exhibit &&
                    item.name.StartsWith("Estudo_dupla_face_", StringComparison.Ordinal)))
                {
                    CheckCaptions(reference.transform, entry);
                    // Combine() may absorb the Quad into a mesh grouped by its artwork material.
                    CheckSurface(reference.transform, reference.transform, texture,
                        2.2f - .12f, 1.73f - .12f, entry.exhibitId + " island");
                    islands++;
                }
            }
            var islandIds = new[] { "homem_amarelo", "estudante_russa", "di_cavalcanti_pierrot", "tropical" };
            int expectedIslands = manifest.items.Count(item => islandIds.Contains(item.exhibitId));
            Require(islands == expectedIslands, "Every mapped island face displays its source image.");
            Debug.Log("[ArtworkImageValidation] PASS: " + walls + " wall reproductions, " + islands +
                " island reproductions; documented sources, correct labels, full UVs, native proportions and mobile imports.");
        }

        private static void CheckImport(Entry entry, Texture2D texture)
        {
            var importer = AssetImporter.GetAtPath(entry.texturePath) as TextureImporter;
            Require(importer != null && importer.textureType == TextureImporterType.Default &&
                importer.sRGBTexture && importer.mipmapEnabled && !importer.isReadable,
                "Artwork uses color sampling and mipmaps without a CPU texture copy: " + entry.exhibitId);
            Require(importer.maxTextureSize <= 2048 && texture.width <= 2048 && texture.height <= 2048,
                "Artwork stays within the 2048-pixel mobile budget: " + entry.exhibitId);
            var android = importer.GetPlatformTextureSettings("Android");
            Require(android.overridden && android.maxTextureSize <= 2048 && android.format == TextureImporterFormat.ASTC_6x6,
                "Android artwork uses ASTC 6x6 within the texture budget: " + entry.exhibitId);
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            Require(sourceWidth > 0 && sourceHeight > 0 &&
                Mathf.Abs((float)texture.width / texture.height / ((float)sourceWidth / sourceHeight) - 1) < .01f,
                "Import preserves the source image proportions: " + entry.exhibitId);
        }

        private static void CheckCaptions(Transform display, Entry entry)
        {
            var captions = display.GetComponentsInChildren<TextMesh>()
                .Where(item => item.GetComponent<Renderer>() == null || item.GetComponent<Renderer>().enabled)
                .Select(item => item.text).ToArray();
            Require(captions.Any(text => text.Contains(entry.title)) && captions.Any(text => text.Contains(entry.artist)),
                "The visible caption identifies the displayed title and artist: " + display.name);
            Require(!captions.Any(text => text.IndexOf("CENOGR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("PAINEL DIDÁTICO", StringComparison.OrdinalIgnoreCase) >= 0),
                "The reproduction has no obsolete demonstration caption: " + display.name);
        }

        private static void CheckSurface(Transform root, Transform display, Texture2D texture,
            float availableWidth, float availableHeight, string label)
        {
            var points = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled || filter.sharedMesh == null) continue;
                var mesh = filter.sharedMesh;
                var vertices = mesh.vertices;
                var meshUvs = mesh.uv;
                var meshNormals = mesh.normals;
                var materials = renderer.sharedMaterials;
                for (int submesh = 0; submesh < mesh.subMeshCount && submesh < materials.Length; submesh++)
                {
                    var material = materials[submesh];
                    if (material == null || material.mainTexture != texture) continue;
                    Require(material.mainTextureScale == Vector2.one && material.mainTextureOffset == Vector2.zero,
                        "The material shows the complete source image: " + label);
                    Require(meshUvs.Length == vertices.Length && meshNormals.Length == vertices.Length,
                        "The artwork surface has complete UVs and normals: " + label);
                    foreach (int index in mesh.GetIndices(submesh).Distinct())
                    {
                        var delta = filter.transform.TransformPoint(vertices[index]) - display.position;
                        points.Add(new Vector3(Vector3.Dot(delta, display.right), Vector3.Dot(delta, display.up),
                            Vector3.Dot(delta, display.forward)));
                        uvs.Add(meshUvs[index]);
                        normals.Add(filter.transform.TransformDirection(meshNormals[index]));
                    }
                }
            }
            Require(points.Count >= 4, "The source image is rendered by saved geometry: " + label);
            float width = points.Max(point => point.x) - points.Min(point => point.x);
            float height = points.Max(point => point.y) - points.Min(point => point.y);
            Require(width > 0 && height > 0 && width <= availableWidth + .01f && height <= availableHeight + .01f,
                "The entire artwork fits inside its frame with a border: " + label);
            Require(Mathf.Abs(width / height / ((float)texture.width / texture.height) - 1) < .005f,
                "The displayed artwork preserves its image aspect ratio: " + label);
            Require(points.Max(point => point.z) - points.Min(point => point.z) < .001f &&
                normals.All(normal => Vector3.Dot(normal.normalized, display.forward) > .99f),
                "The artwork is a flat surface facing the visitor: " + label);
            Require(Mathf.Abs(uvs.Min(uv => uv.x)) < .001f && Mathf.Abs(uvs.Min(uv => uv.y)) < .001f &&
                Mathf.Abs(uvs.Max(uv => uv.x) - 1) < .001f && Mathf.Abs(uvs.Max(uv => uv.y) - 1) < .001f,
                "UVs cover the full artwork without cropping: " + label);
            var across = Vector3.zero;
            var up = Vector3.zero;
            for (int i = 0; i < points.Count; i++)
            {
                across += points[i] * (uvs[i].x - .5f);
                up += points[i] * (uvs[i].y - .5f);
            }
            Require(across.normalized.x < -.99f && up.normalized.y > .99f,
                "The image is upright and reads correctly from the visitor side: " + label);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[ArtworkImageValidation] " + message);
        }
    }
}
