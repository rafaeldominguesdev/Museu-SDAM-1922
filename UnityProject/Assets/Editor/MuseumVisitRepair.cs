using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    [InitializeOnLoad]
    public static class MuseumVisitRepair
    {
        [MenuItem("Museum/Correcoes/Aplicar corpo luzes piso e movimento")]
        public static void Apply()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            foreach(var movement in UnityEngine.Object.FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None))
            {
                var so=new SerializedObject(movement);
                so.FindProperty("walkSpeed").floatValue=1.25f;
                so.FindProperty("acceleration").floatValue=14f;
                so.FindProperty("deceleration").floatValue=14f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var gyro in UnityEngine.Object.FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None))
            {
                var camera=gyro.GetComponent<Camera>(); camera.nearClipPlane=.1f; camera.farClipPlane=80f;
                EditorUtility.SetDirty(camera);
            }
            ConfigurePictureLights();
            var architecture=GameObject.Find("Museum_LouvreStyle").transform;
            RemoveLegacyMedallion(architecture);
            BuildMedallion(architecture);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Falha ao salvar correções.");
            Debug.Log("[VisitRepair] Cena salva: velocidade 1,25 m/s; câmera 0,1 m; medalhão sem sobreposição; focos reais.");
        }

        public static void ConfigurePictureLights()
        {
            var architecture=GameObject.Find("Museum_LouvreStyle");
            if(architecture==null) return;
            const string name="Focos_das_obras";
            var old=architecture.transform.Find(name);
            if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=new GameObject(name); root.transform.SetParent(architecture.transform,false);
            foreach(var exhibit in UnityEngine.Object.FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None))
            {
                if(exhibit.PaintingData==null || GameObject.Find("Moldura_perfilada_"+exhibit.PaintingData.uniqueId)==null) continue;
                var surface=exhibit.ArtworkRenderer!=null?exhibit.ArtworkRenderer:exhibit.GetComponent<Renderer>();
                if(surface==null) continue;
                var center=surface.bounds.center;
                var front=exhibit.transform.forward;
                var spot=new GameObject("Foco_"+exhibit.PaintingData.uniqueId).AddComponent<Light>();
                spot.transform.SetParent(root.transform,false);
                spot.transform.position=center+Vector3.up*(surface.bounds.extents.y+.43f)+front*.65f;
                spot.transform.LookAt(center+front*.025f);
                spot.type=LightType.Spot; spot.range=4f; spot.spotAngle=100f; spot.innerSpotAngle=65f;
                spot.color=new Color(1f,.94f,.83f); spot.intensity=MuseumPictureLighting.Intensity;
                spot.shadows=LightShadows.None; spot.renderMode=LightRenderMode.ForcePixel;
                spot.lightmapBakeType=LightmapBakeType.Realtime;
                spot.enabled=false;
            }
            root.AddComponent<MuseumPictureLighting>();
            Debug.Log("[VisitRepair] Focos instalados: "+root.transform.childCount+"; seleção dos três mais próximos.");
        }

        private static void RemoveLegacyMedallion(Transform architecture)
        {
            foreach(var name in new[]{"Salao_Calcario","Salao_Bronze_dourado","Salao_Verde_profundo"})
            {
                var filter=architecture.GetComponentsInChildren<MeshFilter>().FirstOrDefault(f=>f.name==name);
                if(filter==null) throw new InvalidOperationException("Malha ausente: "+name);
                var mesh=filter.sharedMesh; var vertices=mesh.vertices; var original=mesh.triangles;
                var kept=new List<int>();
                bool InDisk(Vector3 v)=>v.y>-.004f && v.y<.05f && new Vector2(v.x,v.z).sqrMagnitude<3.7f*3.7f;
                for(int i=0;i<original.Length;i+=3)
                    if(!(InDisk(vertices[original[i]]) && InDisk(vertices[original[i+1]]) && InDisk(vertices[original[i+2]])))
                        kept.AddRange(new[]{original[i],original[i+1],original[i+2]});
                if(kept.Count==original.Length) continue;
                mesh.triangles=kept.ToArray(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
                Debug.Log($"[VisitRepair] {name}: removidos {(original.Length-kept.Count)/3} triângulos do medalhão em camadas.");
            }
        }

        public static void BuildMedallion(Transform architecture)
        {
            const string name="Medalhao_continuo";
            var previous=architecture.Find(name);
            if(previous!=null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var vertices=new List<Vector3>(); var groups=new[]{new List<int>(),new List<int>(),new List<int>()};
            Vector3 Point(float a,float r)=>new Vector3(Mathf.Cos(a)*r,.024f,Mathf.Sin(a)*r);
            void Tri(Vector3 a,Vector3 b,Vector3 c,int material)
            {
                int start=vertices.Count; vertices.AddRange(new[]{a,b,c});
                groups[material].AddRange(new[]{start,start+2,start+1});
            }
            void Band(float a,float b,float r0,float r1,int material)
            {
                Tri(Point(a,r0),Point(a,r1),Point(b,r1),material);
                Tri(Point(a,r0),Point(b,r1),Point(b,r0),material);
            }
            const int segments=64;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                float Radius(int j)=>j%4==0?(j%8==0?2.8f:2.0f):.48f;
                float ra=Radius(i),rb=Radius((i+1)%segments);
                int spoke=i%4==3?(i+1)/4:i/4;
                int material=(i%4==0 || i%4==3)?(spoke%2==0?1:2):0;
                Tri(Point(a,0),Point(a,ra),Point(b,rb),material);
                Tri(Point(a,ra),Point(a,3.33f),Point(b,3.33f),0);
                Tri(Point(a,ra),Point(b,3.33f),Point(b,rb),0);
                Band(a,b,3.33f,3.36f,2); Band(a,b,3.36f,3.55f,0); Band(a,b,3.55f,3.6f,1);
            }
            var mesh=new Mesh {name=name}; mesh.SetVertices(vertices); mesh.subMeshCount=3;
            for(int i=0;i<3;i++) mesh.SetTriangles(groups[i],i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            const string path="Assets/MuseumStyle/Meshes/Medalhao_continuo.asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null) { AssetDatabase.CreateAsset(mesh,path); saved=mesh; }
            else { EditorUtility.CopySerialized(mesh,saved); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
            var go=new GameObject(name); go.transform.SetParent(architecture,false);
            go.AddComponent<MeshFilter>().sharedMesh=saved;
            var renderer=go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials=new[]{"Calcario","Bronze_dourado","Verde_profundo"}.Select(n=>AssetDatabase.LoadAssetAtPath<Material>("Assets/MuseumStyle/Materials/"+n+".mat")).ToArray();
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.lightProbeUsage=LightProbeUsage.Off;
        }
        static MuseumVisitRepair()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("VisitCapture", false)) return;
                SessionState.SetBool("VisitCapture", false);
                new GameObject("VisitRuntimeValidation").AddComponent<VisitRuntimeValidation>();
            };
        }
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            SessionState.SetBool("VisitCapture", true);
            EditorApplication.isPlaying = true;
        }
        public static void Audit()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            foreach (var root in scene.GetRootGameObjects())
                Debug.Log($"[VisitAudit] ROOT {root.name} active={root.activeSelf}");
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Debug.Log($"[VisitAudit] LIGHT {light.name} active={light.isActiveAndEnabled} type={light.type} position={light.transform.position} intensity={light.intensity} shadows={light.shadows}");
            foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var renderer = filter.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled || filter.sharedMesh == null) continue;
                var floor = filter.sharedMesh.vertices.Select(v => filter.transform.TransformPoint(v)).Where(v => Math.Abs(v.y) < .06f).ToArray();
                if (floor.Length < 3) continue;
                Debug.Log($"[VisitAudit] FLOOR {filter.name} mesh={filter.sharedMesh.name} shadows={renderer.shadowCastingMode} lightmap={renderer.lightmapIndex} heights={string.Join(",", floor.Select(v => v.y.ToString("F5")).Distinct().Take(20))} vertices={floor.Length}");
            }
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Debug.Log($"[VisitAudit] CAMERA {camera.name} active={camera.isActiveAndEnabled} near={camera.nearClipPlane}");
            var movement = new SerializedObject(UnityEngine.Object.FindAnyObjectByType<HeadGazeMovement>());
            Debug.Log($"[VisitAudit] MOVEMENT speed={movement.FindProperty("walkSpeed").floatValue} acceleration={movement.FindProperty("acceleration").floatValue}");
        }
    }
}
