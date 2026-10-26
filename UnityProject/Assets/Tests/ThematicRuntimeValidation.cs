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
            foreach(var node in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if(UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject)>0)
                    throw new Exception("Script ausente em objeto ativo: "+node.name);
            Require(true,"Objetos ativos sem componentes de script ausentes");
            player=FindAnyObjectByType<PlayerController>();controller=player.GetComponent<CharacterController>();eye=player.GetComponentInChildren<Camera>();
            Require(new Vector2(player.transform.position.x,player.transform.position.z).magnitude<.06f,"Spawn no centro do Hall");
            Require(Mathf.Abs(controller.bounds.min.y)<.07f,"Pés apoiados no piso");
            MobileVrMode.Instance.EnterNormalMode();
            foreach(var g in FindObjectsByType<GyroscopeController>(FindObjectsSortMode.None))g.enabled=false;
            foreach(var m in FindObjectsByType<HeadGazeMovement>(FindObjectsSortMode.None))m.enabled=false;
            foreach(var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))c.enabled=false;
            player.GetComponent<PlayerBodyVisual>().enabled=false;
            var gaze=eye.GetComponent<GazeDwellInteraction>();gaze.enabled=false;
            var exhibits=FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);
            Require(exhibits.Length==31&&exhibits.Select(e=>e.PaintingData.uniqueId).Distinct().Count()==31,"31 fichas com IDs únicos");
            foreach(var room in new[]{"Mario","Oswald","Anita","Di","Tarsila"})
            {
                var pieces=GameObject.Find("Sala_"+room).GetComponentsInChildren<PaintingExhibit>();
                Require(pieces.Length==5&&pieces.Count(e=>e.PaintingData.categoria=="biografia")==1,room+": biografia e quatro obras");
            }
            var tarsila=GameObject.Find("Sala_Tarsila").GetComponentsInChildren<PaintingExhibit>();
            var tarsilaWorks=tarsila.Where(e=>e.PaintingData.categoria=="pintura").ToArray();
            Require(tarsilaWorks.Select(e=>e.PaintingData.uniqueId).OrderBy(id=>id).SequenceEqual(
                new[]{"abaporu","tarsila_cuca","tarsila_negra","tarsila_operarios"})&&
                tarsilaWorks.All(e=>e.PaintingData.artist=="Tarsila do Amaral"&&e.PaintingData.paintingTexture!=null),
                "Tarsila reúne as quatro pinturas solicitadas com reproduções e autoria corretas");
            Require(GameObject.Find("Sala_Hall").GetComponentsInChildren<PaintingExhibit>().All(e=>e.PaintingData.categoria=="contexto"),"Hall dedicado ao contexto e introdução");
            var manifestos=GameObject.Find("Sala_Manifestos").GetComponentsInChildren<PaintingExhibit>();
            Require(manifestos.Length==3&&manifestos.All(e=>e.PaintingData.categoria=="manifesto"),
                "Sala dos manifestos reservada aos três textos posteriores à Semana");
            Require(manifestos.All(e=>e.transform.position.x < -10 && e.transform.position.z > 0),
                "Manifestos ocupam a antiga sala de Oswald");
            Require(GameObject.Find("Sala_Oswald").GetComponentsInChildren<PaintingExhibit>().All(e=>e.transform.position.z>10),
                "Literatura de Oswald preservada na sala ao fundo");
            var pescadores=exhibits.Single(e=>e.PaintingData.uniqueId=="di_pescadores").PaintingData;
            Require(pescadores.year==1951&&pescadores.paintingTexture.name=="di_pescadores_1951"&&pescadores.fontes.Contains("17893"),
                "Pescadores usa a reprodução de 1951 identificada pelo MAC-USP");
            foreach(var room in new[]{"Mario","Oswald","Anita","Di","Tarsila","Manifestos"})
            {
                var pieces=GameObject.Find("Sala_"+room).GetComponentsInChildren<PaintingExhibit>();
                var years=pieces.Where(e=>e.PaintingData.categoria!="biografia").Select(e=>e.PaintingData.year).ToArray();
                Require(years.SequenceEqual(years.OrderBy(y=>y)),"Percurso cronológico: "+room);
                for(int i=0;i<pieces.Length;i++)for(int j=i+1;j<pieces.Length;j++)
                {
                    var a=pieces[i].GetComponent<BoxCollider>();var b=pieces[j].GetComponent<BoxCollider>();
                    Require(!Physics.ComputePenetration(a,a.transform.position,a.transform.rotation,b,b.transform.position,b.transform.rotation,out _,out _),
                        "Obras sem sobreposição: "+pieces[i].name+" / "+pieces[j].name);
                }
            }
            var abaporu=tarsila.Single(e=>e.PaintingData.uniqueId=="abaporu");
            Require(abaporu.PaintingData.paintingTexture!=null&&abaporu.PaintingData.year==1928&&
                abaporu.PaintingData.obra_historica_ou_reinterpretacao.Contains("MALBA"),"Abaporu institucional, creditado e datado");
            foreach(var id in new[]{"mario_bio","oswald_bio","anita_bio","di_bio","tarsila_bio"})
            {
                var portrait=exhibits.Single(e=>e.PaintingData.uniqueId==id);
                Require(portrait.PaintingData.paintingTexture!=null&&!string.IsNullOrWhiteSpace(portrait.PaintingData.obra_historica_ou_reinterpretacao),
                    "Biografia com retrato documental creditado: "+id);
            }
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
                new[]{new Vector3(0,0,7),new Vector3(0,0,20)},
                new[]{new Vector3(0,0,-7),new Vector3(0,0,-15)}})
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
            Require(exhibits.Count(e=>e.PaintingData.paintingTexture!=null)==17,"Dezessete imagens: doze pinturas e cinco retratos documentais");
            foreach(var exhibit in exhibits.Where(e=>e.PaintingData.paintingTexture!=null &&
                e.PaintingData.paintingTexture.height>e.PaintingData.paintingTexture.width))
            {
                var fundo=exhibit.transform.Find("Fundo");
                var moldura=exhibit.transform.Find("Moldura_perfilada_"+exhibit.PaintingData.uniqueId);
                Require(fundo.localScale.y>fundo.localScale.x&&moldura.localScale.y>moldura.localScale.x,
                    "Moldura e painel verticais: "+exhibit.PaintingData.uniqueId);
            }
            var dramaticSpots=FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(l=>l.name.StartsWith("Spotlight_")).ToArray();
            Require(dramaticSpots.Length==31&&dramaticSpots.All(l=>l.shadows==LightShadows.Soft),
                "31 focos de obra preparados para sombras e proximidade");
            Position(new Vector3(0,.02f,0));RefreshLights();
            Require(dramaticSpots.All(l=>!l.enabled),"Focos das obras apagados no centro distante");
            foreach(var exhibit in exhibits)
            {
                var near=exhibit.transform.position+exhibit.transform.forward*2.5f;
                Position(new Vector3(near.x,.02f,near.z));RefreshLights();
                Require(exhibit.GetComponentInChildren<Light>().enabled,"Foco acende ao aproximar: "+exhibit.PaintingData.uniqueId);
                Require(dramaticSpots.Count(l=>l.enabled)<=3,"Até três focos de obras ativos");
                var behind=exhibit.transform.position-exhibit.transform.forward*1.5f;
                Position(new Vector3(behind.x,.02f,behind.z));RefreshLights();
                Require(!exhibit.GetComponentInChildren<Light>().enabled,"Foco não acende pelo verso: "+exhibit.PaintingData.uniqueId);
            }
            var chandelier=arch.Find("Luz_do_lustre").GetComponent<Light>();
            Require(chandelier.enabled&&chandelier.intensity>1&&chandelier.shadows==LightShadows.Soft,"Lustre com luz real e sombras");
            var moonLights=arch.GetComponentsInChildren<Light>().Where(l=>l.name.StartsWith("Luz_lunar_")).ToArray();
            Require(moonLights.Length==7&&moonLights.All(l=>l.enabled&&l.shadows==LightShadows.Soft&&l.color.b>l.color.r),
                "Claraboias iluminam todas as salas e o Hall com luz lunar");
            var skies=arch.GetComponentsInChildren<Renderer>().Where(r=>r.name.Contains("Ceu_noturno")).ToArray();
            Require(skies.Length==7&&skies.All(r=>r.sharedMaterial.shader.name=="Museum/Night Sky"&&r.sharedMaterial.shader.isSupported),
                "Sete claraboias usam o céu direcional, independente de UVs esticadas");
            Require(skies.All(r=>r.sharedMaterial==skies[0].sharedMaterial)&&skies[0].sharedMaterial.GetTexture("_MoonTex").width==512&&
                skies[0].sharedMaterial.GetFloat("_MoonRadius")>.04f,
                "Lua com relevo e direção compartilhada entre todas as salas");
            Require(arch.Find("Mesas_de_leitura").childCount==3&&arch.Find("Mesas_de_leitura").GetComponentsInChildren<BoxCollider>().Length==3,
                "Três vitrines cenográficas protegidas por colisores");
            var readingRenderers=arch.Find("Mesas_de_leitura").GetComponentsInChildren<Renderer>();
            Require(readingRenderers.Where(r=>r.name=="Paginas").All(r=>r.sharedMaterial.IsKeywordEnabled("_EMISSION")),
                "Páginas das vitrines mantêm preenchimento de leitura");
            Require(readingRenderers.Where(r=>r.name.StartsWith("Vidro_")).All(r=>r.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.Off),
                "Vidros não projetam sombras opacas sobre os livros");
            Require(arch.Find("Ensaio_curatorial_Tarsila")!=null,"Sala Tarsila com composição curatorial própria");
            Require(chandelier.shadowStrength<=.3f,"Sombras do lustre suavizadas");
            foreach(var id in new[]{"anita_bio","tarsila_bio","oswald_bio"})
            {
                var portrait=exhibits.Single(e=>e.PaintingData.uniqueId==id);
                var uv=portrait.ArtworkRenderer.GetComponent<MeshFilter>().sharedMesh.uv;
                Require(uv.Any(p=>p.x>0&&p.x<1||p.y>0&&p.y<1),"Retrato com recorte não destrutivo: "+id);
            }
            var diPlants=arch.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Vaso_Di_Cavalcanti_")).ToArray();
            Require(diPlants.Length==4&&diPlants.All(t=>Mathf.Abs(t.position.x-17)>5&&Mathf.Abs(t.position.z+5)>3),
                "Sala Di Cavalcanti ambientada com um vaso em cada canto");
            var roomSigns=FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Sinal_sala_")).ToArray();
            Require(roomSigns.Length==6&&roomSigns.All(s=>s.Find("Placa_sala")!=null&&s.GetComponentsInChildren<TextMesh>().Length==3),
                "Seis cabeçalhos de sala redesenhados e sem sobreposição");
            Require(roomSigns.All(s=>s.GetComponentsInChildren<TextMesh>().Single(t=>t.text==s.name.Substring("Sinal_sala_".Length)).color==Color.white),
                "Seis números das salas brancos para contrastar com o medalhão");
            Require(RenderSettings.ambientLight.r<.4f&&RenderSettings.fog,"Ambiente não é sobrescrito pelo ciclo legado");
            Require(arch.Find("Focos_das_obras")==null,"Focos legados removidos sem duplicação");
            var ropes=arch.Find("Ambientacao_Museologica");
            Require(ropes!=null&&ropes.childCount>=6&&ropes.GetComponentsInChildren<Collider>().Length>=26,
                "Cordões de segurança reaplicados nas paredes com obras");
            folder=Path.GetFullPath(Environment.GetEnvironmentVariable("MUSEUM_CAPTURE_OUTPUT") ??
                Path.Combine(Application.dataPath,"../../artifacts/cinco-salas-2026-09-30"));
            Directory.CreateDirectory(folder);
            Shot("hall",new Vector3(0,1.65f,-8),new Vector3(0,2.7f,10));
            Shot("placa-sala-numero",new Vector3(-4,4.5f,-5),new Vector3(-9.25f,5.05f,-5));
            Shot("mario",new Vector3(-11,1.65f,-5),new Vector3(-20,2.3f,-6));
            Shot("manifestos",new Vector3(-11,1.65f,5),new Vector3(-20,2.3f,3));
            Shot("anita",new Vector3(11,1.65f,5),new Vector3(20,2.3f,6));
            Shot("di-cavalcanti",new Vector3(11,1.65f,-5),new Vector3(20,2.3f,-6));
            Shot("oswald",new Vector3(0,1.65f,11),new Vector3(-3,2.45f,20));
            Require(GameObject.Find("Mapa_do_percurso")==null,"Painel de percurso retirado do Hall");
            Shot("hall-sem-painel",new Vector3(0,1.65f,0),new Vector3(3,3,-10));
            Shot("anita-detalhe",new Vector3(14,2.35f,2.5f),new Vector3(14,2.4f,.35f));
            Shot("mario-biografia",new Vector3(-21,2.4f,-5),new Vector3(-23.65f,2.4f,-5));
            Shot("tarsila",new Vector3(0,1.65f,-11),new Vector3(4.65f,2.45f,-15));
            Shot("lustre",new Vector3(0,1.65f,-5),new Vector3(0,5.6f,0));
            Shot("di-cavalcanti-plantas",new Vector3(14,1.65f,-5),new Vector3(22,1.7f,-5));
            Shot("claraboia-di",new Vector3(17,1.65f,-5),new Vector3(17,6.5f,-5));
            Shot("lua-e-estrelas",new Vector3(17,1.65f,-5),new Vector3(17.8f,6.65f,-5.9f));
            Shot("claraboia-hall",new Vector3(0,1.65f,0),new Vector3(1.26f,9.55f,-1.42f));
            Shot("vitrine-mario",new Vector3(-17,1.65f,-6.4f),new Vector3(-17,1.10f,-8.15f));
            Shot("tarsila-ambiente",new Vector3(0,1.65f,-11.5f),new Vector3(0,2.6f,-19.7f));
            foreach(var exhibit in exhibits.Where(e=>e.ArtworkRenderer!=null))
            {
                var center=exhibit.transform.position;
                Shot(exhibit.PaintingData.uniqueId,center+exhibit.transform.forward*3.5f-Vector3.up*.3f,center-Vector3.up*.3f);
            }
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
            RefreshLights();
            var rt=new RenderTexture(1600,1000,24);var previous=RenderTexture.active;eye.targetTexture=rt;eye.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),tex.EncodeToPNG());eye.targetTexture=null;RenderTexture.active=previous;
            Destroy(tex);rt.Release();Destroy(rt);
        }
        private void Require(bool condition,string message)
        {if(!condition)throw new Exception("[ThematicValidation] "+message);checks++;Debug.Log("[ThematicValidation] PASS "+message);}
        private void RefreshLights()
        {
            var lighting=FindAnyObjectByType<MuseumPictureLighting>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(MuseumPictureLighting).GetField("nextSelection",flags).SetValue(lighting,0f);
            var tick=typeof(MuseumPictureLighting).GetMethod("Tick",flags);
            tick.Invoke(lighting,new object[]{1f});
        }
    }
}
#endif
