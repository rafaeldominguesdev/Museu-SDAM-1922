using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Instala o percurso de quatro artistas e três manifestos, preservando a cena anterior.</summary>
    public static class MuseumThematicSetup
    {
        [Serializable] public class Entry
        {
            public string id, room, kind, title, author, period, summary, body, relation, source, imagePath, credit;
            public int year;
        }
        [Serializable] private class Catalog { public Entry[] items; }
        private const string Folder = "Assets/MuseumThematic";
        private static Material paper, ink, gold;
        private static Transform frameTemplate;
        private static Font font;

        public static Entry[] Read() => JsonUtility.FromJson<Catalog>(
            AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/MuseumThematicCatalog.json").text).items;

        [MenuItem("Museum/Percurso/Instalar Hall e cinco salas")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var entries = Read();
            if (entries.Length != 26 || entries.Select(e=>e.id).Distinct().Count()!=26)
                throw new InvalidOperationException("O catálogo deve conter 26 fichas únicas.");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MuseumScene.unity");
            Directory.CreateDirectory(Folder + "/Data");
            Directory.CreateDirectory(Folder + "/Materials");
            AssetDatabase.Refresh();
            foreach(var entry in entries.Where(e=>!string.IsNullOrEmpty(e.imagePath)))
                ArtworkTextureSetup.ConfigureTexture(entry.imagePath);

            // Arquiva o conjunto anterior uma única vez, sem apagar cenas ou objetos autorados.
            var archive = scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Museum_PreviousLayout");
            if (archive == null)
            {
                archive = new GameObject("Museum_PreviousLayout");
                archive.SetActive(false);
                var keep = new[] {"Player","Main Camera","GameManager","AmbientAudioManager","EventSystem","ExhibitManager"};
                foreach(var go in scene.GetRootGameObjects())
                    if(go!=archive && !keep.Contains(go.name) && go.GetComponentInChildren<UIManager>(true)==null)
                        go.transform.SetParent(archive.transform,true);
            }
            else
            {
                // Apenas os conjuntos gerados por este instalador são substituídos ao reaplicar.
                foreach(var name in new[]{"Museum_LouvreStyle","Museum_GuidedExhibition"})
                {
                    var generated=GameObject.Find(name);
                    if(generated!=null) UnityEngine.Object.DestroyImmediate(generated);
                }
            }
            archive.SetActive(false);
            // A interface tem RectTransform e pode ter outro nome em cenas antigas.
            // Mantê-la fora do arquivo também preserva home, configurações e bootstrap VR.
            foreach(var ui in archive.GetComponentsInChildren<UIManager>(true))
            {
                var uiRoot=ui.transform;
                while(uiRoot.parent!=archive.transform) uiRoot=uiRoot.parent;
                uiRoot.SetParent(null,true);
            }
            if(UnityEngine.Object.FindAnyObjectByType<UIManager>()==null)
                throw new InvalidOperationException("Interface do visitante ausente ou inativa.");
            frameTemplate=archive.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t=>t.name=="Moldura_perfilada_homem_amarelo");
            if(frameTemplate==null) throw new InvalidOperationException("Moldura original ausente no arquivo da cena.");
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            paper=Material("Papel",new Color(.92f,.88f,.77f));
            ink=Material("Petroleo",new Color(.035f,.075f,.067f));
            gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/MuseumStyle/Materials/Bronze_dourado.mat");

            var architecture=LouvreMuseumSetup.BuildThematicArchitecture();
            var exhibition=new GameObject("Museum_GuidedExhibition").transform;
            foreach(var room in new[]{"Hall","Mario","Oswald","Anita","Di","Manifestos"})
            {
                var group=new GameObject("Sala_"+room).transform;
                group.SetParent(exhibition,false);
                int work=0;
                foreach(var entry in entries.Where(e=>e.room==room))
                {
                    Position(entry,work,out var position,out var yaw);
                    Panel(group,entry,position,yaw);
                    if(entry.kind!="biografia") work++;
                }
            }
            Sign(exhibition,"01  MÁRIO DE ANDRADE",new Vector3(-9.7f,4.8f,-5),90);
            Sign(exhibition,"02  OSWALD DE ANDRADE",new Vector3(-9.7f,4.8f,5),90);
            Sign(exhibition,"03  ANITA MALFATTI",new Vector3(9.7f,4.8f,5),-90);
            Sign(exhibition,"04  DI CAVALCANTI",new Vector3(9.7f,4.8f,-5),-90);
            Sign(exhibition,"05  MANIFESTOS",new Vector3(0,5.25f,9.7f),180);
            Sign(exhibition,"INTRODUÇÃO AO MODERNISMO",new Vector3(0,4.1f,-10.3f),180);
            foreach(var center in new[]{new Vector3(-17,0,-5),new Vector3(-17,0,5),new Vector3(17,0,5),new Vector3(17,0,-5)})
            {
                bool west=center.x<0;
                Sign(exhibition,"VOLTAR AO HALL",new Vector3(west?-10.3f:10.3f,4.2f,center.z),west?-90:90);
            }
            Sign(exhibition,"VOLTAR AO HALL",new Vector3(0,4.2f,10.3f),0);
            Map(exhibition);
            // Bancos originais nas zonas de pausa; eixos e cinco entradas ficam livres.
            foreach(var name in new[]{"Banco_curvo_-5_-5","Banco_curvo_-5_5","Banco_curvo_5_-5","Banco_curvo_5_5"})
            {
                var source=archive.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
                if(source==null) continue;
                var bench=UnityEngine.Object.Instantiate(source.gameObject,architecture);
                bench.name=name; bench.SetActive(true);
                bench.transform.SetPositionAndRotation(source.position,source.rotation);
                // Os novos acessos laterais ficam em z=±5; mova as pausas para dentro do Hall.
                var p=bench.transform.position; p.z=Mathf.Sign(p.z)*3.2f; bench.transform.position=p;
            }
            MuseumVisitRepair.ConfigurePictureLights();
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                camera.useOcclusionCulling=false; // o bake antigo corresponde à planta anterior
            RenderSettings.sun=architecture.Find("Luz_diurna").GetComponent<Light>();
            var active=UnityEngine.Object.FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);
            if(active.Length!=26) throw new InvalidOperationException("Quantidade incorreta de fichas ativas: "+active.Length);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Falha ao salvar percurso temático.");
            Debug.Log("[Thematic] Hall + 5 salas, 26 fichas, 5 acessos diretos. Percurso anterior arquivado na cena.");
        }

        private static void Position(Entry e,int index,out Vector3 p,out float yaw)
        {
            if(e.room=="Hall")
            {
                p=index==0?new Vector3(-5.5f,2.45f,-9.65f):new Vector3(index==1?-5.5f:5.5f,2.45f,9.65f);
                yaw=index==0?0:180; return;
            }
            if(e.room=="Manifestos")
            {
                p=new Vector3(-4.8f+index*4.8f,2.45f,23.65f); yaw=180; return;
            }
            bool west=e.room=="Mario"||e.room=="Oswald";
            float z=e.room=="Mario"||e.room=="Di"?-5:5;
            if(e.kind=="biografia") {p=new Vector3(west?-23.65f:23.65f,2.45f,z);yaw=west?90:-90;return;}
            p=new Vector3((west?-1:1)*(index%2==0?14:20),2.45f,z+(index<2?-4.65f:4.65f));
            yaw=index<2?0:180;
        }

        private static void Panel(Transform parent,Entry entry,Vector3 position,float yaw)
        {
            string path=Folder+"/Data/"+entry.id+".asset";
            var data=AssetDatabase.LoadAssetAtPath<PaintingInfo>(path);
            if(data==null){data=ScriptableObject.CreateInstance<PaintingInfo>();AssetDatabase.CreateAsset(data,path);}
            data.uniqueId=entry.id;data.title=entry.title;data.artist=entry.author;data.year=entry.year;
            data.periodo=entry.period;data.categoria=entry.kind;data.tecnica=entry.kind=="pintura"?"Pintura":"Painel didático";
            data.movement="Modernismo brasileiro";data.resumo=entry.summary;data.description=entry.body;
            data.relacao_com_a_semana_de_1922=entry.relation;data.fontes=entry.source;
            data.paintingTexture=string.IsNullOrEmpty(entry.imagePath)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(entry.imagePath);
            if(!string.IsNullOrEmpty(entry.imagePath)&&data.paintingTexture==null) throw new InvalidOperationException("Imagem ausente: "+entry.imagePath);
            bool image=data.paintingTexture!=null;
            data.obra_historica_ou_reinterpretacao=image?"Reprodução digital da obra. "+entry.credit:
                "Painel didático original do museu sobre a obra ou assunto; não é reprodução de pintura nem de capa histórica.";
            EditorUtility.SetDirty(data);
            var panel=new GameObject("OBRA_"+entry.id).transform;panel.SetParent(parent,false);
            panel.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            panel.gameObject.layer=LayerMask.NameToLayer("Painting");
            var collider=panel.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(2.9f,2.9f,.15f);collider.center=Vector3.down*.3f;
            var exhibit=panel.gameObject.AddComponent<PaintingExhibit>();exhibit.SetData(data);
            Cube(panel,"Fundo",Vector3.zero,new Vector3(2.38f,1.87f,.035f),paper);
            if(image)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Reproducao_obra";
                go.transform.SetParent(panel,false);go.transform.localPosition=Vector3.forward*.03f;
                go.transform.localRotation=Quaternion.Euler(0,180,0);
                float scale=Mathf.Min(2.26f/data.paintingTexture.width,1.75f/data.paintingTexture.height);
                go.transform.localScale=new Vector3(data.paintingTexture.width*scale,data.paintingTexture.height*scale,1);
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                var material=Material(entry.id,Color.white);material.mainTexture=data.paintingTexture;
                material.SetTexture("_EmissionMap",data.paintingTexture);material.SetColor("_EmissionColor",Color.white*.45f);
                EditorUtility.SetDirty(material);
                var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                exhibit.SetArtworkRenderer(renderer);
            }
            else
            {
                string tag=entry.kind=="livro"?"LITERATURA":entry.kind=="manifesto"?"IDEIAS EM DISPUTA":entry.kind=="biografia"?"TRAJETÓRIA E PARTICIPAÇÃO":"MODERNISMO BRASILEIRO";
                Text(panel,tag,new Vector3(0,.71f,.04f),.075f,ink.color,38);
                Text(panel,entry.title,new Vector3(0,.35f,.04f),entry.title.Length>32?.14f:.19f,ink.color,28);
                Text(panel,entry.summary,new Vector3(0,-.28f,.04f),.095f,ink.color,37);
                Text(panel,"OLHE PARA EXPLORAR",new Vector3(0,-.76f,.04f),.075f,ink.color,38);
            }
            var frame=UnityEngine.Object.Instantiate(frameTemplate.gameObject,panel).transform;
            // A malha preservada tem abertura de 1,40 × 1,10 m. O novo painel tem 2,38 × 1,87 m.
            frame.name="Moldura_perfilada_"+entry.id;frame.localPosition=Vector3.zero;frame.localRotation=Quaternion.identity;frame.localScale=new Vector3(1.7f,1.7f,1);
            frame.gameObject.SetActive(true);
            Text(panel,entry.title,new Vector3(0,-1.34f,.08f),.095f,new Color(1,.96f,.86f),38);
            Text(panel,entry.period,new Vector3(0,-1.52f,.08f),.065f,new Color(1,.87f,.59f),48);
            Text(panel,image?entry.credit:"PAINEL DIDÁTICO",new Vector3(0,-1.8f,.08f),.065f,ink.color,48);
        }

        private static void Map(Transform root)
        {
            var map=new GameObject("Mapa_do_percurso").transform;map.SetParent(root,false);
            map.position=new Vector3(5.5f,2.5f,-9.65f);
            Cube(map,"Placa",Vector3.zero,new Vector3(3.8f,2.6f,.06f),ink);
            Text(map,"ESCOLHA SEU PERCURSO",new Vector3(0,1.02f,.05f),.17f,paper.color,34,3.5f);
            Text(map,"À ESQUERDA\n01  Mário de Andrade\n02  Oswald de Andrade\n\nÀ DIREITA\n03  Anita Malfatti\n04  Di Cavalcanti\n\nAO FUNDO\n05  Manifestos",new Vector3(0,-.1f,.05f),.12f,paper.color,38,3.5f);
        }
        private static void Sign(Transform parent,string value,Vector3 position,float yaw)
        {
            var sign=new GameObject("Sinal_"+value).transform;sign.SetParent(parent,false);
            sign.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            Text(sign,value,new Vector3(0,0,.02f),.18f,ink.color,40,5);
        }
        public static void Text(Transform parent,string value,Vector3 position,float size,Color color,int columns,float maxWidth=2.12f)
        {
            var go=new GameObject("Texto");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.transform.localRotation=Quaternion.Euler(0,180,0);
            var text=go.AddComponent<TextMesh>();text.font=font;text.fontSize=64;text.characterSize=size*10/64;
            text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.lineSpacing=1.15f;
            text.color=color;text.text=Wrap(value,columns);
            go.GetComponent<Renderer>().sharedMaterial=font.material;
            float width=go.GetComponent<Renderer>().localBounds.size.x;
            if(width>maxWidth) go.transform.localScale=Vector3.one*(maxWidth/width);
        }
        private static string Wrap(string value,int columns)
        {
            var output=new StringBuilder();
            foreach(var line in value.Split('\n'))
            {
                int length=0;
                foreach(var word in line.Split(' '))
                {
                    if(length>0&&length+1+word.Length>columns){output.Append('\n');length=0;}
                    if(length>0){output.Append(' ');length++;}output.Append(word);length+=word.Length;
                }
                output.Append('\n');
            }
            return output.ToString().TrimEnd();
        }
        private static void Cube(Transform parent,string name,Vector3 p,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            go.GetComponent<Renderer>().receiveShadows=false;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        private static Material Material(string name,Color color)
        {
            string path=Folder+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;material.SetFloat("_Glossiness",.1f);material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor",color*.45f);
            // EmissiveIsBlack faz o importador remover _EMISSION, mesmo após EnableKeyword.
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);return material;
        }
    }
}
