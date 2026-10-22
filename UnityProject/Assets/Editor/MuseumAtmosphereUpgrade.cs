using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>Authored offline: shared materials and meshes, no per-frame generation.</summary>
    public static class MuseumAtmosphereUpgrade
    {
        private const string Folder="Assets/MuseumStyle/Materials/";
        private static readonly Dictionary<Mesh,Mesh> Frames=new Dictionary<Mesh,Mesh>();
        public static void PrepareMaterials()
        {
            Frames.Clear();
            var grain=new Texture2D(256,256,TextureFormat.RGB24,true){name="Grain_mineral",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            var pixels=new Color[256*256];
            for(int y=0;y<256;y++) for(int x=0;x<256;x++)
            {
                float n=.20f+.48f*Mathf.PerlinNoise(x*.035f,y*.035f)+.22f*Mathf.PerlinNoise(x*.49f,y*.49f);
                pixels[y*256+x]=new Color(n,n,n);
            }
            grain.SetPixels(pixels);grain.Apply(true);grain=Save(grain,Folder+"Grain_mineral.asset");
            Mineral("Calcario",new Color(.76f,.735f,.68f),grain,.19f);
            Mineral("Estuque",new Color(.86f,.85f,.81f),grain,.13f);
            var gold=Standard("Bronze_dourado",new Color(.53f,.365f,.155f),.68f,.36f);
            gold.mainTexture=grain;gold.mainTextureScale=Vector2.one*3;
            Standard("Ouro_das_molduras",new Color(.64f,.455f,.22f),.58f,.30f);
            Standard("Verde_profundo",new Color(.065f,.105f,.098f),.04f,.25f);
            Standard("Veludo_vinho",new Color(.245f,.065f,.074f),0,.15f);
            var wood=new Texture2D(1024,256,TextureFormat.RGB24,true){name="Carvalho_veios_finos",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};
            var timber=new Color[1024*256];
            for(int y=0;y<256;y++) for(int x=0;x<1024;x++)
            {
                float u=x/1024f,v=y/256f;
                float warp=Mathf.PerlinNoise(u*2.1f,v*3.1f)*3;
                float pores=Mathf.PerlinNoise(u*8,v*110+warp);
                float lines=Mathf.Pow(.5f+.5f*Mathf.Sin(v*490+warp*9),10);
                float broad=Mathf.PerlinNoise(u*1.5f,v*9);
                float value=.77f+.16f*broad+.11f*pores-.052f*lines;
                timber[y*1024+x]=new Color(value,value,value);
            }
            wood.SetPixels(timber);wood.Apply(true);wood=Save(wood,Folder+"Carvalho_veios_finos.asset");
            for(int i=0;i<7;i++)
            {
                float tone=(i-3)*.016f;
                var m=Standard("Carvalho_"+i,new Color(.48f+tone,.319f+tone*.8f,.188f+tone*.6f),0,.25f+i*.01f);
                m.mainTexture=wood;m.mainTextureScale=Vector2.one;m.mainTextureOffset=Vector2.zero;
            }
        }

        public static Material NightSky()
        {
            const int size=512;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                float maria=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.34f,.61f,Mathf.PerlinNoise(u*4+9,v*4+4)));
                float fine=Mathf.PerlinNoise(u*67,v*67)*.09f+Mathf.PerlinNoise(u*191,v*191)*.035f;
                float a=.52f+.30f*maria+fine;
                pixels[y*size+x]=new Color(a,a,a);
            }
            var rng=new System.Random(1922);
            for(int c=0;c<240;c++)
            {
                float cx=(float)rng.NextDouble()*size,cy=(float)rng.NextDouble()*size;
                float radius=2+Mathf.Pow((float)rng.NextDouble(),3)*30;
                for(int y=Mathf.Max(0,(int)(cy-radius*1.3f));y<Mathf.Min(size,(int)(cy+radius*1.3f));y++)
                    for(int x=Mathf.Max(0,(int)(cx-radius*1.3f));x<Mathf.Min(size,(int)(cx+radius*1.3f));x++)
                    {
                        float dx=(x-cx)/radius,dy=(y-cy)/radius,r=Mathf.Sqrt(dx*dx+dy*dy);
                        float rim=Mathf.Exp(-Mathf.Pow((r-.87f)*10,2));
                        float bowl=Mathf.Exp(-r*r*4);
                        float value=pixels[y*size+x].r+rim*(.065f-.075f*dx+.05f*dy)-.10f*bowl;
                        pixels[y*size+x]=new Color(value,value,value);
                    }
            }
            var texture=new Texture2D(size,size,TextureFormat.RGB24,true){name="Lua_relevo_autoral",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
            texture.SetPixels(pixels);texture.Apply(true);texture=Save(texture,Folder+"Lua_relevo_autoral.asset");
            var shader=Shader.Find("Museum/Night Sky");
            if(shader==null) throw new InvalidOperationException("Shader do céu ausente.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Ceu_noturno.mat");
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,Folder+"Ceu_noturno.mat");}
            material.shader=shader;material.shaderKeywords=Array.Empty<string>();material.renderQueue=-1;
            material.SetTexture("_MoonTex",texture);material.SetVector("_MoonDirection",new Vector4(.16f,1,-.18f,0));
            material.SetFloat("_MoonRadius",.068f);EditorUtility.SetDirty(material);return material;
        }

        public static void RefineFrame(Transform frame)
        {
            foreach(var filter in frame.GetComponentsInChildren<MeshFilter>(true))
            {
                var original=filter.sharedMesh;if(original==null)continue;
                if(!Frames.TryGetValue(original,out var refined))
                {
                    refined=UnityEngine.Object.Instantiate(original);var vertices=refined.vertices;
                    // Keep the original aperture; only slim the carved rail outside it.
                    for(int i=0;i<vertices.Length;i++)
                    {
                        var p=vertices[i];
                        p.x=Slim(p.x,.70f);p.y=Slim(p.y,.55f);p.z*=.72f;vertices[i]=p;
                    }
                    refined.vertices=vertices;refined.RecalculateNormals();refined.RecalculateBounds();
                    string key=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));
                    if(string.IsNullOrEmpty(key))key=original.name.Replace("/","_");
                    refined=Save(refined,"Assets/MuseumStyle/Meshes/Moldura_delicada_"+key+".asset");Frames.Add(original,refined);
                }
                filter.sharedMesh=refined;
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer!=null)
                {
                    var materials=renderer.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)materials[i]=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Ouro_das_molduras.mat");
                    renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
                }
            }
        }
        private static float Slim(float value,float edge)=>Mathf.Sign(value)*(Mathf.Min(Mathf.Abs(value),edge)+Mathf.Max(0,Mathf.Abs(value)-edge)*.52f);

        public static Rect PortraitCrop(string id)
        {
            // Only display UVs change. Full archival photographs and credits are preserved.
            if(id=="anita_bio")return new Rect(.035f,.575f,.91f,.425f);
            if(id=="tarsila_bio")return new Rect(0,.05f,.30f,.78f);
            if(id=="oswald_bio")return new Rect(.235f,.05f,.47f,.89f);
            return new Rect(0,0,1,1);
        }

        public static void AddReadingDisplays(Transform architecture)
        {
            var root=new GameObject("Mesas_de_leitura").transform;root.SetParent(architecture,false);
            var wood=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Carvalho_3.mat");
            var gold=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Bronze_dourado.mat");
            var green=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Verde_profundo.mat");
            var paper=Standard("Papel_das_vitrines",new Color(.80f,.76f,.64f),0,.14f);
            paper.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
            paper.EnableKeyword("_EMISSION");paper.SetColor("_EmissionColor",new Color(.45f,.425f,.36f));
            EditorUtility.SetDirty(paper);
            var glass=Standard("Vidro_das_vitrines",new Color(.32f,.46f,.52f,.10f),0,.48f);
            glass.SetFloat("_Mode",2);glass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHABLEND_ON");glass.renderQueue=3000;
            string[] titles={"MÁRIO DE ANDRADE","IDEIAS EM CIRCULAÇÃO","OSWALD DE ANDRADE"};
            Vector3[] positions={new Vector3(-17,0,-8.15f),new Vector3(-17,0,8.15f),new Vector3(6.55f,0,17)};
            for(int i=0;i<3;i++)
            {
                var table=new GameObject("Vitrine_ilha_leitura_"+i).transform;table.SetParent(root,false);
                table.SetPositionAndRotation(positions[i],Quaternion.Euler(0,i==1?180:i==2?-90:0,0));
                Box(table,"Base",new Vector3(0,.51f,0),new Vector3(1.38f,1.02f,.58f),green);
                Box(table,"Tampo",new Vector3(0,1.03f,0),new Vector3(1.60f,.09f,.78f),wood);
                Box(table,"Filete",new Vector3(0,1.09f,.36f),new Vector3(1.58f,.02f,.02f),gold);
                var book=new GameObject("Livro_cenografico_aberto").transform;book.SetParent(table,false);book.localPosition=new Vector3(0,1.105f,0);book.localRotation=Quaternion.Euler(-8,0,0);
                Box(book,"Capa",Vector3.zero,new Vector3(1.06f,.035f,.50f),green);
                foreach(int side in new[]{-1,1})
                {
                    var page=Box(book,"Paginas",new Vector3(side*.257f,.042f,0),new Vector3(.50f,.047f,.47f),paper);
                    page.localRotation=Quaternion.Euler(0,0,side*4);
                    for(int line=0;line<9;line++)Box(book,"Linha_editorial",new Vector3(side*.257f,.074f,-.17f+line*.038f),new Vector3(line==8?.25f:.37f,.001f,.003f),green);
                }
                // Glass cover has depth; no invented historical cover, signature or text.
                Box(table,"Vidro_superior",new Vector3(0,1.35f,0),new Vector3(1.58f,.009f,.76f),glass);
                foreach(int side in new[]{-1,1})
                {
                    Box(table,"Vidro_lateral",new Vector3(side*.785f,1.22f,0),new Vector3(.009f,.26f,.76f),glass);
                    Box(table,"Vidro_frontal",new Vector3(0,1.22f,side*.375f),new Vector3(1.58f,.26f,.009f),glass);
                }
                MuseumThematicSetup.Text(table,titles[i],new Vector3(0,.83f,.305f),.105f,new Color(.95f,.91f,.8f),30,1.25f,true);
                MuseumThematicSetup.Text(table,"LIVRO CENOGRÁFICO\nNão é um exemplar histórico.",new Vector3(0,.61f,.305f),.068f,new Color(.83f,.82f,.75f),32,1.25f);
                var col=table.gameObject.AddComponent<BoxCollider>();col.center=new Vector3(0,.68f,0);col.size=new Vector3(1.60f,1.36f,.78f);
            }
            var title=new GameObject("Ensaio_curatorial_Tarsila").transform;title.SetParent(architecture,false);title.position=new Vector3(1.2f,3.15f,-19.7f);
            Box(title,"Fundo_curatorial",new Vector3(0,-.52f,-.065f),new Vector3(4.4f,2.15f,.07f),green);
            foreach(int side in new[]{-1,1})Box(title,"Filete_curatorial",new Vector3(0,-.52f+side*1.045f,-.02f),new Vector3(4.34f,.018f,.015f),gold);
            MuseumThematicSetup.Text(title,"TARSILA",Vector3.zero,.58f,new Color(.91f,.78f,.50f),20,3.8f,true);
            MuseumThematicSetup.Text(title,"COR, IMAGINAÇÃO E TRABALHO",Vector3.down*.54f,.17f,new Color(.94f,.92f,.84f),30,3.8f);
            MuseumThematicSetup.Text(title,"1923–1933: formas, memórias\ne a vida na cidade industrial.",Vector3.down*1.03f,.15f,new Color(.85f,.86f,.81f),40,3.8f);
        }

        private static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;
            if(material.renderQueue>=3000){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
            return go.transform;
        }
        private static void Mineral(string name,Color color,Texture texture,float smooth)
        {
            var m=Standard(name,color,0,smooth);m.shader=Shader.Find("Museum/Mineral");m.mainTexture=texture;m.SetFloat("_GrainScale",1.4f);EditorUtility.SetDirty(m);
        }
        private static Material Standard(string name,Color color,float metal,float smooth)
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(Folder+name+".mat");
            if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,Folder+name+".mat");}
            m.shader=Shader.Find("Standard");m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",smooth);
            m.DisableKeyword("_EMISSION");EditorUtility.SetDirty(m);return m;
        }
        private static T Save<T>(T asset,string path) where T:UnityEngine.Object
        {
            var saved=AssetDatabase.LoadAssetAtPath<T>(path);
            if(saved==null){AssetDatabase.CreateAsset(asset,path);return asset;}
            EditorUtility.CopySerialized(asset,saved);UnityEngine.Object.DestroyImmediate(asset);EditorUtility.SetDirty(saved);return saved;
        }
    }
}
