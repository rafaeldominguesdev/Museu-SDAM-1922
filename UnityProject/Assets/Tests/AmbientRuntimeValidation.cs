#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MuseumModerna
{
    public sealed class AmbientRuntimeValidation : MonoBehaviour
    {
        private static string Output => Path.GetFullPath(Environment.GetEnvironmentVariable("MUSEUM_CAPTURE_OUTPUT") ??
            Path.Combine(Application.dataPath,"../../artifacts/ambientacao-2026-09-23"));
        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            try { Validate(); }
            catch (Exception e) { Debug.LogException(e); UnityEditor.EditorApplication.Exit(1); yield break; }
            Debug.Log("[AmbientValidation] PASS: colisões, circulação, olhar e capturas reais da cena.");
            UnityEditor.EditorApplication.Exit(0);
        }
        private void Validate()
        {
            var root = GameObject.Find("Museum_LouvreStyle").transform.Find("Ambientacao_Museologica");
            Require(root != null,"Ambientação persistida");
            Require(root.GetComponentsInChildren<Light>().Length == 0,"Nenhuma luz adicional");
            Require(root.Cast<Transform>().Count(t => t.name.StartsWith("Cordao_")) == 3,"Três barreiras");
            var all = root.GetComponentsInChildren<MeshFilter>();
            int triangles = all.Sum(m => m.sharedMesh.triangles.Length / 3);
            Require(triangles < 25000,"Geometria adicional, vasos e sombras abaixo de 25 mil triângulos: " + triangles);
            foreach (var filter in all)
                foreach (var v in filter.sharedMesh.vertices)
                    Require(!float.IsNaN(v.sqrMagnitude) && !float.IsInfinity(v.sqrMagnitude),"Geometria finita");
            foreach (var cycle in FindObjectsByType<MuseumDayNightCycle>(FindObjectsSortMode.None)) cycle.enabled=false;
            foreach (var movement in FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None)) movement.enabled=false;
            foreach (var gyro in FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None)) gyro.enabled=false;
            foreach (var anchor in FindObjectsByType<PlayerSpawnAnchor>(FindObjectsSortMode.None)) anchor.enabled=false;
            var player = FindAnyObjectByType<PlayerController>();
            var gaze = FindAnyObjectByType<GazeDwellInteraction>();
            Require(player != null && gaze != null,"Câmera e jogador existentes");
            var eye = gaze.GetComponent<Camera>();
            var controller = player.GetComponent<CharacterController>();
            Require(controller != null,"CharacterController existente");
            controller.enabled=false;
            player.transform.position=new Vector3(0,1.7f,-16);
            Physics.SyncTransforms();
            int solidChecks=0;
            foreach (var box in root.GetComponentsInChildren<BoxCollider>())
            {
                bool side = box.name.StartsWith("Retorno_");
                var normal = side ? box.transform.right : box.transform.forward;
                var center = box.transform.TransformPoint(box.center);
                Require(box.Raycast(new Ray(center+normal, -normal),out _,2),"Colisão no cordão "+box.name);
                solidChecks++;
            }
            // Testa a locomoção real contra cada cordão, com o controlador do visitante.
            foreach (Transform barrier in root)
            {
                if (!barrier.name.StartsWith("Cordao_")) continue;
                var box=barrier.GetComponentsInChildren<BoxCollider>().First(b=>b.name.StartsWith("Limite_"));
                var center=box.transform.TransformPoint(box.center); center.y=0;
                controller.enabled=false; player.transform.position=center+barrier.forward*1.1f;
                controller.enabled=true; Physics.SyncTransforms();
                for(int i=0;i<30;i++) controller.Move(-barrier.forward*.06f);
                Require(Vector3.Dot(player.transform.position-center,barrier.forward)>.15f,"Jogador não atravessa "+barrier.name);
                controller.enabled=false;
            }
            player.transform.position=new Vector3(0,1.7f,-16);
            Physics.SyncTransforms();
            int gazeChecks=0;
            foreach (var exhibit in FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None))
            {
                var center=exhibit.transform.position; var front=exhibit.transform.forward;
                if(exhibit.PaintingData.categoria=="Escultura cenográfica") continue;
                if(exhibit.PaintingData.categoria=="Vitrine documental") { center+=Vector3.up*1.1f; front=new Vector3(0,.3f,1).normalized; }
                var origin=center+front*2.5f;
                Require(gaze.RaycastExhibit(new Ray(origin,(center-origin).normalized))==exhibit,"Olhar livre: "+exhibit.name);
                gazeChecks++;
            }
            foreach (float h in new[]{.55f,1.7f})
            {
                Route(player,new Vector3(0,h,7),new Vector3(0,h,22));
                Route(player,new Vector3(-5.7f,h,15.5f),new Vector3(-5.7f,h,21.5f));
                Route(player,new Vector3(5.7f,h,15.5f),new Vector3(5.7f,h,21.5f));
            }
            var garden=GameObject.Find("Vegetacao_de_galeria");
            Require(garden != null && garden.transform.childCount==8,"Oito vasos instalados");
            foreach(var vase in garden.GetComponentsInChildren<CapsuleCollider>())
                foreach(var support in root.GetComponentsInChildren<CapsuleCollider>().Where(c=>c.name.StartsWith("Poste_cordao_")))
                    Require(Vector3.Distance(vase.transform.position,support.transform.position)>vase.radius+support.radius+.1f,"Vaso livre de poste");
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled=false;
            // Captura com a iluminação e os materiais reais, em altura de visitação.
            Directory.CreateDirectory(Output);
            Capture(eye,"galeria.png",new Vector3(-4.5f,1.7f,15.3f),new Vector3(1,1.9f,23));
            Capture(eye,"cordoes.png",new Vector3(.3f,1.7f,19.1f),new Vector3(.3f,1.85f,23.75f));
            Capture(eye,"entrada-e-plantas.png",new Vector3(0,1.7f,9.6f),new Vector3(0,2,18));
            Capture(eye,"plantas.png",new Vector3(3.7f,1.7f,14.8f),new Vector3(6.15f,1.1f,10.85f));
            Capture(eye,"detalhe.png",new Vector3(-3.2f,1.35f,21.5f),new Vector3(-4.4f,.7f,22.8f));
            File.WriteAllText(Path.Combine(Output,"validacao.txt"),
                "PASS\n"+triangles+" triângulos adicionais\n"+solidChecks+" limites físicos\n3 testes de locomoção contra cordões\n"+
                gazeChecks+" fichas não escultóricas acessíveis\n6 percursos livres (raio 0,30 m)\n8 vasos instalados\n"+
                "Zero novas luzes; banho de luz modelado com malha de gradiente.\nCapturas mono do Editor; aparelho VR não testado.\n");
        }
        private static void Route(PlayerController player,Vector3 from,Vector3 to)
        {
            var delta=to-from;
            var hits=Physics.SphereCastAll(from,.3f,delta.normalized,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
                .Select(h=>h.collider).Concat(Physics.OverlapSphere(from,.3f)).Concat(Physics.OverlapSphere(to,.3f))
                .Where(c=>!c.transform.IsChildOf(player.transform)).ToArray();
            Require(hits.Length==0,"Percurso "+from+" até "+to+": "+string.Join(",",hits.Select(c=>c.name)));
        }
        private static void Capture(Camera eye,string name,Vector3 position,Vector3 target)
        {
            eye.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            var previous=eye.targetTexture; var active=RenderTexture.active;
            var rt=new RenderTexture(1600,1000,24){antiAliasing=4};
            var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try
            {
                eye.targetTexture=rt; eye.aspect=1.6f; eye.fieldOfView=62;
                eye.Render(); RenderTexture.active=rt; texture.ReadPixels(new Rect(0,0,1600,1000),0,0); texture.Apply();
                File.WriteAllBytes(Path.Combine(Output,name),texture.EncodeToPNG());
            }
            finally { eye.targetTexture=previous; RenderTexture.active=active; rt.Release(); Destroy(rt); Destroy(texture); }
        }
        private static void Require(bool condition,string message) { if(!condition) throw new Exception("[AmbientValidation] "+message); }
    }
}
#endif
