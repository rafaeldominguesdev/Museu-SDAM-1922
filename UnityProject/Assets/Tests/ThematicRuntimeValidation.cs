#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MuseumModerna
{
    public sealed class ThematicRuntimeValidation : MonoBehaviour
    {
        private int checks;
        private PlayerController player;
        private CharacterController controller;
        private Camera eye;
        private string folder;
        private IEnumerator Start()
        {
            for(int i=0;i<10;i++) yield return null;
            try{Check();Debug.Log("[ThematicValidation] PASS: "+checks+" verificações.");}
            catch(Exception e){Debug.LogException(e);UnityEditor.EditorApplication.Exit(1);yield break;}
            yield return null;UnityEditor.EditorApplication.Exit(0);
        }
        private void Check()
        {
            player=FindAnyObjectByType<PlayerController>();controller=player.GetComponent<CharacterController>();eye=player.GetComponentInChildren<Camera>();
            Require(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(0,-13))<.06f,"Spawn no foyer preservado");
            Require(Mathf.Abs(controller.bounds.min.y)<.07f,"Pés apoiados no piso");
            MobileVrMode.Instance.EnterNormalMode();
            foreach(var g in FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None))g.enabled=false;
            foreach(var m in FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None))m.enabled=false;
            foreach(var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))c.enabled=false;
            player.GetComponent<PlayerBodyVisual>().enabled=false;
            var gaze=eye.GetComponent<GazeDwellInteraction>();gaze.enabled=false;
            var exhibits=FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);
            Require(exhibits.Length==26&&exhibits.Select(e=>e.PaintingData.uniqueId).Distinct().Count()==26,"26 fichas ativas sem duplicação");
            foreach(var room in new[]{"Mario","Oswald","Anita","Di"})
            {
                var pieces=GameObject.Find("Sala_"+room).GetComponentsInChildren<PaintingExhibit>();
                Require(pieces.Length==5&&pieces.Count(e=>e.PaintingData.categoria=="biografia")==1,room+": biografia e quatro obras");
            }
            Require(GameObject.Find("Sala_Hall").GetComponentsInChildren<PaintingExhibit>().All(e=>e.PaintingData.categoria=="contexto"),"Hall dedicado ao contexto e introdução");
            var manifestos=GameObject.Find("Sala_Manifestos").GetComponentsInChildren<PaintingExhibit>();
            Require(manifestos.Length==3&&manifestos.Select(e=>e.PaintingData.year).OrderBy(y=>y).SequenceEqual(new[]{1924,1928,1929}),"Três manifestos com cronologia posterior à Semana");
            var arch=GameObject.Find("Museum_LouvreStyle").transform;
            int before=arch.GetComponentsInChildren<Collider>().Length;PillarPhysicsRepair.Repair(arch);
            Require(before==arch.GetComponentsInChildren<Collider>().Length,"Reparação legada não cria pilastras nas entradas novas");
            Require(FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Museum_LouvreStyle")==1,"Apenas uma arquitetura ativa");

            Route(new Vector3(0,0,-13),new Vector3(0,0,0));
            // Cada acesso é atravessado nos dois sentidos com a cápsula real.
            foreach(var route in new[]{
                new[]{new Vector3(-7,0,-5),new Vector3(-18,0,-5)},
                new[]{new Vector3(-7,0,5),new Vector3(-18,0,5)},
                new[]{new Vector3(7,0,5),new Vector3(18,0,5)},
                new[]{new Vector3(7,0,-5),new Vector3(18,0,-5)},
                new[]{new Vector3(0,0,7),new Vector3(0,0,20)}})
            {Route(route[0],route[1]);Route(route[1],route[0]);}
            foreach(int side in new[]{-1,1})
            {
                Position(new Vector3(side*17,.02f,-2));
                for(int i=0;i<160;i++)controller.Move(new Vector3(0,-.02f,.03f));
                Require(player.transform.position.z<-.25f,"Divisória separa salas do lado "+side);
            }
            foreach(var exhibit in exhibits)
            {
                var data=exhibit.PaintingData;
                var backing=exhibit.transform.Find("Fundo").GetComponent<Renderer>();
                Require(backing.sharedMaterial.IsKeywordEnabled("_EMISSION"),"Painel mantém iluminação de leitura: "+data.uniqueId);
                if(exhibit.ArtworkRenderer!=null)
                    Require(exhibit.ArtworkRenderer.sharedMaterial.IsKeywordEnabled("_EMISSION"),"Reprodução mantém preenchimento neutro: "+data.uniqueId);
                Require(!string.IsNullOrWhiteSpace(data.description)&&!string.IsNullOrWhiteSpace(data.fontes)&&
                    !string.IsNullOrWhiteSpace(data.relacao_com_a_semana_de_1922),"Ficha completa: "+data.uniqueId);
                var origin=exhibit.transform.position+exhibit.transform.forward*2.5f;
                Position(new Vector3(origin.x,.02f,origin.z));
                Require(gaze.RaycastExhibit(new Ray(origin,-exhibit.transform.forward))==exhibit,"Olhar alcança ficha: "+data.uniqueId);
                var fromBehind=exhibit.transform.position-exhibit.transform.forward*1;
                Require(gaze.RaycastExhibit(new Ray(fromBehind,exhibit.transform.forward))!=exhibit,"Parede bloqueia olhar por trás: "+data.uniqueId);
            }
            Require(exhibits.Count(e=>e.PaintingData.paintingTexture!=null)==8,"Oito reproduções nas salas dos pintores");
            Require(FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l=>l.enabled&&l.type==LightType.Spot)<=3,"Até três focos simultâneos");
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../artifacts/cinco-salas-2026-09-30"));Directory.CreateDirectory(folder);
            Shot("hall",new Vector3(0,1.65f,-8),new Vector3(0,2.7f,10));
            Shot("mario",new Vector3(-11,1.65f,-5),new Vector3(-20,2.3f,-6));
            Shot("oswald",new Vector3(-11,1.65f,5),new Vector3(-20,2.3f,6));
            Shot("anita",new Vector3(11,1.65f,5),new Vector3(20,2.3f,6));
            Shot("di-cavalcanti",new Vector3(11,1.65f,-5),new Vector3(20,2.3f,-6));
            Shot("manifestos",new Vector3(0,1.65f,17),new Vector3(0,2.45f,23.65f));
            Shot("mapa",new Vector3(5.5f,2.1f,-6),new Vector3(5.5f,2.5f,-9.65f));
            Shot("anita-detalhe",new Vector3(14,2.35f,2.5f),new Vector3(14,2.4f,.35f));
            Shot("mario-biografia",new Vector3(-21,2.4f,-5),new Vector3(-23.65f,2.4f,-5));
        }
        private void Route(Vector3 from,Vector3 to)
        {
            Position(from+Vector3.up*.02f);Vector3 step=(to-from)/400;step.y=-.02f;
            for(int i=0;i<400;i++)controller.Move(step);
            Require(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(to.x,to.z))<.12f&&controller.isGrounded,"Trajeto livre "+from+" → "+to);
        }
        private void Position(Vector3 point)
        {controller.enabled=false;player.transform.position=point;controller.enabled=true;Physics.SyncTransforms();}
        private void Shot(string name,Vector3 point,Vector3 target)
        {
            Position(new Vector3(point.x,.02f,point.z));eye.transform.position=point;eye.transform.LookAt(target);
            var rt=new RenderTexture(1600,1000,24);var previous=RenderTexture.active;eye.targetTexture=rt;eye.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),tex.EncodeToPNG());eye.targetTexture=null;RenderTexture.active=previous;
            Destroy(tex);rt.Release();Destroy(rt);
        }
        private void Require(bool condition,string message)
        {if(!condition)throw new Exception("[ThematicValidation] "+message);checks++;Debug.Log("[ThematicValidation] PASS "+message);}
    }
}
#endif
