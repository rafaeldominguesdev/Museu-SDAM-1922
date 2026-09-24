using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Exports a read-only visual snapshot. Gameplay remains in Unity.</summary>
    public static class MuseumBlenderExport
    {
        [Serializable] private class Snapshot
        {
            public string source = "Assets/Scenes/MuseumScene.unity";
            public string coordinates = "Blender world: Unity (x,y,z) -> (x,z,y)";
            public List<Surface> materials = new List<Surface>();
            public List<Geometry> meshes = new List<Geometry>();
            public List<Label> texts = new List<Label>();
            public List<Lamp> lights = new List<Lamp>();
        }
        [Serializable] private class Surface
        {
            public int id;
            public string name, shader, texture;
            public float[] color, emission, textureScale, textureOffset;
            public float metallic, roughness;
            public bool emissionUsesTexture;
        }
        [Serializable] private class Geometry
        {
            public string name, path;
            public float[] vertices, normals, uv, colors;
            public int[] triangles, faceMaterials;
        }
        [Serializable] private class Label
        {
            public string name, path, body;
            public float[] position, right, up, color;
            public float width, height;
            public int anchor, alignment;
        }
        [Serializable] private class Lamp
        {
            public string name, type;
            public float[] position, direction, color;
            public float intensity, range, spotAngle;
        }

        [MenuItem("MuseumModerna/Exportar visual atual para Blender")]
        public static void Export()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts/blender-preview"));
            Directory.CreateDirectory(folder);
            var data = new Snapshot();
            var ids = new Dictionary<Material, int>();
            var textures = new Dictionary<Texture, string>();
            int triangleCount = 0;
            foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.InstanceID))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                var mesh = filter.sharedMesh;
                if (!renderer || !renderer.enabled || !mesh || !filter.gameObject.activeInHierarchy) continue;
                // UI and the player's reticle are part of the application, not the museum model.
                if (filter.GetComponentInParent<Canvas>() || filter.GetComponentInParent<Camera>()) continue;
                var vertices = mesh.vertices;
                var normals = mesh.normals;
                var uv = mesh.uv;
                var colors = mesh.colors;
                var matrix = filter.transform.localToWorldMatrix;
                var normalMatrix = matrix.inverse.transpose;
                var output = new Geometry
                {
                    name = filter.name, path = ObjectPath(filter.transform),
                    vertices = new float[vertices.Length * 3],
                    normals = normals.Length == vertices.Length ? new float[vertices.Length * 3] : new float[0],
                    uv = uv.Length == vertices.Length ? new float[vertices.Length * 2] : new float[0],
                    colors = colors.Length == vertices.Length ? new float[vertices.Length * 4] : new float[0]
                };
                for (int i = 0; i < vertices.Length; i++)
                {
                    CopyVector(output.vertices, i, matrix.MultiplyPoint3x4(vertices[i]));
                    if (output.normals.Length > 0) CopyVector(output.normals, i, normalMatrix.MultiplyVector(normals[i]).normalized);
                    if (output.uv.Length > 0) { output.uv[i * 2] = uv[i].x; output.uv[i * 2 + 1] = uv[i].y; }
                    if (output.colors.Length > 0)
                    {
                        output.colors[i * 4] = colors[i].r; output.colors[i * 4 + 1] = colors[i].g;
                        output.colors[i * 4 + 2] = colors[i].b; output.colors[i * 4 + 3] = colors[i].a;
                    }
                }
                var triangles = new List<int>();
                var faceMaterials = new List<int>();
                var slots = renderer.sharedMaterials;
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    if (mesh.GetTopology(submesh) != MeshTopology.Triangles) continue;
                    var material = slots.Length > 0 ? slots[Math.Min(submesh, slots.Length - 1)] : null;
                    int id = MaterialId(material, data, ids, textures, folder);
                    var indices = mesh.GetTriangles(submesh);
                    bool reverse = matrix.determinant > 0; // Axis conversion reverses handedness.
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        triangles.Add(indices[i]);
                        triangles.Add(indices[i + (reverse ? 2 : 1)]);
                        triangles.Add(indices[i + (reverse ? 1 : 2)]);
                        faceMaterials.Add(id);
                    }
                }
                output.triangles = triangles.ToArray(); output.faceMaterials = faceMaterials.ToArray();
                triangleCount += faceMaterials.Count;
                data.meshes.Add(output);
            }
            foreach (var text in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.InstanceID))
            {
                var renderer = text.GetComponent<MeshRenderer>();
                if (!renderer || !renderer.enabled || !text.gameObject.activeInHierarchy || string.IsNullOrEmpty(text.text)) continue;
                var t = text.transform;
                var bounds = renderer.localBounds;
                data.texts.Add(new Label
                {
                    name = text.name, path = ObjectPath(t), body = text.text,
                    position = Vector(t.position), right = Vector(t.right), up = Vector(t.up),
                    color = ColorArray(text.color), anchor = (int)text.anchor, alignment = (int)text.alignment,
                    width = bounds.size.x * t.TransformVector(Vector3.right).magnitude,
                    height = bounds.size.y * t.TransformVector(Vector3.up).magnitude
                });
            }
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID))
            {
                if (!light.enabled || !light.gameObject.activeInHierarchy) continue;
                data.lights.Add(new Lamp
                {
                    name = light.name, type = light.type.ToString(), position = Vector(light.transform.position),
                    direction = Vector(light.transform.forward), color = ColorArray(light.color),
                    intensity = light.intensity, range = light.range, spotAngle = light.spotAngle
                });
            }
            File.WriteAllText(Path.Combine(folder, "museum-export.json"), JsonUtility.ToJson(data));
            Debug.Log($"[BlenderExport] Exported {data.meshes.Count} meshes, {triangleCount} triangles, {data.materials.Count} materials, {data.texts.Count} texts, {data.lights.Count} lights to {folder}. Scene was not modified.");
        }

        private static int MaterialId(Material material, Snapshot data, Dictionary<Material, int> ids,
            Dictionary<Texture, string> textures, string folder)
        {
            if (!material) return -1;
            if (ids.TryGetValue(material, out int existing)) return existing;
            int id = data.materials.Count;
            ids.Add(material, id);
            var color = material.HasProperty("_Color") ? material.color : Color.white;
            var emission = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
            bool hasTexture = material.HasProperty("_MainTex");
            var scale = hasTexture ? material.mainTextureScale : Vector2.one;
            var offset = hasTexture ? material.mainTextureOffset : Vector2.zero;
            var output = new Surface
            {
                id = id, name = material.name, shader = material.shader ? material.shader.name : "",
                color = ColorArray(color), emission = new[] { emission.r, emission.g, emission.b },
                metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0,
                roughness = 1 - (material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : .5f),
                texture = "", textureScale = new[] { scale.x, scale.y }, textureOffset = new[] { offset.x, offset.y }
            };
            var texture = hasTexture ? material.mainTexture : null;
            output.emissionUsesTexture = texture && material.HasProperty("_EmissionMap") && material.GetTexture("_EmissionMap") == texture;
            if (texture)
            {
                if (!textures.TryGetValue(texture, out string filename))
                {
                    filename = "texture_" + textures.Count.ToString("D3") + ".png";
                    Texture2D copy = null; RenderTexture temporary = null;
                    var previous = RenderTexture.active;
                    try
                    {
                        if (texture is Texture2D readable && readable.isReadable)
                            File.WriteAllBytes(Path.Combine(folder, filename), readable.EncodeToPNG());
                        else
                        {
                            temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                            Graphics.Blit(texture, temporary); RenderTexture.active = temporary;
                            copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); copy.Apply();
                            File.WriteAllBytes(Path.Combine(folder, filename), copy.EncodeToPNG());
                        }
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                        if (temporary) RenderTexture.ReleaseTemporary(temporary);
                        if (copy) UnityEngine.Object.DestroyImmediate(copy);
                    }
                    textures.Add(texture, filename);
                }
                output.texture = filename;
            }
            data.materials.Add(output);
            return id;
        }
        private static string ObjectPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent) { transform = transform.parent; path = transform.name + "/" + path; }
            return path;
        }
        private static float[] Vector(Vector3 v) => new[] { v.x, v.z, v.y };
        private static float[] ColorArray(Color c) => new[] { c.r, c.g, c.b, c.a };
        private static void CopyVector(float[] target, int index, Vector3 v)
        { target[index * 3] = v.x; target[index * 3 + 1] = v.z; target[index * 3 + 2] = v.y; }
    }
}
