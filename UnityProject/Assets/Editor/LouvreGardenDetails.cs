using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>
    /// Vasos torneados e folhagem original, opaca e imóvel para as galerias móveis.
    /// Cinco malhas compartilhadas pelos vasos; sem texturas ou luzes adicionais.
    /// </summary>
    public static class LouvreGardenDetails
    {
        private const string MeshFolder = "Assets/MuseumStyle/Meshes";
        private const string MaterialFolder = "Assets/MuseumStyle/Materials";
        private const int VaseSides = 18;

        public static void Apply(Transform parent, Material stone, Material gold, Material dark)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (stone == null || gold == null || dark == null)
                throw new ArgumentException("Os vasos precisam dos materiais de pedra, bronze e base escura.");
            EnsureFolder(MeshFolder);
            EnsureFolder(MaterialFolder);

            var old = parent.Find("Vegetacao_de_galeria");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var garden = Child(parent, "Vegetacao_de_galeria");

            var stoneGeometry = new Geometry();
            Lathe(stoneGeometry, new[]
            {
                new Vector2(0, 0), new Vector2(.21f, 0),
                new Vector2(.245f, .045f), new Vector2(.20f, .09f),
                new Vector2(.17f, .14f), new Vector2(.255f, .38f),
                new Vector2(.30f, .51f), new Vector2(.305f, .57f),
                new Vector2(.292f, .64f), new Vector2(.245f, .64f),
                new Vector2(.235f, .57f)
            }, VaseSides);
            var metalGeometry = new Geometry();
            Lathe(metalGeometry, new[] { new Vector2(.241f, .035f), new Vector2(.241f, .057f) }, VaseSides);
            Lathe(metalGeometry, new[] { new Vector2(.304f, .565f), new Vector2(.301f, .593f) }, VaseSides);
            var soilGeometry = new Geometry();
            Disk(soilGeometry, .236f, .577f, VaseSides);

            var greenGeometry = new Geometry();
            var sageGeometry = new Geometry();
            // Haste central discreta; todas as folhas nascem dentro da boca do vaso.
            Lathe(greenGeometry, new[] { new Vector2(.021f, .575f), new Vector2(.009f, 1.35f) }, 6);
            for (int leaf = 0; leaf < 12; leaf++)
            {
                int tier = leaf / 4;
                float angle = leaf * 137.508f * Mathf.Deg2Rad;
                float height = new[] { .91f, 1.21f, 1.49f }[tier];
                float reach = new[] { .61f, .49f, .24f }[tier];
                Leaf(leaf % 2 == 0 ? greenGeometry : sageGeometry, angle, height, reach, .085f + tier * .013f);
            }

            var stoneMesh = SaveMesh("Vaso_torneado", stoneGeometry.ToMesh());
            var metalMesh = SaveMesh("Aneis_de_bronze", metalGeometry.ToMesh());
            var soilMesh = SaveMesh("Terra", soilGeometry.ToMesh());
            var greenMesh = SaveMesh("Folhagem_verde", greenGeometry.ToMesh());
            var sageMesh = SaveMesh("Folhagem_salvia", sageGeometry.ToMesh());
            var green = LeafMaterial("LouvreGarden_Verde", new Color(.135f, .235f, .115f));
            var sage = LeafMaterial("LouvreGarden_Salvia", new Color(.245f, .335f, .165f));
            int trianglesPerVase = (stoneMesh.triangles.Length + metalMesh.triangles.Length + soilMesh.triangles.Length +
                greenMesh.triangles.Length + sageMesh.triangles.Length) / 3;
            const int vaseCount = 11;
            if (trianglesPerVase * vaseCount > 10000)
                throw new InvalidOperationException("A vegetação ultrapassou o orçamento de 10.000 triângulos.");

            var positions = new[]
            {
                new Vector3(-7.6f, 0, -7.6f), new Vector3(7.6f, 0, -7.6f),
                new Vector3(-7.6f, 0, 7.6f), new Vector3(7.6f, 0, 7.6f),
                new Vector3(-6.5f, 0, 11.2f), new Vector3(6.5f, 0, 11.2f),
                new Vector3(-22.65f, 0, -8.65f),
                // A sala Di Cavalcanti recebe um vaso em cada canto. As posições
                // preservam o arco de entrada, as fichas e os cordões de segurança.
                new Vector3(11.25f, 0, -8.35f), new Vector3(22.75f, 0, -8.35f),
                new Vector3(11.25f, 0, -1.65f), new Vector3(22.75f, 0, -1.65f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                var vase = Child(garden, "Vaso_galeria_" + (i + 1).ToString("00"));
                if (i >= 7) vase.name = "Vaso_Di_Cavalcanti_" + (i - 6).ToString("00");
                vase.SetPositionAndRotation(positions[i], Quaternion.Euler(0, i * 41, 0));
                float heightVariation=.90f+(i%4)*.07f;
                vase.localScale=new Vector3(.95f+(i%3)*.035f,heightVariation,.95f+(i%3)*.035f);
                Render(vase, "Pedra_torneada", stoneMesh, stone);
                Render(vase, "Filetes_de_bronze", metalMesh, gold);
                Render(vase, "Terra", soilMesh, dark);
                Render(vase, "Folhas_verdes", greenMesh, green);
                Render(vase, "Folhas_salvia", sageMesh, sage);
                ConfigureCollider(vase);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[LouvreStyle] Vegetação: {positions.Length} vasos, {trianglesPerVase * positions.Length} triângulos, 2 materiais verdes opacos compartilhados, sem sombras em tempo real.");
        }

        public static void ConfigureCollider(Transform vase)
        {
            // Faces verticais até a altura da folhagem, sem a rampa arredondada
            // que o CharacterController conseguia subir na cápsula de 68 cm.
            foreach (var old in vase.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(old);
            var collider = vase.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(.66f, 1.8f, .66f);
            collider.center = Vector3.up * .9f;
        }

        private static void Leaf(Geometry output, float angle, float rise, float reach, float halfWidth)
        {
            var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var tangent = new Vector3(-radial.z, 0, radial.x);
            const int segments = 4;
            int start = output.Vertices.Count;
            // Cada folha tem duas superfícies, com vértices separados para normais corretas.
            // A nervura central forma uma dobra sutil; o material permanece opaco.
            for (int face = 0; face < 2; face++)
                for (int row = 0; row <= segments; row++)
                {
                    float t = row / (float)segments;
                    float width = halfWidth * Mathf.Sin(Mathf.PI * t);
                    var center = radial * (.025f + reach * t * t) +
                        Vector3.up * (.65f + rise * Mathf.Sin(t * Mathf.PI * .61f));
                    float fold = .021f * Mathf.Sin(Mathf.PI * t);
                    output.Vertices.Add(center - tangent * width);
                    output.Vertices.Add(center + radial * fold);
                    output.Vertices.Add(center + tangent * width);
                }
            int surfaceVertices = (segments + 1) * 3;
            for (int row = 0; row < segments; row++)
                for (int column = 0; column < 2; column++)
                {
                    int a = start + row * 3 + column;
                    output.Quad(a, a + 3, a + 4, a + 1);
                    a += surfaceVertices;
                    output.Quad(a + 1, a + 4, a + 3, a);
                }
        }

        private static void Lathe(Geometry output, Vector2[] profile, int sides)
        {
            int start = output.Vertices.Count;
            foreach (var point in profile)
                for (int side = 0; side <= sides; side++)
                {
                    float angle = side * Mathf.PI * 2 / sides;
                    output.Vertices.Add(new Vector3(point.x * Mathf.Cos(angle), point.y, point.x * Mathf.Sin(angle)));
                }
            for (int row = 0; row < profile.Length - 1; row++)
                for (int side = 0; side < sides; side++)
                {
                    int a = start + row * (sides + 1) + side;
                    output.Quad(a, a + sides + 1, a + sides + 2, a + 1);
                }
        }

        private static void Disk(Geometry output, float radius, float y, int sides)
        {
            int center = output.Vertices.Count;
            output.Vertices.Add(new Vector3(0, y, 0));
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2 / sides;
                output.Vertices.Add(new Vector3(radius * Mathf.Cos(angle), y, radius * Mathf.Sin(angle)));
            }
            for (int side = 0; side < sides; side++)
            {
                output.Triangles.Add(center);
                output.Triangles.Add(center + 1 + (side + 1) % sides);
                output.Triangles.Add(center + 1 + side);
            }
        }

        private sealed class Geometry
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<int> Triangles = new List<int>();

            public void Quad(int a, int b, int c, int d)
            {
                Triangles.Add(a); Triangles.Add(b); Triangles.Add(c);
                Triangles.Add(a); Triangles.Add(c); Triangles.Add(d);
            }

            public Mesh ToMesh()
            {
                foreach (var v in Vertices)
                    if (!float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z))
                        throw new InvalidOperationException("A vegetação contém vértices não finitos.");
                var mesh = new Mesh { indexFormat = IndexFormat.UInt16 };
                mesh.SetVertices(Vertices);
                mesh.SetTriangles(Triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.isStatic = true;
            return go.transform;
        }

        private static void Render(Transform parent, string name, Mesh mesh, Material material)
        {
            var child = Child(parent, name);
            child.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Material LeafMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("O shader Standard não está disponível.");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.color = color;
            material.SetFloat("_Metallic", 0);
            material.SetFloat("_Glossiness", .19f);
            material.SetFloat("_Mode", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1);
            material.SetColor("_EmissionColor", Color.black);
            material.DisableKeyword("_EMISSION");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path = MeshFolder + "/LouvreGarden_" + name + ".asset";
            mesh.name = "LouvreGarden_" + name;
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            if (split <= 0) throw new InvalidOperationException("Pasta de assets inválida: " + path);
            string parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }
    }
}
