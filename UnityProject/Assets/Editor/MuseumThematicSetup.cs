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
    /// <summary>Instala o percurso dos artistas e dos manifestos, preservando a cena anterior.</summary>
    public static class MuseumThematicSetup
    {
        [Serializable] public class Entry
        {
            public string id, room, kind, title, author, period, summary, body, relation, source, imagePath, credit, technique;
            public int year;
        }
        [Serializable] private class Catalog { public Entry[] items; }
        private const string Folder = "Assets/MuseumThematic";
        private static Material paper, ink, gold;
        private static Transform frameTemplate;
        private static Font font;

        public static Entry[] Read() => JsonUtility.FromJson<Catalog>(
            AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/MuseumThematicCatalog.json").text).items;

        [MenuItem("Museum/Percurso/Instalar Hall e seis salas")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var entries = Read();
            if (entries.Length != 31 || entries.Select(e=>e.id).Distinct().Count()!=31)
                throw new InvalidOperationException("O catálogo deve conter 31 fichas únicas.");
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
            PrepareFont(entries);
            paper=Material("Papel",new Color(.92f,.88f,.77f));
            ink=Material("Petroleo",new Color(.035f,.075f,.067f));
            gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/MuseumStyle/Materials/Bronze_dourado.mat");

            var architecture=LouvreMuseumSetup.BuildThematicArchitecture();
            var exhibition=new GameObject("Museum_GuidedExhibition").transform;
            foreach(var room in new[]{"Hall","Mario","Oswald","Anita","Di","Manifestos","Tarsila"})
            {
                var group=new GameObject("Sala_"+room).transform;
                group.SetParent(exhibition,false);
                int work=0;
                foreach(var entry in entries.Where(e=>e.room==room).OrderBy(e=>e.kind=="biografia"?0:1).ThenBy(e=>e.year))
                {
                    Position(entry,work,out var position,out var yaw);
                    Panel(group,entry,position,yaw);
                    if(entry.kind!="biografia") work++;
                }
            }
            RoomSign(exhibition,"01","MÁRIO DE ANDRADE","LITERATURA E PESQUISA",new Vector3(-9.25f,5.05f,-5),90);
            RoomSign(exhibition,"02","SALA DOS MANIFESTOS","PAU-BRASIL, ANTROPOFAGIA E VERDE-AMARELISMO",new Vector3(-9.25f,5.05f,5),90);
            RoomSign(exhibition,"03","ANITA MALFATTI","COR, FORMA E RUPTURA",new Vector3(9.25f,5.05f,5),-90);
            RoomSign(exhibition,"04","DI CAVALCANTI","ARTE, CIDADE E VIDA POPULAR",new Vector3(9.25f,5.05f,-5),-90);
            RoomSign(exhibition,"05","OSWALD DE ANDRADE","POESIA, PROSA E PROVOCAÇÃO",new Vector3(0,5.05f,9.25f),180);
            RoomSign(exhibition,"06","TARSILA DO AMARAL","COR, IMAGINAÇÃO E TRABALHO",new Vector3(0,5.05f,-9.25f),0);
            foreach(var center in new[]{new Vector3(-17,0,-5),new Vector3(-17,0,5),new Vector3(17,0,5),new Vector3(17,0,-5)})
            {
                bool west=center.x<0;
                WayfindingSign(exhibition,new Vector3(west?-10.6f:10.6f,4.72f,center.z),west?-90:90);
            }
            WayfindingSign(exhibition,new Vector3(0,4.72f,10.6f),0);
            WayfindingSign(exhibition,new Vector3(0,4.72f,-10.6f),180);
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
            // A iluminação dramática substitui os focos legados sem sombra. Ela é
            // reaplicável e remove o root antigo antes de criar um spotlight por obra.
            MuseumLightingSetup.ApplyToScene();
            MuseumAtmosphereUpgrade.AddReadingDisplays(architecture);
            // A ambientação agrupa automaticamente as obras por parede; aplicá-la aqui
            // impede que uma reinstalação da planta deixe os cordões no layout arquivado.
            MuseumAmbientDetails.ApplyTo(architecture);
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                camera.useOcclusionCulling=false; // o bake antigo corresponde à planta anterior
            RenderSettings.sun=architecture.Find("Luz_diurna").GetComponent<Light>();
            var active=UnityEngine.Object.FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);
            if(active.Length!=entries.Length) throw new InvalidOperationException("Quantidade incorreta de fichas ativas: "+active.Length);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Falha ao salvar percurso temático.");
            Debug.Log("[Thematic] Hall, seis salas de artistas e manifestos, 31 fichas. Spawn no centro.");
        }

        private static void Position(Entry e,int index,out Vector3 p,out float yaw)
        {
            if(e.room=="Hall")
            {
                p=index==0?new Vector3(-5.5f,2.45f,-9.65f):new Vector3(index==1?-5.5f:5.5f,2.45f,9.65f);
                yaw=index==0?0:180; return;
            }
            if(e.room=="Oswald")
            {
                if(e.kind=="biografia") {p=new Vector3(0,2.45f,23.65f);yaw=180;return;}
                bool left=index<2;
                p=new Vector3(left?-7.65f:7.65f,2.45f,index%2==0?14:20);yaw=left?90:-90;return;
            }
            if(e.room=="Manifestos")
            {
                p=index==2?new Vector3(-23.65f,2.45f,5):new Vector3(index==0?-14:-20,2.45f,.35f);
                yaw=index==2?90:0;return;
            }
            if(e.room=="Tarsila")
            {
                if(e.kind=="biografia") {p=new Vector3(-3,2.45f,-19.65f);yaw=0;return;}
                bool left=index<2;
                p=new Vector3(left?-4.65f:4.65f,2.45f,index%2==0?-12.3f:-17.7f);yaw=left?90:-90;return;
            }
            bool west=e.room=="Mario";
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
            data.periodo=entry.period;data.categoria=entry.kind;
            data.movement="Modernismo brasileiro";data.resumo=entry.summary;data.description=entry.body;
            data.relacao_com_a_semana_de_1922=entry.relation;data.fontes=entry.source;
            data.paintingTexture=string.IsNullOrEmpty(entry.imagePath)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(entry.imagePath);
            if(!string.IsNullOrEmpty(entry.imagePath)&&data.paintingTexture==null) throw new InvalidOperationException("Imagem ausente: "+entry.imagePath);
            bool image=data.paintingTexture!=null;
            data.tecnica=!string.IsNullOrWhiteSpace(entry.technique)?entry.technique:
                entry.kind=="pintura"?"Pintura":entry.kind=="biografia"&&image?"Fotografia documental":"Painel didático";
            data.obra_historica_ou_reinterpretacao=image?"Reprodução digital da obra. "+entry.credit:
                "Painel didático original do museu sobre a obra ou assunto; não é reprodução de pintura nem de capa histórica.";
            EditorUtility.SetDirty(data);
            var panel=new GameObject("OBRA_"+entry.id).transform;panel.SetParent(parent,false);
            panel.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            panel.gameObject.layer=LayerMask.NameToLayer("Painting");
            var collider=panel.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(2.9f,3.35f,.15f);collider.center=Vector3.down*.48f;
            var exhibit=panel.gameObject.AddComponent<PaintingExhibit>();exhibit.SetData(data);
            var crop=MuseumAtmosphereUpgrade.PortraitCrop(entry.id);
            bool portrait=image && data.paintingTexture.height*crop.height>data.paintingTexture.width*crop.width;
            float panelWidth=portrait?1.87f:2.38f, panelHeight=portrait?2.38f:1.87f;
            Cube(panel,"Fundo",Vector3.zero,new Vector3(panelWidth,panelHeight,.035f),paper);
            if(image)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Reproducao_obra";
                go.transform.SetParent(panel,false);go.transform.localPosition=Vector3.forward*.03f;
                go.transform.localRotation=Quaternion.Euler(0,180,0);
                float imageWidth=panelWidth-.12f, imageHeight=panelHeight-.12f;
                float sourceWidth=data.paintingTexture.width*crop.width, sourceHeight=data.paintingTexture.height*crop.height;
                float scale=Mathf.Min(imageWidth/sourceWidth,imageHeight/sourceHeight);
                go.transform.localScale=new Vector3(sourceWidth*scale,sourceHeight*scale,1);
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                if(crop!=new Rect(0,0,1,1))
                {
                    // Crop geometry UVs, not the archival source or its shared texture.
                    var mesh=UnityEngine.Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
                    var uv=mesh.uv;for(int i=0;i<uv.Length;i++)uv[i]=crop.position+Vector2.Scale(uv[i],crop.size);mesh.uv=uv;
                    string meshPath="Assets/MuseumStyle/Meshes/Retrato_recorte_"+entry.id+".asset";
                    var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(saved==null){AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}
                    else {EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
                    go.GetComponent<MeshFilter>().sharedMesh=saved;
                }
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
            // A malha preservada tem abertura de 1,40 × 1,10 m. A moldura acompanha
            // cada formato, sem esticar uma obra vertical dentro de um painel horizontal.
            frame.name="Moldura_perfilada_"+entry.id;frame.localPosition=Vector3.zero;frame.localRotation=Quaternion.identity;
            frame.localScale=new Vector3(panelWidth/1.4f,panelHeight/1.1f,1);
            frame.gameObject.SetActive(true);
            MuseumAtmosphereUpgrade.RefineFrame(frame);
            float captionTop=-panelHeight*.5f-.38f;
            Cube(panel,"Placa_da_obra",new Vector3(0,captionTop-.25f,.075f),new Vector3(panelWidth+.22f,.86f,.04f),ink);
            Border(panel,panelWidth+.22f,.86f,new Vector3(0,captionTop-.25f,.101f),.03f);
            Text(panel,entry.title,new Vector3(0,captionTop+.03f,.11f),.132f,new Color(1,.97f,.88f),30,panelWidth-.18f,true);
            Text(panel,entry.period,new Vector3(0,captionTop-.23f,.11f),.086f,new Color(1,.82f,.43f),36,panelWidth-.18f);
            string credit=image?entry.credit:"PAINEL DIDÁTICO";
            if(crop!=new Rect(0,0,1,1))credit+=" • RECORTE";
            Text(panel,credit,new Vector3(0,captionTop-.51f,.11f),.07f,paper.color,40,panelWidth-.18f);
        }

        private static void RoomSign(Transform parent,string number,string title,string subtitle,Vector3 position,float yaw)
        {
            var sign=new GameObject("Sinal_sala_"+number).transform;sign.SetParent(parent,false);
            sign.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            Cube(sign,"Placa_sala",Vector3.zero,new Vector3(6.5f,1.38f,.08f),ink);
            Border(sign,6.5f,1.38f,new Vector3(0,0,.055f),.04f);
            Cube(sign,"Medalhao_numero",new Vector3(-2.58f,0,.075f),new Vector3(.94f,.94f,.035f),gold);
            Text(sign,number,new Vector3(-2.58f,0,.105f),.45f,Color.white,4,.74f,true);
            Text(sign,title,new Vector3(.38f,.23f,.105f),.35f,paper.color,30,5.02f,true);
            Text(sign,subtitle,new Vector3(.38f,-.29f,.105f),.14f,new Color(1,.84f,.48f),38,5.02f);
        }
        private static void WayfindingSign(Transform parent,Vector3 position,float yaw)
        {
            var sign=new GameObject("Sinal_voltar_ao_hall").transform;sign.SetParent(parent,false);
            sign.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            Cube(sign,"Placa_direcional",Vector3.zero,new Vector3(4.35f,.68f,.065f),ink);
            Border(sign,4.35f,.68f,new Vector3(0,0,.046f),.028f);
            Text(sign,"←  VOLTAR AO HALL",new Vector3(0,0,.082f),.255f,paper.color,24,3.92f,true);
        }
        private static void Border(Transform parent,float width,float height,Vector3 center,float thickness)
        {
            Cube(parent,"Filete_superior",center+Vector3.up*(height*.5f-thickness*.5f),new Vector3(width-thickness,thickness,.018f),gold);
            Cube(parent,"Filete_inferior",center-Vector3.up*(height*.5f-thickness*.5f),new Vector3(width-thickness,thickness,.018f),gold);
            Cube(parent,"Filete_esquerdo",center-Vector3.right*(width*.5f-thickness*.5f),new Vector3(thickness,height-thickness,.018f),gold);
            Cube(parent,"Filete_direito",center+Vector3.right*(width*.5f-thickness*.5f),new Vector3(thickness,height-thickness,.018f),gold);
        }
        public static void Text(Transform parent,string value,Vector3 position,float size,Color color,int columns,float maxWidth=2.12f,bool bold=false)
        {
            var go=new GameObject("Texto");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.transform.localRotation=Quaternion.Euler(0,180,0);
            var text=go.AddComponent<TextMesh>();text.font=font;text.fontSize=64;text.characterSize=size*10/64;
            text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.lineSpacing=1.15f;
            text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;text.color=color;text.text=Wrap(value,columns);
            go.GetComponent<Renderer>().sharedMaterial=font.material;
            go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            go.GetComponent<Renderer>().receiveShadows=false;
            float width=go.GetComponent<Renderer>().localBounds.size.x;
            if(width>maxWidth) go.transform.localScale=Vector3.one*(maxWidth/width);
        }
        private static void PrepareFont(Entry[] entries)
        {
            var all=new StringBuilder("0123456789 ←·–—©/ÁÀÂÃÉÊÍÓÔÕÚÇáàâãéêíóôõúç");
            foreach(var entry in entries)
                all.Append(entry.title).Append(entry.author).Append(entry.period).Append(entry.summary).Append(entry.credit);
            all.Append("MÁRIO DE ANDRADEOSWALD DE ANDRADEANITA MALFATTIDI CAVALCANTITARSILA DO AMARAL")
                .Append("ANTROPOFAGIA E MANIFESTOSANTROPOFAGIA EM DIÁLOGOVOLTAR AO HALL")
                .Append("LITERATURA E PESQUISAPOESIA PROSA E PROVOCAÇÃOCOR FORMA E RUPTURA")
                .Append("ARTE CIDADE E VIDA POPULARIDEIAS DEPOIS DE 1922");
            font.RequestCharactersInTexture(all.ToString(),64,FontStyle.Normal);
            font.RequestCharactersInTexture(all.ToString(),64,FontStyle.Bold);
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
