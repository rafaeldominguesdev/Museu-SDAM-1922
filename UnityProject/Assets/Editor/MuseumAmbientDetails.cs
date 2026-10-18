using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Expografia leve, persistida e reaplicável, sem execução por frame.</summary>
    public static class MuseumAmbientDetails
    {
        public const string RootName = "Ambientacao_Museologica";
        private const string Folder = "Assets/MuseumStyle/";

        [MenuItem("Museum/Visual/Aplicar cordoes e luz de cornija")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            var architecture = GameObject.Find("Museum_LouvreStyle");
            if (architecture == null) throw new InvalidOperationException("Arquitetura ausente.");
            ApplyTo(architecture.transform);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Falha ao salvar ambientação.");
            Debug.Log("[MuseumAmbient] Cena salva: cordões em todas as obras de parede; vasos sem rampa; faixas decorativas de brilho retiradas.");
        }

        public static void ApplyTo(Transform architecture)
        {
            var exhibits = UnityEngine.Object.FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None)
                .Where(e => e.PaintingData != null).ToDictionary(e => e.PaintingData.uniqueId);
            var groups = WallGroups(exhibits.Values).ToArray();
            var old = architecture.Find(RootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root = Child(architecture, RootName);
            var bronze = Material("Barreira_Bronze", "Standard", new Color(.25f, .16f, .075f), .65f, .32f);
            var cord = Material("Barreira_Veludo", "Standard", new Color(.18f, .022f, .035f), 0, .16f);
            var post = Save("Poste", Lathe(new[]
            {
                new Vector2(0,.025f), new Vector2(.18f,.025f), new Vector2(.19f,.045f),
                new Vector2(.18f,.072f), new Vector2(.13f,.095f), new Vector2(.065f,.13f),
                new Vector2(.04f,.18f), new Vector2(.027f,.72f), new Vector2(.036f,.75f),
                new Vector2(.036f,.78f), new Vector2(.027f,.80f), new Vector2(.036f,.814f),
                new Vector2(.049f,.825f), new Vector2(.057f,.843f), new Vector2(.061f,.865f),
                new Vector2(.057f,.887f), new Vector2(.049f,.905f), new Vector2(.036f,.918f),
                new Vector2(.018f,.927f), new Vector2(0,.93f)
            }, 12));
            for (int i = 0; i < groups.Length; i++)
            {
                var selected = groups[i];
                var front = selected[0].transform.forward;
                var right = selected[0].transform.right;
                var origin = selected[0].transform.position;
                origin.y = 0;
                var positions = selected.Select(e => Vector3.Dot(e.transform.position - origin, right)).ToArray();
                float min = positions.Min() - 1.56f, max = positions.Max() + 1.56f;
                var group = Child(root, "Cordao_" + i);
                group.SetPositionAndRotation(origin + front * .95f, Quaternion.LookRotation(front));
                var posts = new List<float> { min };
                // Supports between artworks, never directly in front of a credit or face.
                var orderedPositions=positions.OrderBy(p=>p).ToArray();
                for(int j=1;j<orderedPositions.Length;j++)posts.Add((orderedPositions[j-1]+orderedPositions[j])*.5f);
                posts.Add(max);
                for (int j = 0; j < posts.Count; j++)
                {
                    var support = Child(group, "Poste_cordao_" + j);
                    support.localPosition = Vector3.right * posts[j];
                    Render(support, "Bronze", post, bronze);
                    var col = support.gameObject.AddComponent<CapsuleCollider>();
                    col.center = Vector3.up * .48f; col.height = .96f; col.radius = .19f;
                }
                var vertices = new List<Vector3>(); var indices = new List<int>();
                for (int j = 0; j < posts.Count - 1; j++)
                {
                    Rope(vertices, indices, new Vector3(posts[j], .84f, 0), new Vector3(posts[j + 1], .84f, 0), .14f);
                    var col = Child(group, "Limite_de_aproximacao_" + j).gameObject.AddComponent<BoxCollider>();
                    col.center = new Vector3((posts[j] + posts[j + 1]) * .5f, .46f, 0);
                    col.size = new Vector3(posts[j + 1] - posts[j], .92f, .055f);
                }
                // Retornos discretos fecham a aproximação lateral até a parede.
                foreach (float end in new[] { min, max })
                {
                    Rope(vertices, indices, new Vector3(end,.84f,0), new Vector3(end,.84f,-.88f), .055f);
                    var col = Child(group, "Retorno_" + (end == min ? "A" : "B")).gameObject.AddComponent<BoxCollider>();
                    col.center = new Vector3(end,.46f,-.44f); col.size = new Vector3(.055f,.92f,.88f);
                }
                Render(group, "Cordas", Save("Cordas_" + i, Mesh(vertices, indices)), cord);
                Debug.Log("[MuseumAmbient] " + group.name + ": " + string.Join(",", selected.Select(e=>e.PaintingData.uniqueId)));
            }
            // Alguns quadros texturizados ainda tinham a geometria demonstrativa por cima.
            // Só oculta essa geometria quando a pintura já possui uma imagem real.
            foreach (var entry in ArtworkTextureSetup.Read())
            {
                if (!exhibits.TryGetValue(entry.exhibitId,out var exhibit)) continue;
                var renderer=exhibit.GetComponent<Renderer>();
                var display=GameObject.Find("Expografia_"+entry.exhibitId);
                var composition=display != null ? display.transform.Find("Composicao_cenografica") : null;
                if (composition != null && renderer != null && renderer.sharedMaterial != null &&
                    (renderer.sharedMaterial.mainTexture != null || exhibit.PaintingData.paintingTexture != null))
                    composition.gameObject.SetActive(false);
            }
            // A planta temática usa MuseumLightingSetup (Spotlight_ com sombras).
            // Não reinstale o conjunto legado Focos_das_obras nesta passagem.
            ArtworkTextureSetup.ApplyDisplayLightingToScene(configureLegacyPictureLights: false);
            // Afasta os vasos da entrada das bases dos novos cordões laterais.
            var environment = architecture.Find("Ambiente_Preenchido");
            // Cenas recentes podem ter apenas a arquitetura, sem as ilhas antigas.
            // Instala somente os vasos, sem restaurar móveis removidos pelo usuário.
            var decoration = environment != null ? environment : root;
            if (decoration.Find("Vegetacao_de_galeria") == null)
            {
                Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>(Folder+"Materials/"+name+".mat");
                LouvreGardenDetails.Apply(decoration, Load("Calcario"), bronze, Load("Verde_profundo"));
            }
            var garden = decoration.Find("Vegetacao_de_galeria");
            foreach (Transform vase in garden)
            {
                LouvreGardenDetails.ConfigureCollider(vase);
                if (vase.name == "Vaso_galeria_05" || vase.name == "Vaso_galeria_06")
                    vase.position = new Vector3(Mathf.Sign(vase.position.x) * 6.15f, 0, 10.85f);
            }
            LouvreContactShadows.Apply(decoration);
        }

        private static IEnumerable<PaintingExhibit[]> WallGroups(IEnumerable<PaintingExhibit> exhibits)
        {
            // Só obras com moldura de parede. Não cerca esculturas, vitrines ou portas.
            var walls = exhibits.Where(e => GameObject.Find("Moldura_perfilada_" + e.PaintingData.uniqueId) != null)
                .GroupBy(e => new Vector3(Mathf.Round(e.transform.forward.x), Mathf.Round(e.transform.forward.z),
                    Mathf.Round(Vector3.Dot(e.transform.position, e.transform.forward) * 10f)));
            foreach (var wall in walls)
            {
                var right = wall.First().transform.right;
                var ordered = wall.OrderBy(e => Vector3.Dot(e.transform.position, right)).ToArray();
                var group = new List<PaintingExhibit>();
                foreach (var exhibit in ordered)
                {
                    if (group.Count > 0 && Vector3.Dot(exhibit.transform.position - group[group.Count - 1].transform.position, right) > 4.2f)
                    { yield return group.ToArray(); group.Clear(); }
                    group.Add(exhibit);
                }
                if (group.Count > 0) yield return group.ToArray();
            }
        }

        private static Mesh Lathe(Vector2[] profile, int sides)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            foreach (var point in profile) for (int side = 0; side <= sides; side++)
            {
                float a = side * Mathf.PI * 2 / sides;
                v.Add(new Vector3(Mathf.Cos(a)*point.x,point.y,Mathf.Sin(a)*point.x));
            }
            for (int row = 0; row < profile.Length - 1; row++) for (int side = 0; side < sides; side++)
            { int a = row * (sides+1)+side; Quad(t,a,a+sides+1,a+sides+2,a+1); }
            return Mesh(v,t);
        }

        private static void Rope(List<Vector3> v, List<int> t, Vector3 from, Vector3 to, float sag)
        {
            const int segments = 24, sides = 8;
            int start = v.Count;
            var across = Vector3.Cross((to-from).normalized, Vector3.up).normalized;
            for (int i = 0; i <= segments; i++)
            {
                float u = i/(float)segments;
                var center = Vector3.Lerp(from,to,u) - Vector3.up * (4*sag*u*(1-u));
                var tangent = ((to-from)-Vector3.up*(4*sag*(1-2*u))).normalized;
                var up = Vector3.Cross(across,tangent).normalized;
                for (int side = 0; side <= sides; side++)
                {
                    float a = side*Mathf.PI*2/sides;
                    v.Add(center + .022f*(across*Mathf.Cos(a)+up*Mathf.Sin(a)));
                }
            }
            for (int i = 0; i < segments; i++) for (int s = 0; s < sides; s++)
            { int a = start+i*(sides+1)+s; Quad(t,a,a+sides+1,a+sides+2,a+1); }
        }

        private static void Quad(List<int> t,int a,int b,int c,int d) { t.AddRange(new[]{a,b,c,a,c,d}); }
        private static Mesh Mesh(List<Vector3> v,List<int> t)
        { var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t,0); m.RecalculateNormals(); m.RecalculateBounds(); return m; }
        private static Mesh Save(string name,Mesh mesh)
        {
            string path = Folder+"Meshes/MuseumAmbient_"+name+".asset"; mesh.name = "MuseumAmbient_"+name;
            var previous = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (previous == null) { AssetDatabase.CreateAsset(mesh,path); return mesh; }
            EditorUtility.CopySerialized(mesh,previous); previous.UploadMeshData(false);
            UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(previous); return previous;
        }
        private static Material Material(string name,string shader,Color color,float metal,float gloss)
        {
            string path = Folder+"Materials/"+name+".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var s = Shader.Find(shader); if (s == null) throw new InvalidOperationException("Shader ausente: "+shader);
            if (m == null) { m = new Material(s); AssetDatabase.CreateAsset(m,path); }
            m.shader = s;
            if (m.HasProperty("_Color")) m.color=color;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic",metal);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness",gloss);
            m.enableInstancing=true; EditorUtility.SetDirty(m); return m;
        }
        private static Transform Child(Transform parent,string name)
        { var t = new GameObject(name).transform; t.SetParent(parent,false); return t; }
        private static void Render(Transform parent,string name,Mesh mesh,Material material)
        {
            var go = Child(parent,name).gameObject;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.AddComponent<MeshRenderer>(); r.sharedMaterial=material;
            r.shadowCastingMode=ShadowCastingMode.Off; r.lightProbeUsage=LightProbeUsage.Off;
            // Novos detalhes não dependem do bake de oclusão da arquitetura existente.
            r.allowOcclusionWhenDynamic=false;
        }
    }
}
