using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>
    /// Sombras de contato modeladas, sem luzes, texturas ou custo de atualização.
    /// Reaplicar após posicionar a expografia; as malhas persistidas preservam seus GUIDs.
    /// </summary>
    public static class LouvreContactShadows
    {
        private const string MeshFolder = "Assets/MuseumStyle/Meshes";
        private const string MaterialPath = "Assets/MuseumStyle/Materials/Sombra_de_contato.mat";
        // Parquet a 0,008 m; medalhão e seus frisos chegam a aproximadamente 0,025 m.
        private const float GroundHeight = .025f;

        public static void Apply(Transform environmentRoot)
        {
            if (environmentRoot == null) throw new ArgumentNullException(nameof(environmentRoot));
            EnsureFolder(MeshFolder);
            EnsureFolder("Assets/MuseumStyle/Materials");
            var old = environmentRoot.Find("Sombras_de_contato");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Sombras_de_contato").transform;
            root.SetParent(environmentRoot, false);
            var material = ShadowMaterial();
            var rooms = new Dictionary<string, Geometry>();
            int footprints = 0;

            var targets = environmentRoot.gameObject.scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<Transform>())
                .Where(t => t.gameObject.activeInHierarchy && IsSupport(t.name))
                .OrderBy(t => t.name, StringComparer.Ordinal).ToArray();
            foreach (var target in targets)
            {
                if (!TryLocalBounds(target, out var bounds)) continue;
                string room = RoomAt(target.position);
                if (!rooms.TryGetValue(room, out var geometry))
                {
                    geometry = new Geometry(root);
                    rooms.Add(room, geometry);
                }

                if (target.name.StartsWith("Banco_curvo_", StringComparison.Ordinal))
                    geometry.CurvedBench(target);
                else if (target.name.StartsWith("Soclo_circular_", StringComparison.Ordinal) ||
                         target.name.StartsWith("Poste_cordao_", StringComparison.Ordinal) ||
                         target.name.StartsWith("Vaso_galeria_", StringComparison.Ordinal))
                    geometry.Ellipse(target, bounds, .25f, .78f);
                else
                    geometry.RoundedRectangle(target, bounds,
                        target.name.StartsWith("Painel_curvo_", StringComparison.Ordinal) ? .24f : .36f,
                        target.name.StartsWith("Painel_curvo_", StringComparison.Ordinal) ? .89f : .84f);
                footprints++;
            }

            int triangles = 0;
            foreach (var room in rooms.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                var go = new GameObject("Contato_" + room.Key);
                go.transform.SetParent(root, false);
                go.layer = 2; // Ignore Raycast: as sombras nunca capturam seleção ou movimento.
                var mesh = room.Value.Build();
                triangles += mesh.triangles.Length / 3;
                go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(room.Key, mesh);
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                // Já agrupado por sala. Evita nova cópia das malhas no static batching.
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccludeeStatic);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[LouvreContactShadows] {footprints} apoios, {rooms.Count} malhas por sala, " +
                      $"{triangles} triângulos, um material, zero luzes/texturas/colliders adicionais.");
        }

        private static bool IsSupport(string name)
        {
            return name.StartsWith("Banco_curvo_", StringComparison.Ordinal) ||
                   name.StartsWith("Soclo_circular_", StringComparison.Ordinal) ||
                   name.StartsWith("Painel_curvo_", StringComparison.Ordinal) ||
                   name.StartsWith("Vitrine_ilha_", StringComparison.Ordinal) ||
                   name.StartsWith("Poste_cordao_", StringComparison.Ordinal) ||
                   name.StartsWith("Vaso_galeria_", StringComparison.Ordinal);
        }

        private static string RoomAt(Vector3 position)
        {
            if (position.z > 10) return "Pintura";
            if (position.z < -10) return "Entrada";
            if (position.x > 10) return "Escultura";
            if (position.x < -10) return "Literatura";
            return "Salao";
        }

        private static bool TryLocalBounds(Transform target, out Bounds result)
        {
            result = default;
            bool initialized = false;
            foreach (var collider in target.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy) continue;
                Bounds bounds;
                if (collider is BoxCollider box) bounds = new Bounds(box.center, box.size);
                else if (collider is CapsuleCollider capsule)
                {
                    var size = Vector3.one * capsule.radius * 2;
                    size[capsule.direction] = Mathf.Max(capsule.height, capsule.radius * 2);
                    bounds = new Bounds(capsule.center, size);
                }
                else if (collider is SphereCollider sphere)
                    bounds = new Bounds(sphere.center, Vector3.one * sphere.radius * 2);
                else if (collider is MeshCollider mesh && mesh.sharedMesh != null) bounds = mesh.sharedMesh.bounds;
                else continue;

                for (int corner = 0; corner < 8; corner++)
                {
                    var local = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                        (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    var point = target.InverseTransformPoint(collider.transform.TransformPoint(local));
                    if (!initialized) { result = new Bounds(point, Vector3.zero); initialized = true; }
                    else result.Encapsulate(point);
                }
            }
            return initialized && result.size.x > .001f && result.size.z > .001f;
        }

        private static Material ShadowMaterial()
        {
            var shader = Shader.Find("Museum/Contact Shadow");
            if (shader == null) throw new InvalidOperationException("Shader Museum/Contact Shadow não encontrado.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Sombra_de_contato" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else material.shader = shader;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh SaveMesh(string room, Mesh mesh)
        {
            string path = MeshFolder + "/LouvreContact_" + room + ".asset";
            mesh.name = "Sombra_de_contato_" + room;
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }

        private sealed class Geometry
        {
            private readonly Transform root;
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Color32> colors = new List<Color32>();
            private readonly List<int> triangles = new List<int>();

            public Geometry(Transform root) { this.root = root; }

            private int Vertex(Transform support, Vector2 point, float shade)
            {
                var world = support.TransformPoint(new Vector3(point.x, 0, point.y));
                world.y = world.x * world.x + world.z * world.z < 3.85f * 3.85f ? .04f : GroundHeight;
                vertices.Add(root.InverseTransformPoint(world));
                byte value = (byte)Mathf.RoundToInt(Mathf.Clamp01(shade) * 255);
                colors.Add(new Color32(value, value, value, 255));
                return vertices.Count - 1;
            }

            private void Triangle(int a, int b, int c)
            { triangles.Add(a); triangles.Add(b); triangles.Add(c); }

            private void Quad(int a, int b, int c, int d)
            { Triangle(a, b, c); Triangle(a, c, d); }

            public void Ellipse(Transform target, Bounds bounds, float spread, float shade)
            {
                const int segments = 16;
                var center = new Vector2(bounds.center.x, bounds.center.z);
                var half = new Vector2(bounds.extents.x, bounds.extents.z);
                ConvexFootprint(target, center, segments, shade, (ring, segment) =>
                {
                    float angle = segment * Mathf.PI * 2 / segments;
                    float expansion = ring == 0 ? 0 : spread * ring / 3;
                    return center + Vector2.Scale(half + Vector2.one * expansion,
                        new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)));
                });
            }

            public void RoundedRectangle(Transform target, Bounds bounds, float spread, float shade)
            {
                const int cornerSegments = 3;
                const int count = 4 * cornerSegments;
                var center = new Vector2(bounds.center.x, bounds.center.z);
                var half = new Vector2(bounds.extents.x, bounds.extents.z);
                float radius = Mathf.Min(.24f, Mathf.Min(half.x, half.y) * .8f);
                ConvexFootprint(target, center, count, shade, (ring, segment) =>
                {
                    float expansion = ring == 0 ? 0 : spread * ring / 3;
                    int corner = segment / cornerSegments;
                    float angle = (corner * 90f + (segment % cornerSegments) * 90f / (cornerSegments - 1)) * Mathf.Deg2Rad;
                    var cornerCenter = new Vector2((corner == 0 || corner == 3 ? 1 : -1) * (half.x - radius),
                        (corner < 2 ? 1 : -1) * (half.y - radius));
                    return center + cornerCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius + expansion);
                });
            }

            private void ConvexFootprint(Transform target, Vector2 center, int count, float shade,
                Func<int, int, Vector2> outline)
            {
                int middle = Vertex(target, center, shade);
                int first = vertices.Count;
                for (int ring = 0; ring < 4; ring++)
                {
                    float fade = ring / 3f;
                    float value = Mathf.Lerp(shade, 1, fade * fade * (3 - 2 * fade));
                    for (int segment = 0; segment < count; segment++) Vertex(target, outline(ring, segment), value);
                }
                for (int segment = 0; segment < count; segment++)
                {
                    int next = (segment + 1) % count;
                    Triangle(middle, first + segment, first + next);
                    for (int ring = 0; ring < 3; ring++)
                    {
                        int inner = first + ring * count;
                        int outer = inner + count;
                        Quad(inner + segment, outer + segment, outer + next, inner + next);
                    }
                }
            }

            public void CurvedBench(Transform target)
            {
                const int segments = 12;
                const float radius = 2.05f;
                float[] widths = { -.68f, -.48f, -.28f, 0, .28f, .48f, .68f };
                float[] values = { 1, .97f, .89f, .86f, .89f, .97f, 1 };
                int first = vertices.Count;
                for (int band = 0; band < widths.Length; band++)
                    for (int step = 0; step <= segments; step++)
                    {
                        float angle = Mathf.Lerp(-48, 48, step / (float)segments) * Mathf.Deg2Rad;
                        Vertex(target, new Vector2(Mathf.Sin(angle) * (radius + widths[band]),
                            Mathf.Cos(angle) * (radius + widths[band]) - radius), values[band]);
                    }
                for (int band = 0; band < widths.Length - 1; band++)
                    for (int step = 0; step < segments; step++)
                    {
                        int a = first + band * (segments + 1) + step;
                        int b = a + segments + 1;
                        Quad(a, b, b + 1, a + 1);
                    }
                BenchCap(target, -48, -1);
                BenchCap(target, 48, 1);
            }

            private void BenchCap(Transform target, float degrees, float direction)
            {
                const int segments = 6;
                float angle = degrees * Mathf.Deg2Rad;
                var radial = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                var tangent = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * direction;
                var center = radial * 2.05f - new Vector2(0, 2.05f);
                int middle = Vertex(target, center, .86f);
                int first = vertices.Count;
                float[] radii = { .28f, .48f, .68f };
                float[] values = { .89f, .97f, 1 };
                for (int ring = 0; ring < radii.Length; ring++)
                    for (int step = 0; step <= segments; step++)
                    {
                        float a = step * Mathf.PI / segments;
                        Vertex(target, center + (radial * Mathf.Cos(a) + tangent * Mathf.Sin(a)) * radii[ring], values[ring]);
                    }
                for (int step = 0; step < segments; step++)
                {
                    Triangle(middle, first + step, first + step + 1);
                    for (int ring = 0; ring < radii.Length - 1; ring++)
                    {
                        int a = first + ring * (segments + 1) + step;
                        int b = a + segments + 1;
                        Quad(a, b, b + 1, a + 1);
                    }
                }
            }

            public Mesh Build()
            {
                var mesh = new Mesh { indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
