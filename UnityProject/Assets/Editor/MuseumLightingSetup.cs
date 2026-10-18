using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MuseumModerna
{
    /// <summary>
    /// Ferramenta de Editor para iluminação dramática do museu.
    /// Acesse: MuseumModerna → Iluminação Dramática do Museu
    ///
    /// O que faz:
    ///  - Escurece a iluminação ambiente (sala quase no escuro)
    ///  - Adiciona Spotlight apontado para cada quadro (PaintingExhibit)
    ///  - Adiciona CandleFlicker às luzes existentes (Point Lights)
    ///  - Configura névoa atmosférica
    /// </summary>
    public class MuseumLightingSetup : EditorWindow
    {
        // ─── Configurações de Iluminação ──────────────────────────────────────

        private Color ambientColor     = new Color(0.04f, 0.02f, 0.02f); // quase preto, tom quente
        private Color spotColor        = new Color(1f, 0.82f, 0.45f);    // âmbar incandescente
        private float spotIntensity    = 2.2f;
        private float spotRange        = 5f;
        private float spotAngle        = 42f;
        private float spotHeight       = 1.8f;   // metros acima do quadro
        private float spotForward      = 1.2f;   // metros à frente do quadro
        private bool  spotShadows      = true;

        private bool  enableFog        = true;
        private Color fogColor         = new Color(0.06f, 0.04f, 0.04f);
        private float fogDensity       = 0.04f;

        private bool addCandleFlicker  = true;
        private Vector2 _scroll;

        // ─── Menu ─────────────────────────────────────────────────────────────

        [MenuItem("MuseumModerna/Iluminação Dramática do Museu")]
        public static void ShowWindow()
        {
            GetWindow<MuseumLightingSetup>("Museum Lighting").minSize = new Vector2(400, 580);
        }

        /// <summary>
        /// Versão reutilizável pela instalação do percurso e por batch mode. Mantém
        /// uma única estratégia de focos: remove os antigos Focos_das_obras e recria
        /// somente os Spotlight_ filhos dos painéis.
        /// </summary>
        public static void ApplyToScene()
        {
            var legacyRoot = GameObject.Find("Museum_LouvreStyle")?.transform.Find("Focos_das_obras");
            if (legacyRoot != null) DestroyImmediate(legacyRoot.gameObject);

            var setup = CreateInstance<MuseumLightingSetup>();
            try
            {
                setup.spotColor=new Color(1f,.94f,.84f);
                setup.spotAngle=95f;
                setup.spotForward=1.5f;
                setup.addCandleFlicker=false;
                setup.ApplyAll();
                LouvreGalleryLighting.ApplyGallery();
                var exhibition=GameObject.Find("Museum_GuidedExhibition");
                if(exhibition.GetComponent<MuseumPictureLighting>()==null)
                    exhibition.AddComponent<MuseumPictureLighting>();
                foreach(var light in exhibition.GetComponentsInChildren<Light>()) light.enabled=false;
                var architecture=GameObject.Find("Museum_LouvreStyle");
                foreach(var renderer in architecture.GetComponentsInChildren<MeshRenderer>())
                {
                    bool sky=renderer.name.Contains("Ceu_noturno")||renderer.name.StartsWith("Lua_claraboia_");
                    renderer.shadowCastingMode=sky?ShadowCastingMode.Off:ShadowCastingMode.On;
                    renderer.receiveShadows=!sky;
                }
                foreach(var light in architecture.GetComponentsInChildren<Light>())
                    if(light.name=="Luz_difusa") light.intensity=.4f;
                var previous=architecture.transform.Find("Luz_do_lustre");
                if(previous!=null) DestroyImmediate(previous.gameObject);
                var chandelier=new GameObject("Luz_do_lustre").AddComponent<Light>();
                chandelier.transform.SetParent(architecture.transform,false);
                chandelier.transform.localPosition=new Vector3(0,4.8f,0);
                chandelier.type=LightType.Point; chandelier.range=17;
                chandelier.color=new Color(1f,.88f,.70f); chandelier.intensity=2.35f;
                chandelier.renderMode=LightRenderMode.ForcePixel;
                chandelier.shadows=LightShadows.Soft; chandelier.shadowCustomResolution=512;
                chandelier.shadowStrength=.24f; chandelier.shadowBias=.045f; chandelier.shadowNormalBias=.35f;
                // Luz fria atravessando as claraboias: a origem coincide com o céu
                // estrelado e contrasta com o calor do lustre e das molduras.
                foreach(var center in new[]{new Vector3(-17,0,-5),new Vector3(-17,0,5),new Vector3(17,0,-5),new Vector3(17,0,5),new Vector3(0,0,17),new Vector3(0,0,-15)})
                {
                    string legacyName="Luz_sala_"+center.x+"_"+center.z;
                    var old=architecture.transform.Find(legacyName); if(old!=null) DestroyImmediate(old.gameObject);
                    string name="Luz_lunar_sala_"+center.x+"_"+center.z;
                    old=architecture.transform.Find(name); if(old!=null) DestroyImmediate(old.gameObject);
                    var fill=new GameObject(name).AddComponent<Light>();
                    fill.transform.SetParent(architecture.transform,false);
                    fill.transform.position=center+Vector3.up*6.52f; fill.transform.rotation=Quaternion.Euler(90,0,0);
                    fill.type=LightType.Spot; fill.spotAngle=112; fill.range=13; fill.intensity=1.5f;
                    fill.color=new Color(.64f,.77f,1f); fill.shadows=LightShadows.Soft;
                    fill.shadowCustomResolution=512; fill.shadowStrength=.38f;fill.shadowBias=.035f;
                    fill.renderMode=LightRenderMode.Auto;
                }
                var hallOld=architecture.transform.Find("Luz_lunar_Hall");
                if(hallOld!=null) DestroyImmediate(hallOld.gameObject);
                var hallMoon=new GameObject("Luz_lunar_Hall").AddComponent<Light>();
                hallMoon.transform.SetParent(architecture.transform,false);
                hallMoon.transform.localPosition=new Vector3(0,9.25f,0);hallMoon.transform.localRotation=Quaternion.Euler(90,0,0);
                hallMoon.type=LightType.Spot;hallMoon.spotAngle=118;hallMoon.range=16;hallMoon.intensity=1.05f;
                hallMoon.color=new Color(.52f,.67f,1f);hallMoon.shadows=LightShadows.Soft;
                hallMoon.shadowCustomResolution=512;hallMoon.shadowStrength=.32f;hallMoon.shadowBias=.04f;
                hallMoon.renderMode=LightRenderMode.Auto;
            }
            finally { DestroyImmediate(setup); }
        }

        // ─── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Iluminação Dramática do Museu", title);
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Transforma o museu numa experiência sombria e atmosférica:\n" +
                "sala escura com spotlights sobre cada quadro, névoa e velas tremeluzentes.",
                MessageType.Info);

            EditorGUILayout.Space(12);

            // Ambiente
            EditorGUILayout.LabelField("Ambiente", EditorStyles.boldLabel);
            ambientColor  = EditorGUILayout.ColorField("Cor Ambiente (quase preta)", ambientColor);
            enableFog     = EditorGUILayout.Toggle("Névoa Atmosférica", enableFog);
            if (enableFog)
            {
                EditorGUI.indentLevel++;
                fogColor   = EditorGUILayout.ColorField("Cor da Névoa", fogColor);
                fogDensity = EditorGUILayout.Slider("Densidade", fogDensity, 0.005f, 0.15f);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);

            // Spotlights
            EditorGUILayout.LabelField("Spotlights nos Quadros", EditorStyles.boldLabel);
            spotColor     = EditorGUILayout.ColorField("Cor do Spot", spotColor);
            spotIntensity = EditorGUILayout.Slider("Intensidade", spotIntensity, 0.5f, 6f);
            spotRange     = EditorGUILayout.Slider("Alcance (m)", spotRange, 2f, 10f);
            spotAngle     = EditorGUILayout.Slider("Ângulo (°)", spotAngle, 20f, 80f);
            spotHeight    = EditorGUILayout.Slider("Altura acima do quadro (m)", spotHeight, 0.5f, 3f);
            spotForward   = EditorGUILayout.Slider("Distância à frente (m)", spotForward, 0.3f, 2.5f);
            spotShadows   = EditorGUILayout.Toggle("Sombras Suaves", spotShadows);

            EditorGUILayout.Space(8);

            // Velas
            addCandleFlicker = EditorGUILayout.Toggle("CandleFlicker nas Point Lights", addCandleFlicker);

            EditorGUILayout.Space(16);

            // Botões
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Aplicar Tudo", GUILayout.Height(42)))
                ApplyAll();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Só Ambiente + Névoa"))  ApplyAmbient();
            if (GUILayout.Button("Só Spotlights"))        ApplySpotlights();
            if (GUILayout.Button("Só CandleFlicker"))     ApplyCandleFlicker();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);
            EditorGUILayout.HelpBox(
                "Dica: ajuste os valores acima e clique 'Aplicar Tudo' para ver o resultado.\n" +
                "Salve a cena (Ctrl+S) depois de aplicar.\n\n" +
                "Para deletar os spotlights gerados: busque 'Spotlight_' na hierarquia e delete.",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        // ─── Aplicar Tudo ─────────────────────────────────────────────────────

        private void ApplyAll()
        {
            ApplyAmbient();
            ApplySpotlights();
            if (addCandleFlicker) ApplyCandleFlicker();
            Debug.Log("[MuseumModerna] Iluminação dramática aplicada. Salve a cena (Ctrl+S).");
        }

        // ─── Ambiente e Névoa ─────────────────────────────────────────────────

        private void ApplyAmbient()
        {
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;

            if (enableFog)
            {
                RenderSettings.fog         = true;
                RenderSettings.fogMode     = FogMode.ExponentialSquared;
                RenderSettings.fogColor    = fogColor;
                RenderSettings.fogDensity  = fogDensity;
            }
            else
            {
                RenderSettings.fog = false;
            }

            // Apaga a Directional Light padrão se ainda existir (clareia tudo)
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    l.intensity = 0.05f; // quase apagada em vez de deletar
                    Undo.RecordObject(l, "Dim Directional Light");
                    Debug.Log($"[MuseumModerna] Luz direcional '{l.gameObject.name}' escurecida.");
                }
            }

            Debug.Log("[MuseumModerna] Ambiente escurecido. Névoa: " + (enableFog ? "ativada" : "desativada"));
        }

        // ─── Spotlights nos Quadros ───────────────────────────────────────────

        private void ApplySpotlights()
        {
            PaintingExhibit[] exhibits = FindObjectsByType<PaintingExhibit>(FindObjectsSortMode.None);

            if (exhibits.Length == 0)
            {
                Debug.LogWarning("[MuseumModerna] Nenhum PaintingExhibit encontrado na cena.");
                return;
            }

            int created = 0;
            foreach (PaintingExhibit exhibit in exhibits)
            {
                string spotName = $"Spotlight_{exhibit.gameObject.name}";

                // Remove spotlight anterior se já existia
                Transform existingSpot = exhibit.transform.Find(spotName);
                if (existingSpot != null)
                    Undo.DestroyObjectImmediate(existingSpot.gameObject);

                // Cria novo GameObject filho
                GameObject spotGO = new GameObject(spotName);
                Undo.RegisterCreatedObjectUndo(spotGO, "Create Painting Spotlight");
                spotGO.transform.SetParent(exhibit.transform);

                // Posiciona: acima + à frente do quadro
                // forward do quadro = direção que o visitante vê (para frente da parede)
                Vector3 localPos = new Vector3(0f, spotHeight, spotForward);
                spotGO.transform.localPosition = localPos;

                // Aponta para o centro do quadro
                Vector3 worldPaintingCenter = exhibit.transform.position;
                spotGO.transform.LookAt(worldPaintingCenter);

                // Configura a luz
                Light light = Undo.AddComponent<Light>(spotGO);
                light.type      = LightType.Spot;
                light.color     = spotColor;
                light.intensity = spotIntensity;
                light.range     = spotRange;
                light.spotAngle = spotAngle;
                light.shadows   = spotShadows ? LightShadows.Soft : LightShadows.None;
                light.shadowStrength = 0.7f;
                light.shadowCustomResolution = 512;
                light.shadowBias = .025f;
                light.renderMode = LightRenderMode.ForcePixel;

                created++;
            }

            Debug.Log($"[MuseumModerna] {created} spotlights criados nos quadros.");
        }

        // ─── CandleFlicker ────────────────────────────────────────────────────

        private void ApplyCandleFlicker()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            int added = 0;

            foreach (Light l in lights)
            {
                if (l.type != LightType.Point && l.type != LightType.Spot) continue;
                // Não adiciona nos spotlights dos quadros (já oscilam por shader)
                if (l.gameObject.name.StartsWith("Spotlight_")) continue;

                if (l.GetComponent<CandleFlicker>() == null)
                {
                    Undo.AddComponent<CandleFlicker>(l.gameObject);
                    added++;
                }
            }

            Debug.Log($"[MuseumModerna] CandleFlicker adicionado a {added} luzes.");
        }
    }
}
