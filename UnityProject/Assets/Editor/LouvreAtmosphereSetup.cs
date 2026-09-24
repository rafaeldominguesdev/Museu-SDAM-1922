using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Ilhas de exposição e pontos de pausa, gerados e salvos antes da visita.</summary>
    public static class LouvreAtmosphereSetup
    {
        private const string Folder = "Assets/MuseumStyle";
        private static Material stone, gold, dark, wine, paper, glass;
        private static readonly HashSet<Mesh> transientMeshes = new HashSet<Mesh>();

        [MenuItem("Museum/Visual/Preencher ambiente com ilhas de exposição")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            var architecture = GameObject.Find("Museum_LouvreStyle");
            if (architecture == null) throw new InvalidOperationException("A arquitetura do museu precisa estar instalada.");
            ApplyTo(architecture.transform);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Falha ao salvar as ilhas de exposição.");
        }

        public static void ApplyTo(Transform architecture)
        {
            transientMeshes.Clear();
            var previous = architecture.Find("Ambiente_Preenchido");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var root = Child(architecture, "Ambiente_Preenchido");
            Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/" + name + ".mat");
            stone = Load("Calcario"); gold = Load("Ouro_patina"); dark = Load("Verde_profundo");
            wine = Load("Veludo_vinho"); paper = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Guide_Papel.mat");
            glass = Glass();
            if (architecture.GetComponent<MuseumWorldText>() == null) architecture.gameObject.AddComponent<MuseumWorldText>();

            Screen(root, "01", new Vector3(-3.3f, 0, 16.6f), -12, "homem_amarelo", "estudante_russa");
            Screen(root, "02", new Vector3(3.3f, 0, 16.6f), 12, "di_cavalcanti_pierrot", "tropical");
            Case(root, "Salao_01", new Vector3(-5, 0, 1.6f), -12, 0);
            Case(root, "Salao_02", new Vector3(5, 0, -1.6f), 12, 1);
            Case(root, "Literatura_01", new Vector3(-18.8f, 0, -3.4f), -8, 2);
            Case(root, "Literatura_02", new Vector3(-20.5f, 0, 3.8f), 8, 3);
            Bench(root, "Escultura", new Vector3(18.5f, 0, 4.3f), 90);
            Bench(root, "Literatura", new Vector3(-17, 0, -5.2f), 180);
            LouvreGardenDetails.Apply(root, stone, gold, dark);
            MuseumAmbientDetails.ApplyTo(architecture);
            Debug.Log("[LouvreAtmosphere] Salvos: 2 painéis de duas faces, 4 vitrines, 2 bancos, 8 vasos e sombras de contato. 28 fichas preservadas, 8 referências adicionais pelo olhar.");
        }

        private static PaintingExhibit Exhibit(string id) => UnityEngine.Object.FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None)
            .Single(exhibit => exhibit.PaintingData != null && exhibit.PaintingData.uniqueId == id);

        private static void Screen(Transform parent, string id, Vector3 position, float yaw, string frontId, string backId)
        {
            var screen = Child(parent, "Painel_curvo_" + id);
            screen.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var slab = RoundedBlock(3.2f, 2.6f, .18f, .085f);
            Bend(slab, 7, true);
            var saved = SaveMesh("Painel_curvo", slab);
            MeshObject(screen, "Parede_vinho_curva", saved, wine).transform.localPosition = Vector3.up * 1.4f;
            var collision = Child(screen, "Colisao_curva"); collision.localPosition = Vector3.up * 1.4f;
            collision.gameObject.AddComponent<MeshCollider>().sharedMesh = saved;
            var baseMesh = RoundedBlock(3.35f, .10f, .55f, .11f); Bend(baseMesh, 7, false);
            MeshObject(screen, "Soclo_curvo", SaveMesh("Base_painel", baseMesh), dark).transform.localPosition = Vector3.up * .055f;
            var trim = RoundedBlock(3.36f, .02f, .56f, .11f); Bend(trim, 7, false);
            MeshObject(screen, "Filete_bronze", SaveMesh("Friso_painel", trim), gold).transform.localPosition = Vector3.up * .108f;
            var foot = screen.gameObject.AddComponent<BoxCollider>();
            foot.center = new Vector3(0, .06f, .12f); foot.size = new Vector3(3.3f, .12f, .62f);
            Face(screen, frontId, new Vector3(0, 1.56f, .24f), 0);
            Face(screen, backId, new Vector3(0, 1.56f, -.14f), 180);
            Combine(screen, false);
        }

        private static void Face(Transform panel, string exhibitId, Vector3 center, float yaw)
        {
            var source = Exhibit(exhibitId);
            var face = Child(panel, "Estudo_dupla_face_" + exhibitId);
            face.localPosition = center; face.localRotation = Quaternion.Euler(0, yaw, 0);
            const float width = 2.2f, height = 1.73f;
            face.gameObject.AddComponent<ExhibitGuideReference>().SetSource(source);
            var collider = face.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 0, .02f); collider.size = new Vector3(width, height, .05f);
            MeshObject(face, "Tela_cenografica", SaveMesh("Tela_ilha", RoundedBlock(width, height, .035f, .025f)), paper);
            bool reproduction = ArtworkTextureSetup.TryApplyFace(face, source, width, height);
            if (!reproduction)
            {
                var legacy = GameObject.Find("Expografia_" + exhibitId);
                var sourceArt = legacy != null ? legacy.transform.Find("Composicao_cenografica") : null;
                if (sourceArt == null) throw new InvalidOperationException("Composição ausente: " + exhibitId);
                var art = UnityEngine.Object.Instantiate(sourceArt.gameObject, face).transform;
                art.name = "Composicao_complementar";
                art.localPosition = new Vector3(0, 0, .025f); art.localRotation = Quaternion.identity;
                art.localScale = Vector3.Scale(sourceArt.localScale, new Vector3(width / 2.38f, height / 1.87f, 1));
                foreach (var c in art.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            }
            // A moldura secundária tem perfil fino; as molduras de gala continuam nas paredes.
            MeshObject(face, "Moldura_bronze", SaveMesh("Moldura_ilha", Frame(width, height)), gold);
            Text(face, "COR / FIGURA / RUPTURA", new Vector3(0, 1.06f, .024f), .050f, new Color(.94f, .80f, .48f));
            Text(face, source.PaintingData.title + " • " + source.PaintingData.artist,
                new Vector3(0, -1.055f, .025f), .038f, new Color(.98f, .95f, .85f));
            Text(face, reproduction ? "REPRODUÇÃO DIGITAL  /  OLHE PARA LER A FICHA" : "ESTUDO CENOGRÁFICO  /  OLHE PARA LER A FICHA",
                new Vector3(0, -1.18f, .025f), .028f, new Color(.96f, .80f, .52f));
            Combine(face, true);
        }

        private static void Case(Transform parent, string id, Vector3 position, float yaw, int variant)
        {
            var display = Child(parent, "Vitrine_ilha_" + id);
            display.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            display.gameObject.AddComponent<ExhibitGuideReference>().SetSource(Exhibit("vitrine_documentos"));
            var collider = display.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .625f, 0); collider.size = new Vector3(2.4f, 1.25f, 1.0f);
            Block(display, "Pe_recuado", new Vector3(0, .09f, 0), new Vector3(2.05f, .18f, .74f), .16f, dark);
            Block(display, "Corpo_calcario", new Vector3(0, .47f, 0), new Vector3(2.22f, .70f, .86f), .15f, stone);
            Block(display, "Cinta_inferior", new Vector3(0, .22f, 0), new Vector3(2.25f, .035f, .89f), .15f, gold);
            Block(display, "Mesa_bronze", new Vector3(0, .87f, 0), new Vector3(2.4f, .08f, 1), .14f, gold);
            Block(display, "Forro_vinho", new Vector3(0, .914f, 0), new Vector3(2.28f, .012f, .88f), .12f, wine);
            // Bordas finas, quatro montantes e tampa transparente: os livros permanecem visíveis.
            foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                Block(display, "Montante_bronze", new Vector3(x * 1.11f, 1.08f, z * .405f), new Vector3(.018f, .32f, .018f), .005f, gold);
            foreach (int side in new[] { -1, 1 })
            {
                Block(display, "Vidro_longo", new Vector3(0, 1.08f, side * .42f), new Vector3(2.26f, .32f, .005f), .002f, glass);
                Block(display, "Vidro_curto", new Vector3(side * 1.13f, 1.08f, 0), new Vector3(.005f, .32f, .84f), .002f, glass);
            }
            Block(display, "Tampa_vidro", new Vector3(0, 1.245f, 0), new Vector3(2.28f, .009f, .86f), .02f, glass);
            Book(display, new Vector3(-.72f, .95f, .02f), -12 + variant * 4, false, variant % 2 == 0 ? dark : wine);
            Book(display, new Vector3(.02f, .95f, .01f), 7 - variant * 3, true, wine);
            Book(display, new Vector3(.76f, .945f, 0), 13, false, dark);
            var label = Child(display, "Legenda_documentos");
            label.localPosition = new Vector3(0, .61f, -.438f); label.localRotation = Quaternion.Euler(0, 180, 0);
            Text(label, new[] { "UMA SEMANA, MUITAS VOZES", "O ENCONTRO DAS ARTES", "A PALAVRA IMPRESSA", "DOCUMENTOS DE UMA ÉPOCA" }[variant], Vector3.zero, .053f, new Color(1f, .94f, .80f));
            Text(label, "LIVROS E IMPRESSOS CENOGRÁFICOS", new Vector3(0, -.12f, .001f), .033f, new Color(.96f, .80f, .52f));
            Combine(display, true);
        }

        private static void Book(Transform parent, Vector3 position, float yaw, bool open, Material cover)
        {
            var book = Child(parent, "Livro_cenografico"); book.localPosition = position; book.localRotation = Quaternion.Euler(0, yaw, 0);
            float width = open ? .65f : .46f;
            Block(book, "Capa", Vector3.zero, new Vector3(width, .055f, .52f), .015f, cover);
            Block(book, "Miolo", new Vector3(0, .035f, 0), new Vector3(width - .035f, .035f, .48f), .009f, paper);
            if (open)
            {
                Block(book, "Lombada", new Vector3(0, .056f, 0), new Vector3(.014f, .01f, .46f), .003f, cover);
                foreach (int side in new[] { -1, 1 })
                    for (int line = 0; line < 7; line++)
                        Block(book, "Linha_impressa", new Vector3(side * .165f, .056f, -.16f + line * .048f),
                            new Vector3(line == 6 ? .13f : .23f, .002f, .004f), .001f, dark);
            }
            else
            {
                Block(book, "Capa_superior", new Vector3(0, .056f, 0), new Vector3(width, .01f, .52f), .015f, cover);
                var title = Child(book, "Titulo_capa"); title.localPosition = new Vector3(0, .064f, 0);
                title.localRotation = Quaternion.Euler(-90, 0, 180);
                Text(title, "SEMANA\nDE ARTE\n1922", Vector3.zero, .043f, new Color(.95f, .83f, .52f));
            }
        }

        private static void Bench(Transform parent, string id, Vector3 position, float yaw)
        {
            var original = GameObject.Find("Banco_curvo_5_5");
            if (original == null) throw new InvalidOperationException("Banco curvo original ausente.");
            var bench = UnityEngine.Object.Instantiate(original, parent).transform;
            bench.name = "Banco_curvo_Ilha_" + id;
            bench.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0)); bench.localScale = Vector3.one * .72f;
        }

        private static void Text(Transform parent, string value, Vector3 position, float size, Color color)
        {
            var t = Child(parent, "Texto"); t.localPosition = position; t.localRotation = Quaternion.Euler(0, 180, 0);
            var text = t.gameObject.AddComponent<TextMesh>();
            text.text = value; text.fontSize = 80; text.characterSize = size * .25f;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = color;
        }

        private static void Block(Transform parent, string name, Vector3 position, Vector3 size, float radius, Material material)
        {
            // As dimensões recorrentes reutilizam o mesmo asset; a malha final de cada ilha é agrupada.
            var mesh = size.y <= .003f ? Finish(new List<Vector3>
            {
                new Vector3(-size.x/2,size.y/2,-size.z/2),new Vector3(-size.x/2,size.y/2,size.z/2),
                new Vector3(size.x/2,size.y/2,size.z/2),new Vector3(size.x/2,size.y/2,-size.z/2)
            }, new List<int>{0,1,2,0,2,3}) : RoundedBlock(size.x, size.y, size.z, radius);
            var go = MeshObject(parent, name, mesh, material);
            go.transform.localPosition = position;
        }

        private static Mesh RoundedBlock(float width, float height, float depth, float radius)
        {
            // Contorno arredondado no plano XZ, extrudado verticalmente.
            radius = Mathf.Min(radius, Mathf.Min(width, depth) * .5f);
            var outline = new List<Vector2>();
            int segments = radius <= .02f ? 2 : 4;
            for (int corner = 0; corner < 4; corner++)
                for (int j = 0; j <= segments; j++)
                {
                    float angle = (corner * 90f + j * 90f / segments) * Mathf.Deg2Rad;
                    float x = (corner == 0 || corner == 3 ? 1 : -1) * (width / 2 - radius);
                    float z = (corner < 2 ? 1 : -1) * (depth / 2 - radius);
                    outline.Add(new Vector2(x + Mathf.Cos(angle) * radius, z + Mathf.Sin(angle) * radius));
                }
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int side = 0; side < 2; side++)
                foreach (var p in outline) vertices.Add(new Vector3(p.x, (side == 0 ? -.5f : .5f) * height, p.y));
            int n = outline.Count;
            for (int j = 0; j < n; j++)
            {
                int k = (j + 1) % n;
                Quad(triangles, j, j + n, k + n, k);
            }
            for (int side = 0; side < 2; side++)
            {
                int center = vertices.Count; vertices.Add(Vector3.up * (side == 0 ? -.5f : .5f) * height);
                int start = vertices.Count;
                foreach (var p in outline) vertices.Add(new Vector3(p.x, (side == 0 ? -.5f : .5f) * height, p.y));
                for (int j = 0; j < n; j++)
                {
                    triangles.Add(center); triangles.Add(start + (side == 0 ? j : (j + 1) % n));
                    triangles.Add(start + (side == 0 ? (j + 1) % n : j));
                }
            }
            return Finish(vertices, triangles);
        }

        private static Mesh Frame(float width, float height)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            var profile = new[] { new Vector2(.005f, .02f), new Vector2(.018f, .062f), new Vector2(.045f, .068f), new Vector2(.065f, .04f), new Vector2(.075f, -.02f) };
            foreach (var p in profile)
                foreach (var xy in new[] { new Vector2(1,1),new Vector2(-1,1),new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1) })
                    v.Add(new Vector3(xy.x * (width / 2 + p.x), xy.y * (height / 2 + p.x), p.y));
            for (int row = 0; row < profile.Length - 1; row++) for (int side = 0; side < 4; side++)
            { int a = row * 5 + side; Quad(t, a, a + 5, a + 6, a + 1); }
            return Finish(v, t);
        }

        private static void Bend(Mesh mesh, float radius, bool crown)
        {
            // A curvatura depende somente de X: fatias verticais evitam subdividir
            // também a altura e multiplicar triângulos nas bases e nos frisos.
            const float sliceWidth = .20f;
            var source = mesh.vertices; var indices = mesh.triangles;
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i < indices.Length; i += 3)
            {
                var original = new List<Vector3> { source[indices[i]], source[indices[i + 1]], source[indices[i + 2]] };
                int first = Mathf.FloorToInt(original.Min(p => p.x) / sliceWidth);
                int last = Mathf.FloorToInt(original.Max(p => p.x) / sliceWidth);
                for (int slice = first; slice <= last; slice++)
                {
                    var polygon = ClipX(ClipX(original, slice * sliceWidth, true), (slice + 1) * sliceWidth, false);
                    for (int j = 1; j < polygon.Count - 1; j++)
                    {
                        if (Vector3.Cross(polygon[j] - polygon[0], polygon[j + 1] - polygon[0]).sqrMagnitude < 1e-12f) continue;
                        foreach (var point in new[] { polygon[0], polygon[j], polygon[j + 1] })
                        {
                            var curved = point;
                            curved.x = Mathf.Sin(point.x / radius) * radius;
                            curved.z += radius * (1 - Mathf.Cos(point.x / radius));
                            if (crown && point.y > 0) curved.y += .12f * Mathf.Cos(point.x / 3.2f * Mathf.PI);
                            triangles.Add(vertices.Count); vertices.Add(curved);
                        }
                    }
                }
            }
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }

        private static List<Vector3> ClipX(List<Vector3> polygon, float limit, bool keepGreater)
        {
            var result = new List<Vector3>();
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count];
                bool insideA = keepGreater ? a.x >= limit : a.x <= limit;
                bool insideB = keepGreater ? b.x >= limit : b.x <= limit;
                if (insideA) result.Add(a);
                if (insideA != insideB) result.Add(Vector3.Lerp(a, b, (limit - a.x) / (b.x - a.x)));
            }
            return result;
        }

        private static void Combine(Transform group, bool descendants)
        {
            var filters = group.GetComponentsInChildren<MeshFilter>().Where(f => f.GetComponent<MeshRenderer>() != null &&
                (descendants || f.transform.parent == group)).ToArray();
            foreach (var set in filters.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial))
            {
                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(set.Select(f => new CombineInstance { mesh=f.sharedMesh, transform=group.worldToLocalMatrix*f.transform.localToWorldMatrix }).ToArray(),true,true);
                MeshObject(group, set.Key.name + "_agrupado", SaveMesh(group.name + "_" + set.Key.name, mesh), set.Key);
            }
            foreach (var f in filters)
            {
                // Textos permanecem na hierarquia; somente os componentes geométricos são removidos.
                var temporary = f.sharedMesh;
                UnityEngine.Object.DestroyImmediate(f.GetComponent<MeshRenderer>());
                UnityEngine.Object.DestroyImmediate(f);
                if (transientMeshes.Remove(temporary) && !AssetDatabase.Contains(temporary)) UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        private static void Quad(List<int> t, int a, int b, int c, int d) { t.Add(a);t.Add(b);t.Add(c);t.Add(a);t.Add(c);t.Add(d); }
        private static Mesh Finish(List<Vector3> v, List<int> t)
        {
            if (v.Any(p=>!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z))) throw new InvalidOperationException("Geometria inválida na ilha.");
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();transientMeshes.Add(mesh);return mesh;
        }
        private static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path=Folder+"/Meshes/LouvreIsland_"+name+".asset";mesh.name=name;
            var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(previous==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
            EditorUtility.CopySerialized(mesh,previous);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(previous);return previous;
        }
        private static Transform Child(Transform parent, string name)
        { var go=new GameObject(name);go.transform.SetParent(parent,false);go.isStatic=true;return go.transform; }
        private static GameObject MeshObject(Transform parent,string name,Mesh mesh,Material material)
        {
            var t=Child(parent,name);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            return t.gameObject;
        }
        private static Material Glass()
        {
            const string path=Folder+"/Materials/Vidro_vitrine_ilha.mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=new Color(.72f,.86f,.84f,.085f);m.SetFloat("_Metallic",.1f);m.SetFloat("_Glossiness",.55f);
            m.SetFloat("_Mode",3);m.SetInt("_SrcBlend",(int)BlendMode.One);m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite",0);m.EnableKeyword("_ALPHAPREMULTIPLY_ON");m.renderQueue=3000;EditorUtility.SetDirty(m);return m;
        }
    }
}
