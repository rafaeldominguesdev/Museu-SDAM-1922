#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MuseumModerna
{
    public sealed class VisitRuntimeValidation : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (int i=0;i<10;i++) yield return null;
            try { Validate(); }
            catch (Exception e) { Debug.LogException(e); UnityEditor.EditorApplication.Exit(1); yield break; }
            // Permite liberar as texturas e objetos Destroy antes de encerrar o Editor.
            yield return null;
            yield return null;
            UnityEditor.EditorApplication.Exit(0);
        }
        private void Validate()
        {
            Time.captureDeltaTime=1f/60f;
            MobileVrMode.Instance.EnterNormalMode();
            var player=FindAnyObjectByType<PlayerController>();
            var eye=player.GetComponentInChildren<Camera>();
            foreach(var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) c.enabled=false;
            foreach(var c in FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None)) c.enabled=false;
            foreach(var c in FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None)) c.enabled=false;
            foreach(var c in FindObjectsByType<GazeDwellInteraction>(FindObjectsSortMode.None)) c.enabled=false;
            foreach(var c in FindObjectsByType<MuseumDayNightCycle>(FindObjectsSortMode.None)) c.enabled=false;
            Debug.Log($"[VisitValidation] scenes={SceneManager.sceneCount} museums={FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Museum_LouvreStyle")} cameras={FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.enabled)} near={eye.nearClipPlane}");
            Require(SceneManager.sceneCount==1 && FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Museum_LouvreStyle")==1 &&
                FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.enabled)==1,"Cena, arquitetura e câmera sem duplicação em runtime");
            Require(eye.nearClipPlane>=.1f,"Entrar no modo normal preserva a precisão do depth buffer");
            var medal=GameObject.Find("Medalhao_continuo").GetComponent<MeshFilter>().sharedMesh;
            Require(medal.vertices.All(v=>Mathf.Abs(v.y-.024f)<.00001f) && medal.normals.All(n=>n.y>.99f),
                "Estrela e anéis integram um plano contínuo com normais para cima");
            // Exercita no Editor a mesma política aplicada por Awake no APK.
            var cycle = FindAnyObjectByType<MuseumDayNightCycle>();
            typeof(MuseumDayNightCycle).GetMethod("ConfigureSunShadows",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(cycle, new object[] { RuntimePlatform.Android });
            cycle.SendMessage("Update");
            MobileVrMode.Instance.EnterEditorPreview();
            MobileVrMode.Instance.EnterNormalMode();
            Require(FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(l=>l.isActiveAndEnabled && l.type==LightType.Directional)
                .All(l=>l.shadows==LightShadows.None),
                "Política Android mantém sol sem projeção de sombra após ciclo e troca de modo");
            Require(FindObjectsByType<Renderer>(FindObjectsSortMode.None).Any(r=>
                r.enabled && r.sharedMaterial!=null && r.sharedMaterial.shader.name=="Museum/Contact Shadow"),
                "Sombras de contato dos móveis permanecem ativas");
            player.GetComponent<PlayerBodyVisual>().enabled=false;
            var reflection = RenderSettings.customReflectionTexture as Cubemap;
            Require(reflection != null && reflection.mipmapCount > 1,
                "Reflexo ambiente possui níveis de filtragem");
            var reference = reflection.GetPixel(CubemapFace.PositiveY,0,0);
            bool uniform = true;
            for(int mip=0;mip<reflection.mipmapCount;mip++)
                for(int face=0;face<6;face++)
                    foreach(var pixel in reflection.GetPixels((CubemapFace)face,mip))
                        uniform &= Mathf.Abs(pixel.r-reference.r)<.005f && Mathf.Abs(pixel.g-reference.g)<.005f &&
                            Mathf.Abs(pixel.b-reference.b)<.005f;
            Require(uniform,"Reflexo não contém face clara nem bordas quadradas em nenhuma direção ou nível");
            if (Environment.GetEnvironmentVariable("MUSEUM_VERIFY_FLOOR") == "1")
            {
                var positions = new[] { new Vector3(0,1.65f,15), new Vector3(2,1.65f,15), new Vector3(0,1.65f,0) };
                for(int i=0;i<positions.Length;i++)
                    Shot(eye,"piso-"+i+"-corrigido",positions[i],positions[i]+Quaternion.Euler(75,0,0)*Vector3.forward);
            }
            if (Environment.GetEnvironmentVariable("MUSEUM_FLOOR_DIAGNOSTIC") == "1")
            {
                float intensity = RenderSettings.reflectionIntensity;
                var positions = new[] { new Vector3(0,1.65f,15), new Vector3(2,1.65f,15), new Vector3(0,1.65f,0) };
                for (int i=0;i<positions.Length;i++)
                {
                    var target = positions[i] + Quaternion.Euler(75,0,0)*Vector3.forward;
                    RenderSettings.reflectionIntensity = intensity;
                    Shot(eye,"piso-"+i+"-reflexo",positions[i],target);
                    RenderSettings.reflectionIntensity = 0;
                    Shot(eye,"piso-"+i+"-sem-reflexo",positions[i],target);
                }
                RenderSettings.reflectionIntensity = intensity;
                return;
            }
            Shot(eye,"galeria",new Vector3(0,1.65f,17),new Vector3(0,.3f,23));
            var shadows=RenderSettings.sun.shadows;
            RenderSettings.sun.shadows=LightShadows.None;
            Shot(eye,"galeria-sem-sombra",new Vector3(0,1.65f,17),new Vector3(0,.3f,23));
            RenderSettings.sun.shadows=shadows;
            Shot(eye,"estrela",new Vector3(0,1.65f,-5),new Vector3(0,0,0));
            Shot(eye,"estrela-longe",new Vector3(0,1.65f,-9),new Vector3(0,0,0));
            var lit=Shot(eye,"obra",new Vector3(-5,2.4f,20.5f),new Vector3(-5,2.2f,23.8f));
            Require(FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l=>l.enabled && l.type==LightType.Spot)<=3,
                "Orçamento estável de até três focos próximos");
            foreach(var l in FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Spot)) l.enabled=false;
            var unlit=Shot(eye,"obra-sem-foco",new Vector3(-5,2.4f,20.5f),new Vector3(-5,2.2f,23.8f),false);
            double difference=0;
            for(int i=0;i<lit.Length;i++) difference+=Math.Abs(lit[i].r-unlit[i].r)+Math.Abs(lit[i].g-unlit[i].g)+Math.Abs(lit[i].b-unlit[i].b);
            difference/=lit.Length*3;
            Require(difference>.5,"Focos alteram a iluminação renderizada: diferença média "+difference.ToString("F2")+"/255");
            var spots = FindAnyObjectByType<MuseumPictureLighting>().GetComponentsInChildren<Light>(true);
            Require(spots.Length==24, "24 focos gerenciados por proximidade");
            Require(!FindObjectsByType<Light>(FindObjectsSortMode.None).Any(l=>
                l.type==LightType.Spot && l.enabled && !spots.Contains(l)), "Sem foco legado aceso fora do controle de proximidade");
            foreach (var spot in spots)
            {
                var close = spot.transform.position - spot.transform.forward * 2f;
                eye.transform.position = close;
                RefreshLights();
                Require(spot.enabled, "Foco acende por proximidade: " + spot.name);
                eye.transform.position = new Vector3(100, 2, 100);
                RefreshLights();
                Require(spots.All(l=>!l.enabled && l.intensity==0), "Todos os focos apagam ao se afastar de " + spot.name);
            }
        }
        private static void Require(bool condition,string description)
        {
            if(!condition) throw new Exception(description);
            Debug.Log("[VisitValidation] PASS "+description);
        }
        private static Color32[] Shot(Camera camera,string name,Vector3 position,Vector3 target,bool refreshLights=true)
        {
            string folder=Environment.GetEnvironmentVariable("MUSEUM_CAPTURE_OUTPUT") ?? "/private/tmp/museum-visit-before";
            Directory.CreateDirectory(folder);
            camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            if(refreshLights) RefreshLights();
            var rt=new RenderTexture(1280,800,24);
            var texture=new Texture2D(1280,800,TextureFormat.RGB24,false);
            var old=RenderTexture.active;
            camera.targetTexture=rt; camera.aspect=1.6f; camera.fieldOfView=75;
            camera.Render(); RenderTexture.active=rt;
            texture.ReadPixels(new Rect(0,0,1280,800),0,0); texture.Apply();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());
            var pixels=texture.GetPixels32();
            camera.targetTexture=null; RenderTexture.active=old;
            Destroy(texture); rt.Release(); Destroy(rt);
            Debug.Log("[VisitValidation] CAPTURE "+name);
            return pixels;
        }
        private static void RefreshLights()
        {
            foreach(var lighting in FindObjectsByType<MuseumPictureLighting>(FindObjectsSortMode.None))
            {
                typeof(MuseumPictureLighting).GetField("nextSelection",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(lighting,0f);
                var tick = typeof(MuseumPictureLighting).GetMethod("Tick",
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                for(int frame=0;frame<60;frame++) tick.Invoke(lighting,new object[]{1f/60f});
            }
        }
    }
}
#endif
